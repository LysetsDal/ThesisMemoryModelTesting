using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ThreadSafetClassAnalyser.Model;
using ThreadSafetClassAnalyser.Utils;

namespace ThreadSafetClassAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CorrectlySynchronizedAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "CorrectlySynchronized";

        // --- FieldUsed ---
        public const string FieldUsedDiagnosticId = "FieldUsed";
        private static readonly AnalyserMetadata FieldUsedMetadata = new AnalyserMetadata(FieldUsedDiagnosticId);

        private static readonly DiagnosticDescriptor FieldUsedRule =
            new DiagnosticDescriptor(
                FieldUsedDiagnosticId,
                FieldUsedMetadata.Title,
                FieldUsedMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: FieldUsedMetadata.Description);
        
        // --- ConflictingAccessThread ---

        private const string ConflictingAccessThreadId = "ConflictingAccessThread";

        private static readonly AnalyserMetadata conflictingAccessThreadMetadata =
            new AnalyserMetadata(ConflictingAccessThreadId);

        private static readonly DiagnosticDescriptor ConflictingAccessThreadRule =
            new DiagnosticDescriptor(
                ConflictingAccessThreadId,
                conflictingAccessThreadMetadata.Title,
                conflictingAccessThreadMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: conflictingAccessThreadMetadata.Description);
        
        
        // --- Test Rule ---
        private const string TestRuleId = "TestRule";

        private static readonly AnalyserMetadata TestRuleMetadata = 
            new AnalyserMetadata(TestRuleId);
        
        private static readonly DiagnosticDescriptor TestRule =
            new DiagnosticDescriptor(
                TestRuleId,
                TestRuleMetadata.Title,
                TestRuleMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: TestRuleMetadata.Description
            );
        
        // --- Register all supported diagnostics ---
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            [DebuggerStepThrough()]
            get =>
                ImmutableArray.Create(
                    FieldUsedRule,
                    ConflictingAccessThreadRule
                );
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the Actions here.
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesInThreads, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesInTasks, SyntaxKind.ClassDeclaration);
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
            if (classSymbol == null) 
                return;

            // Guard for the [ThreadSafe] annotation
            if (!AnalyzerUtils.IsTargetInThreadSafeClass(classSymbol)) return;
            
            // Find all thread instantiations in a class
            var threadCreations = AnalyzerUtils.GetObjectCreationsInClass(context, semanticModel, knownType);
            if (threadCreations is null) return;
            
            if (threadCreations.Count < 2) return;
            
            var classLocks =
                AnalyzerUtils.GetClassLockAssociationDict(classSymbol, semanticModel);
            
            // Map each thread to the fields it accesses
            var analyzedThreads = threadCreations.Select(tc => 
            {
                var bodyExpr = tc.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
                var bodySymbol = bodyExpr != null ? semanticModel.GetSymbolInfo(bodyExpr).Symbol : null;

                return new ThreadAnalysisInfo
                {
                    Syntax = tc,
                    BodySymbol = bodySymbol,
                    Name = AnalyzerUtils.GetThreadName(tc),
                    AccessMap = AnalyzerUtils.GetAccessedFields(tc, semanticModel),
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

                        if (AnalyzerUtils.IsCorrectlySynchronized(info1, info2)) continue;

                        // Report
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
                ConflictingAccessThreadRule,
                t1.Syntax.GetLocation(),
                fieldName, t1.Name, t2.Name));

            context.ReportDiagnostic(Diagnostic.Create(
                ConflictingAccessThreadRule,
                t2.Syntax.GetLocation(),
                fieldName, t2.Name, t1.Name));
        }
    }
}
