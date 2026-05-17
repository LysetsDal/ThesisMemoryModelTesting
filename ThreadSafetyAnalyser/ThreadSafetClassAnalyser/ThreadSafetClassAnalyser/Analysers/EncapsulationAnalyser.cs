using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using ThreadSafetClassAnalyser.Rules;
using ThreadSafetClassAnalyser.Utils;
// ReSharper disable UnusedType.Global

namespace ThreadSafetClassAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EncapsulationAnalyser : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(
                EncapsulationRules.FieldAccessedExternallyRule,
                EncapsulationRules.PublicFieldExposedRule,
                EncapsulationRules.FieldDoesNotUseLockRule,
                EncapsulationRules.InternalFieldNoLockRule,
                EncapsulationRules.LockObjectExposedRule,
                EncapsulationRules.InconsistentLockUseRule
            );
        
        // Internal = The diagnostic message is internally visible inside the class with the field or method.
        // External = The diagnostic rule is externally visible (at the call-site class) but not inside the class with the field or method.
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // [External] (FieldAccessedExternallyRule)
            // This rule flags field or property accesses at the call site
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            
            // [Internal] (PublicFieldExposedRule)
            // This rule flags public fields and public props with public accessor modifiers internally
            context.RegisterSyntaxNodeAction(AnalyzePublicFieldDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzePublicPropertyDeclaration, SyntaxKind.PropertyDeclaration);
            
            // [External] (FieldDoesNotUseLockRule)
            // This rule flags Methods at the callsite, if they don't use an accessor with a lock
            // or have no call-site locking around the method.
            context.RegisterSyntaxNodeAction(AnalyzeCallingMemberAccessWithLock, SyntaxKind.SimpleMemberAccessExpression);
            
            // [Internal] (InternalFieldNoLockRule) or (InconsistentLockUseRule)
            // This rule flags fields internally, if they have public accessors without synchronization.
            context.RegisterSyntaxNodeAction(AnalyzeInternalFieldAccessLockUsage, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInternalFieldAccessLockUsage, SyntaxKind.PropertyDeclaration);
            
            // [Internal] (LockObjectExposedRule)
            // Finds all locks in a namedType (a class) that are exposed through public accessor.
            context.RegisterSyntaxNodeAction(AnalyzeExposedLocksInFile, SyntaxKind.ClassDeclaration);
        }
        
        // -------------------------------------------------------------------------
        // Internal: LockObjectExposed through public Accessor
        // -------------------------------------------------------------------------
        private static void AnalyzeExposedLocksInFile(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel; // Safe and efficient
            // Get the symbol for the logical class
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return;
            
            // Use the validator with the SyntaxNode context
            if (!ThreadSafeValidator.ShouldValidateTarget(classSymbol)) return;

            // Use your utility to find lock objects used anywhere in the class
            var lockMap = LockAssociationUtils.GetClassLocks(classSymbol, semanticModel);
            var allLockSymbols = lockMap.Keys.ToImmutableHashSet(SymbolEqualityComparer.Default);

            if (allLockSymbols.IsEmpty) return;

            // Only process members physically written in THIS file
            foreach (var memberSyntax in classDecl.Members)
            {
                if (!(memberSyntax is MethodDeclarationSyntax || memberSyntax is PropertyDeclarationSyntax)) 
                    continue;

                var memberSymbol = semanticModel.GetDeclaredSymbol(memberSyntax);
                if (memberSymbol == null || memberSymbol.DeclaredAccessibility != Accessibility.Public) 
                    continue;

                // Inspect exit points (Returns/Arrows) within this file
                var exitPoints = memberSyntax.DescendantNodes()
                    .Where(n => n is ReturnStatementSyntax || n is ArrowExpressionClauseSyntax);

                foreach (var exit in exitPoints)
                {
                    var expr = exit is ReturnStatementSyntax ret ? ret.Expression : ((ArrowExpressionClauseSyntax)exit).Expression;
                    if (expr == null) continue;

                    // Safe to analyze because expr and semanticModel belong to the same tree
                    CheckAndReportLockObjectExposedRule(context, semanticModel, expr, allLockSymbols, memberSymbol, classSymbol.Name);
                }
            }
        }

        /// <summary>
        /// Helper func for AnalyzeExposedClassLocks() to verify if an expression resolves to one of our forbidden lock symbols.
        /// </summary>
        private static void CheckAndReportLockObjectExposedRule(
            SyntaxNodeAnalysisContext context, 
            SemanticModel semanticModel, 
            ExpressionSyntax expression, 
            IImmutableSet<ISymbol> lockSymbols,
            ISymbol member,
            string className)
        {
            var returnedSymbol = semanticModel.GetSymbolInfo(expression).Symbol;

            if (returnedSymbol == null || !lockSymbols.Contains(returnedSymbol)) return;
            
            var diagnostic = Diagnostic.Create(
                EncapsulationRules.LockObjectExposedRule, 
                expression.GetLocation(), 
                returnedSymbol.Name,
                className,
                member.Name);

            context.ReportDiagnostic(diagnostic);
        }

        // -------------------------------------------------------------------------
        // Existing: FieldAccessedExternally
        // -------------------------------------------------------------------------
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol;
            if (symbol is null) return;
            
            // // Guard: Only run if context class is annotated with: [ThreadSafe]
            if (!ThreadSafeValidator.ShouldValidate(context)) return;
            
            // // Guard: Only run if annotated with: [ThreadSafe]
            if (!ThreadSafeValidator.ShouldValidateTarget(symbol)) return;

            // Guard: Only care about mutable fields and props (i.e. not readonly or const)
            if (!SyntaxUtils.IsMutableFieldOrProperty(symbol)) return;
            
            var isInSource = symbol.Locations.FirstOrDefault().IsInSource;
            if (!isInSource) return;
            
            var containingType = symbol.ContainingType;
            var accessContainingType = context.ContainingSymbol?.ContainingType;

            // Guard: Skip members in Enums
            if (containingType?.TypeKind == TypeKind.Enum) return;
            
            // Guard: Skip if Field or Prop accessor uses a lock
            var descendantLock = LockAssociationUtils.FindFirstDescendantLockFromMethodSymbol(symbol);
            if (descendantLock != null) return;
            
            // Only warn if accessed from outside the declaring type
            if (accessContainingType != null &&
                SymbolEqualityComparer.Default.Equals(containingType, accessContainingType))
                return;
            
            var declarationDiagnostic = Diagnostic.Create(
                EncapsulationRules.FieldAccessedExternallyRule,
                memberAccess.Name.GetLocation(),
                symbol.Name,
                symbol.ContainingType.Name);

            context.ReportDiagnostic(declarationDiagnostic);
        }
        
        /// <summary>
        /// Analyses if a method to a private field is accessed without a lock. The warning is display at the call-site. Even if not marked with [ThreadSafe].
        /// </summary>
        /// <param name="context"> A MemberAccessExpressionSyntax node from the root analysis context</param>
        private static void AnalyzeCallingMemberAccessWithLock(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol;
            
            if (!ThreadSafeValidator.ShouldValidateTarget(symbol)) return;
            
            var memberName = memberAccess.Name.Identifier.Text;
            var className = context.ContainingSymbol?.ContainingType;

            // Only run this logic for Methods
            if (!(context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol is IMethodSymbol methodSymbol)) return;
            
            // Only for source code files (not SDK Libs)
            var isInSource = methodSymbol.Locations.FirstOrDefault().IsInSource;
            if (!isInSource) return;
            
            if (!(methodSymbol.ContainingSymbol is INamedTypeSymbol)) return;
            
            // Find any locks inside method call
            var methodDescendantLock = LockAssociationUtils.FindFirstDescendantLockFromMethodSymbol(methodSymbol);
            // Pt 'dumb' only knows if a Method call has a lock somewhere inside before a method, prop or class boundary is hit
            if (methodDescendantLock != null) return;
            
            // Does the current context have an ancestor lock around it?
            var callSiteAncestorLock = 
                LockAssociationUtils.GetFirstAncestorLockFromSymbol(memberAccess, context.SemanticModel);
            // If it does it is safe
            if (callSiteAncestorLock != null) return;
            
            // 
            if (AnalyzerUtils.IsInternallySynchronized(methodSymbol, context.SemanticModel)) 
                return;
            
            // No lock found at all!
            var diagnostic = Diagnostic.Create(
                EncapsulationRules.FieldDoesNotUseLockRule,
                memberAccess.Name.GetLocation(),
                memberName,
                className);

            context.ReportDiagnostic(diagnostic);
        }
        
        /// <summary>
        /// Analyses if a field or prop in a source class can be accessed through any field usages
        /// without a lock. Warning is displayed internally in the class
        /// </summary>
        /// <param name="context">A FieldDeclaration or PropertyDeclaration syntax node from the
        /// root analysis context
        /// </param> 
        private static void AnalyzeInternalFieldAccessLockUsage(SyntaxNodeAnalysisContext context)
        {
            if (!ThreadSafeValidator.ShouldValidate(context)) return;
            
            if (!SyntaxUtils.IsFieldOrPropParentAClass(context)) return;

            var className = context.ContainingSymbol?.ContainingType;
            var classDecl = context.Node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDecl == null) return;

            // 1. Identify all symbols declared by this node (Handles multiple fields or single property)
            var symbolsToAnalyze = new List<ISymbol>();

            if (context.Node is FieldDeclarationSyntax fieldDecl)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (symbol != null && !symbol.IsConst) 
                        symbolsToAnalyze.Add(symbol);
                }
            }
            else if (context.Node is PropertyDeclarationSyntax propDecl)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(propDecl);
                // Properties are generally not 'const', but we check IsReadOnly (no setter)
                if (symbol != null) 
                    symbolsToAnalyze.Add(symbol);
            }

            if (symbolsToAnalyze.Count == 0) return;
            
            // 2. Run analysis for each identified symbol
            foreach (var memberSymbol in symbolsToAnalyze)
            {
                // Check if the declaration is readonly
                var isInherentlyReadOnly = false;
                if (memberSymbol is IFieldSymbol field)
                {
                    isInherentlyReadOnly = field.IsReadOnly;
                }
                else if (memberSymbol is IPropertySymbol prop)
                {
                    isInherentlyReadOnly = prop.IsReadOnly || prop.SetMethod == null;
                }

                // If it's read-only, it's inherently thread-safe to read outside constructors.
                // We can skip collecting and analyzing usages entirely!
                if (isInherentlyReadOnly) continue;
                
                // Collect all usages of this specific field/property in the class
                var accessInfos = classDecl.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(id).Symbol, memberSymbol))
                    .Select(usage => new 
                    {
                        Usage = usage,
                        LockSymbol = LockAssociationUtils.GetEnclosingLockSymbol(usage, context.SemanticModel),
                        Method = usage.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault(),
                        IsWrite = AnalyzerUtils.IsWriteAccess(usage),
                        IsInsideConstructor = usage.Ancestors().OfType<ConstructorDeclarationSyntax>().Any()
                    })
                    .Where(info => !info.IsInsideConstructor && info.Method != null)
                    .ToList();

                // Determine the 'Primary Lock' for this specific member
                var primaryLock = accessInfos
                    .Where(a => a.LockSymbol != null)
                    .GroupBy(a => a.LockSymbol, SymbolEqualityComparer.Default)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                foreach (var info in accessInfos)
                {
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(info.Method);
                    if (SyntaxUtils.IsMethodPrivate(methodSymbol))
                        continue;
                    
                    // Check if the field is used as an argument to one of the atomic methods
                    if (AnalyzerUtils.IsInsideThreadSafePrimitive(info.Usage, context.SemanticModel))
                        continue;

                    // SCENARIO A: No lock used
                    if (info.LockSymbol == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            EncapsulationRules.InternalFieldNoLockRule,
                            info.Usage.GetLocation(),
                            memberSymbol.Name,
                            className?.Name ?? "Unknown",
                            info.Method.Identifier.Text));
                        continue;
                    }

                    // SCENARIO B: Inconsistent Lock
                    if (primaryLock != null && !SymbolEqualityComparer.Default.Equals(info.LockSymbol, primaryLock))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            EncapsulationRules.InconsistentLockUseRule,
                            info.Usage.GetLocation(),
                            memberSymbol.Name,
                            info.LockSymbol.Name,
                            primaryLock.Name));
                    }
                }
            }
        }
        
        // -------------------------------------------------------------------------
        // PublicFieldExposed — detects raw public fields (not properties)
        // -------------------------------------------------------------------------
        private static void AnalyzePublicFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDecl = (FieldDeclarationSyntax)context.Node;
            
            // Guard Clause: Only run if annotated with: [ThreadSafe]
            if (!ThreadSafeValidator.ShouldValidate(context)) return;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!SyntaxUtils.IsFieldOrPropParentAClass(context)) return;
            
            // Only care about public fields
            if (!fieldDecl.Modifiers.Any(SyntaxKind.PublicKeyword)) return;

            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol;
                if (symbol == null) continue;

                // Constants and static readonly fields are generally acceptable
                if (symbol.IsConst || (symbol.IsStatic && symbol.IsReadOnly)) continue;

                var detail = symbol.IsReadOnly
                    ? "Field is readonly — consider exposing via a read-only property."
                    : "Field has no accessor control. Consider wrapping it in a property with a restricted setter.";

                var diagnostic = Diagnostic.Create(
                    EncapsulationRules.PublicFieldExposedRule,
                    variable.GetLocation(),
                    symbol.Name,
                    detail);

                context.ReportDiagnostic(diagnostic);
            }
        }

        // -------------------------------------------------------------------------
        // PublicFieldExposed — detects public properties with weak accessor modifiers
        // -------------------------------------------------------------------------
        private static void AnalyzePublicPropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Guard Clause: Only run if annotated with: [ThreadSafe]
            if (!ThreadSafeValidator.ShouldValidate(context)) return;

            var propDecl = (PropertyDeclarationSyntax)context.Node;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!SyntaxUtils.IsFieldOrPropParentAClass(context)) return;
            
            // Only care about public properties (intersection of private)
            if (propDecl.Modifiers.Any(SyntaxKind.PrivateKeyword)) return;

            // expression-bodied property — read-only by nature, skip
            if (propDecl.AccessorList == null) return; 

            var accessors = propDecl.AccessorList.Accessors;

            foreach (var accessor in accessors)
            {
                var isSetter = accessor.IsKind(SyntaxKind.SetAccessorDeclaration);
                var isIniter = accessor.IsKind(SyntaxKind.InitAccessorDeclaration);

                if (!isSetter && !isIniter) continue;
                
                var isPrivate = accessor.Modifiers.Any(SyntaxKind.PrivateKeyword);
                if (isPrivate) continue;
                
                // Auto-generated accessor: no body and no modifiers
                var isAutoGenerated = accessor.Body == null && accessor.ExpressionBody == null;
                var hasNoModifier = !accessor.Modifiers.Any();

                if (isAutoGenerated && hasNoModifier)
                {
                    // Public property with auto-generated public setter — fully open
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                        EncapsulationRules.PublicFieldExposedRule,
                        accessor.GetLocation(),
                        propDecl.Identifier.Text,
                        "Property has an auto-generated public setter with no access restriction (e.g. 'private set' or 'protected set')."
                    ));
                }

                if (hasNoModifier) continue;
                
                // Accessor has an explicit modifier — report it informatively
                var modifierText = string.Join(" ", accessor.Modifiers.Select(m => m.Text));
                var accessorKind = isSetter ? "setter" : "init accessor";
                
                context.ReportDiagnostic(
                    Diagnostic.Create(
                    EncapsulationRules.PublicFieldExposedRule,
                    accessor.GetLocation(),
                    propDecl.Identifier.Text,
                    $"Property {accessorKind} is explicitly marked '{modifierText}'."
                ));
            }
        }
    }
}
