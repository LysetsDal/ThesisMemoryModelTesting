using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using ThreadSafetClassAnalyser.Model;
using ThreadSafetClassAnalyser.Rules;
using ThreadSafetClassAnalyser.Utils;
// ReSharper disable RedundantAnonymousTypePropertyName
// ReSharper disable UnusedType.Global

namespace ThreadSafetClassAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CorrectlySynchronizedAnalyzer : DiagnosticAnalyzer
    {
        // --- Register all supported diagnostics ---
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                CorrectlySynchronizedRules.ConflictingAccessThreadRule,
                CorrectlySynchronizedRules.VolatileReorderingRule,
                CorrectlySynchronizedRules.LockOnClassInstanceRule,
                CorrectlySynchronizedRules.ConflictingAccessRule,
                CorrectlySynchronizedRules.MonitorNotPairedRule,
                CorrectlySynchronizedRules.MonitorNotInFinallyRule,
                CorrectlySynchronizedRules.MonitorConflictingAccessRule
            );
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the Actions here.
            // [Internal] (ConflictingAccessThreadRule)
            // Flags Conflicting accesses on class fields and properties in Thread or Task bodies
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesInThreads, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesInTasks, SyntaxKind.ClassDeclaration);
            
            // [Internal] (VolatileReorderingRule)
            // Flags method bodies with possible volatile reorderings (Independent Store/Load reordering Example)
            context.RegisterSyntaxNodeAction(AnalyzeVolatileReordering, SyntaxKind.ClassDeclaration);

            // [Internal] (LockOnClassInstanceRule)
            // Flags methods in classes that use 'this' (class instance) as a lock target
            context.RegisterSyntaxNodeAction(AnalyzeLockThis, SyntaxKind.ClassDeclaration);
            
            // [Internal] (ConflictingAccessRule)
            // Runs a pair-wise check on all methods and fields. Flags a warning internally, if there is a possibility
            // of conflicting access on shared mutable class state. Flags all methods 
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesAcrossMembers, SyntaxKind.ClassDeclaration);

            // [Internal] (MonitorNotPairedRule / MonitorNotInFinallyRule / MonitorConflictingAccessRule)
            // Flags incorrect or unsafe usage of Monitor.Enter / Monitor.Exit
            context.RegisterSyntaxNodeAction(AnalyzeMonitorUsage, SyntaxKind.ClassDeclaration);
        }
        
        // =============================================================
        // ============== LOCKING ON THE 'THIS' INSTANCE ===============
        // =============================================================
        private static void AnalyzeLockThis(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            // 1. Guard: Only run if the class is annotated with [ThreadSafe]
            if (classSymbol == null || !ThreadSafeValidator.ShouldValidateTarget(classSymbol)) 
                return;

            // 2. Use utility to find all locks and their associations
            var lockMap = LockAssociationUtils.GetClassLocks(classSymbol, semanticModel);

            foreach (var associations in lockMap.Values)
            {
                foreach (var association in associations)
                {
                    var lockStmt = association.Lock;
                    var isLockThis = false;

                    // Direct check for 'lock(this)'
                    if (lockStmt.Expression is ThisExpressionSyntax)
                    {
                        isLockThis = true;
                    }
                    // Semantic check to see if the expression resolves to the class instance
                    else
                    {
                        var lockSymbol = semanticModel.GetSymbolInfo(lockStmt.Expression).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(lockSymbol, classSymbol))
                        {
                            isLockThis = true;
                        }
                    }

                    if (!isLockThis) continue;
                    
                    // 3. Extract the name of the method/member containing the lock
                    // We use your existing GetBodyName helper for consistent naming
                    var enclosingMemberNode = lockStmt.Ancestors()
                        .FirstOrDefault(a => a is MemberDeclarationSyntax || a is AccessorDeclarationSyntax);
                        
                    var displayMethodName = enclosingMemberNode != null 
                        ? SyntaxUtils.GetBodyName(enclosingMemberNode) 
                        : (association.MemberContainingLock?.Name ?? "unknown");
                        
                    context.ReportDiagnostic(Diagnostic.Create(
                        CorrectlySynchronizedRules.LockOnClassInstanceRule,
                        lockStmt.Expression.GetLocation(),
                        classSymbol.Name,  // {0}
                        displayMethodName                   // {1}
                    ));
                }
            }
        }
        
        // =============================================================
        // ========= POSSIBLE VOLATILE STORE LOAD REORDERING ===========
        // =============================================================
        private static void AnalyzeVolatileReordering(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            // Guard: Only run if the class is annotated with [ThreadSafe]
            if (classSymbol == null || !ThreadSafeValidator.ShouldValidateTarget(classSymbol)) 
                 return;

            // To detect reordering, we scan every method and lambda body in the class
            var bodies = classDecl.DescendantNodes()
                .Where(n => n is MethodDeclarationSyntax || n is AnonymousFunctionExpressionSyntax);

            foreach (var body in bodies)
            {
                var methodName = SyntaxUtils.GetBodyName(body);

                // 1. Establish Program Order (PO) within this specific method/lambda body 
                var identifiers = body.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();

                for (var i = 0; i < identifiers.Count; i++)
                {
                    var idWrite = identifiers[i];
                    var writeSymbol = semanticModel.GetSymbolInfo(idWrite).Symbol as IFieldSymbol;

                    // Step: Filter for Volatile Write
                    if (writeSymbol == null || !writeSymbol.IsVolatile || !AnalyzerUtils.IsWriteAccess(idWrite))
                        continue;

                    for (var j = i + 1; j < identifiers.Count; j++)
                    {
                        var idRead = identifiers[j];
                        var readSymbol = semanticModel.GetSymbolInfo(idRead).Symbol as IFieldSymbol;

                        // Step: Identify Volatile Read occurring later in PO 
                        if (readSymbol == null || !readSymbol.IsVolatile || AnalyzerUtils.IsWriteAccess(idRead))
                            continue;

                        // GUARD: Skip if the volatile read is executed inside a lock statement
                        // OR inside a Monitor.Enter/TryEnter-guarded try block (manual lock pattern)
                        if (LockAssociationUtils.GetEnclosingLockSymbol(idRead, semanticModel) != null
                            || AnalyzerUtils.IsInsideMonitorEnterRegion(idRead, semanticModel))
                            continue;
                        
                        // 2. Check for Full Fences (Intervening synchronization) 
                        // We pass 'body' as the root to check for barriers between the write and read
                        if (!AnalyzerUtils.HasFullFenceBetween(body, idWrite, idRead, semanticModel))
                        {
                            // 3. Flag Total Order Violation
                            context.ReportDiagnostic(Diagnostic.Create(
                                CorrectlySynchronizedRules.VolatileReorderingRule,
                                idRead.GetLocation(),
                                readSymbol.Name,
                                methodName,
                                writeSymbol.Name));
                        }
                    }
                }
            }
        }
        
        
        // =============================================================
        // ========= CONFLICTING ACCESSES IN TASKS AND THREADS =========
        // =============================================================
        private static void AnalyzeConflictingAccessesInThreads(SyntaxNodeAnalysisContext context)
        {
            AnalyzeConflictingAccessesInClass(context, KnownTypes.Thread);
        }
        
        private static void AnalyzeConflictingAccessesInTasks(SyntaxNodeAnalysisContext context)
        {
            AnalyzeConflictingAccessesInClass(context, KnownTypes.Task);
        }        
        
        private static void AnalyzeConflictingAccessesInClass(SyntaxNodeAnalysisContext context, string knownType)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            
            // Check that class was found
            if (classSymbol == null) return;

            // Guard for the [ThreadSafe] annotation
            if (!ThreadSafeValidator.ShouldValidateTarget(classSymbol)) return;
            
            
            // Find all thread instantiations in a class
            var threadCreations = AnalyzerUtils.GetObjectCreationsInClass(context, semanticModel, knownType);
            if (threadCreations is null) return;
            
            if (threadCreations.Count < 2) return;
            
            var classLocks = LockAssociationUtils.GetClassLocks(classSymbol, semanticModel);
            
            // Map each thread to the fields it accesses
            var analyzedThreads = threadCreations.Select(tc => 
            {
                var bodyExpr = tc.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
                var bodySymbol = bodyExpr != null ? semanticModel.GetSymbolInfo(bodyExpr).Symbol : null;

                return new ThreadAnalysisInfo
                {
                    Syntax = tc,
                    BodySymbol = bodySymbol,
                    Name = SyntaxUtils.GetThreadName(tc),
                    AccessMap = AnalyzerUtils.GetAccessedFieldsFromExpression(tc, semanticModel),
                    MethodScope = tc.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault(),
                    UsedLockObjects = AnalyzerUtils.GetLockObjectsUsedInMember(classLocks, bodySymbol)
                };
            }).Where(t => t.BodySymbol != null).ToList();
            
            // 4. Compare every thread against every other thread 
            for (var i = 0; i < analyzedThreads.Count; i++)
            {
                for (var j = i + 1; j < analyzedThreads.Count; j++)
                {
                    var t1 = analyzedThreads[i];
                    var t2 = analyzedThreads[j];

                    // Filter by Method Scope (Only sibling threads)
                    if (t1.MethodScope != t2.MethodScope) continue;

                    // Macro-check: Do they share a common lock at the top level?
                    if (t1.UsedLockObjects
                        .Intersect(t2.UsedLockObjects, SymbolEqualityComparer.Default).Any())
                        continue;

                    // Find conflicts using the AccessMaps
                    var conflicts = AnalyzerUtils.FindConflicts(t1.AccessMap, t2.AccessMap);

                    foreach (var conflict in conflicts)
                    {
                        // Micro-check: Is this specific field protected by the same lock?
                        var info1 = t1.AccessMap[conflict];
                        var info2 = t2.AccessMap[conflict];

                        if (AnalyzerUtils.IsUsingSameLockObject(info1, info2)) continue;
                        
                        ReportThreadConflict(context, t1, t2, conflict.Name);
                    }
                }
            }
        }
        
        private static void ReportThreadConflict(
            SyntaxNodeAnalysisContext context, 
            ThreadAnalysisInfo t1, 
            ThreadAnalysisInfo t2, 
            string fieldName)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CorrectlySynchronizedRules.ConflictingAccessThreadRule,
                t1.Syntax.GetLocation(),
                fieldName, t1.Name, t2.Name));

            context.ReportDiagnostic(Diagnostic.Create(
                CorrectlySynchronizedRules.ConflictingAccessThreadRule,
                t2.Syntax.GetLocation(),
                fieldName, t2.Name, t1.Name));
        }
        
        // =====================================================
        // ============= CONFLICTING ACCESS TEST ===============
        // =====================================================
        private static void AnalyzeConflictingAccessesAcrossMembers(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            
            if (classSymbol == null) return;

            // Guard for the [ThreadSafe] annotation
            if (!ThreadSafeValidator.ShouldValidateTarget(classSymbol)) return;

            // 1. Collect all instance methods written in this class (skip static methods)
            var methodDeclarations = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => !m.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Where(m => m.GetLocation().IsInSource)
                .ToList();

            if (methodDeclarations.Count < 2) return;

            // 2. Map out all the class locks
            var classLocks = LockAssociationUtils.GetClassLocks(classSymbol, semanticModel);

            // 3. A list of all members (methods) in the class 
            var analyzedMembers = methodDeclarations.Select(methodDecl =>
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
                if (methodSymbol == null) return null;
                
                if (SyntaxUtils.IsExclusivelyConstructorCalled(methodSymbol, semanticModel, classDecl))
                {
                    return null; 
                }

                return new
                {
                    Syntax = methodDecl,
                    Symbol = methodSymbol,
                    Name = methodSymbol.Name,
                    AccessMap = AnalyzerUtils.GetAccessedFieldsFromMethod(methodDecl, semanticModel),
                    UsedLockObjects = AnalyzerUtils.GetLockObjectsUsedInMember(classLocks, methodSymbol).ToList()
                };
            }).Where(m => m != null).ToList();
            
            // A set of distinct conflicts (MethodName, sharedState)
            var reportedConflicts = new HashSet<(MethodDeclarationSyntax Method, ISymbol Field)>();

            // 4. Pairwise comparison matrix (Compare every method against every other member)
            for (var i = 0; i < analyzedMembers.Count; i++)
            {
                for (var j = i + 1; j < analyzedMembers.Count; j++)
                {
                    var m1 = analyzedMembers[i];
                    var m2 = analyzedMembers[j];
                    
                    // IMPROVEMENT: Avoid flagging wrapper methods
                    // If one method symbol is found inside the other's syntax tree, skip the comparison
                    if (m1.Syntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
                        .Any(inv => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(inv).Symbol, m2.Symbol))) continue;
                    if (m2.Syntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
                        .Any(inv => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(inv).Symbol, m1.Symbol))) continue;
                    
                    // Find overlapping field/property accesses
                    var conflicts = AnalyzerUtils.FindConflicts(m1.AccessMap, m2.AccessMap);

                    foreach (var conflict in conflicts)
                    {
                        if (!SyntaxUtils.IsMutableFieldOrProperty(conflict)) continue;

                        // Is this specific field protected by synchronization in the AccessMap?
                        var info1 = m1.AccessMap[conflict];
                        var info2 = m2.AccessMap[conflict];

                        if (AnalyzerUtils.IsUsingSameLockObject(info1, info2)) continue;
                        
                        var protectedByLock = AnalyzerUtils.IsUsingSameLockObject(info1, info2);

                        // If both sides use Interlocked/Volatile, or the field is volatile, 
                        // it satisfies the "Correctly Synchronized" property for simple state.
                        var protectedByAtomics = (info1.IsVolatile || info1.IsAtomicCall) && 
                                                  (info2.IsVolatile || info2.IsAtomicCall);
                        
                        // Check for Correctly Synchronized 
                        if (protectedByLock || protectedByAtomics) continue;
                        
                        // Only flag Method 1 if it hasn't been warned about this specific field yet
                        if (reportedConflicts.Add((m1.Syntax, conflict)))
                        {
                            ReportSingleMemberConflict(context, m1.Syntax, m1.Name, m2.Name, conflict.Name);
                        }
                
                        // Only flag Method 2 if it hasn't been warned about this specific field yet
                        if (reportedConflicts.Add((m2.Syntax, conflict)))
                        {
                            ReportSingleMemberConflict(context, m2.Syntax, m2.Name, m1.Name, conflict.Name);
                        }
                    }
                }
            }
        }

        private static void ReportSingleMemberConflict(
            SyntaxNodeAnalysisContext context, 
            MethodDeclarationSyntax methodSyntax, 
            string methodName, 
            string conflictingMethodName, 
            string fieldName)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CorrectlySynchronizedRules.ConflictingAccessRule,
                methodSyntax.Identifier.GetLocation(),
                fieldName, methodName, conflictingMethodName));
        }

        // =============================================================
        // ============== MONITOR ENTER / EXIT USAGE ===================
        // =============================================================
        private static void AnalyzeMonitorUsage(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            // Guard: Only run if the class is annotated with [ThreadSafe]
            if (classSymbol == null || !ThreadSafeValidator.ShouldValidateTarget(classSymbol))
                return;

            // Scan every non-static instance method in the class
            var methodDeclarations = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => !m.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Where(m => m.GetLocation().IsInSource);

            foreach (var methodDecl in methodDeclarations)
            {
                var methodName = semanticModel.GetDeclaredSymbol(methodDecl)?.Name ?? "unknown";

                // Collect all invocations in this method that target System.Threading.Monitor
                var invocations = methodDecl.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(inv =>
                    {
                        var sym = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                        return sym?.ContainingType?.ToDisplayString() == KnownTypes.FullMonitorName;
                    })
                    .ToList();

                // --- Collect Enter and Exit call sites ---
                var enterCalls = invocations
                    .Where(inv => (semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol)?.Name
                                  is "Enter" ||
                                  (semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol)?.Name
                                  is "TryEnter")
                    .ToList();

                var exitCalls = invocations
                    .Where(inv => (semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol)?.Name == "Exit")
                    .ToList();

                foreach (var enterCall in enterCalls)
                {
                    // GUARD: Monitor.Enter(obj, ref lockTaken) is the intentional cross-method pairing
                    // pattern. Enter and Exit will be in different methods by design — skip lifetime checks.
                    var isManualLifetimePattern = enterCall.ArgumentList.Arguments.Count >= 2;
                    if (isManualLifetimePattern) continue;

                    // Resolve the lock-object argument symbol (first argument of Monitor.Enter)
                    var lockArgExpr = enterCall.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                    var lockObjSymbol = lockArgExpr != null
                        ? semanticModel.GetSymbolInfo(lockArgExpr).Symbol
                        : null;

                    var lockObjName = lockObjSymbol?.Name ?? lockArgExpr?.ToString() ?? "unknown";

                    // --- Rule: MonitorNotPaired ---
                    // Check there is at least one Exit call for the same lock object in this method
                    var hasMatchingExit = exitCalls.Any(exit =>
                    {
                        var exitArgExpr = exit.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                        var exitObjSymbol = exitArgExpr != null
                            ? semanticModel.GetSymbolInfo(exitArgExpr).Symbol
                            : null;
                        return SymbolEqualityComparer.Default.Equals(lockObjSymbol, exitObjSymbol);
                    });

                    if (!hasMatchingExit)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            CorrectlySynchronizedRules.MonitorNotPairedRule,
                            enterCall.GetLocation(),
                            methodName,     // {0}
                            lockObjName));  // {1}
                    }

                    // --- Rule: MonitorNotInFinally ---
                    // Check that the Enter call is inside a try/finally block
                    var enclosingTry = enterCall.Ancestors().OfType<TryStatementSyntax>().FirstOrDefault();
                    var hasFinally = enclosingTry?.Finally != null;

                    if (!hasFinally)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            CorrectlySynchronizedRules.MonitorNotInFinallyRule,
                            enterCall.GetLocation(),
                            methodName,     // {0}
                            lockObjName));  // {1}
                    }

                    // --- Rule: MonitorConflictingAccess ---
                    // Bare-bones placeholder until access-map comparison is wired up
                    context.ReportDiagnostic(Diagnostic.Create(
                        CorrectlySynchronizedRules.MonitorConflictingAccessRule,
                        enterCall.GetLocation(),
                        lockObjName,    // {0}
                        methodName,     // {1}
                        "?"));          // {2} — conflicting method, TBD in next iteration
                }
            }
        }
    }
}
