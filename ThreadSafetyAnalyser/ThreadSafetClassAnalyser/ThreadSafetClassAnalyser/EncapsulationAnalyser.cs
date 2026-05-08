using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ThreadSafetClassAnalyser.Utils;

namespace ThreadSafetClassAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EncapsulationAnalyser : DiagnosticAnalyzer
    {
        private const string Category = "Encapsulation";
        
        // --- FieldAccessedExternally Rule ---
        private const string FieldAccessedExternallyDiagnosticId = "FieldAccessedExternally";
        private static readonly AnalyserMetadata FieldAccessedExternallyMetadata = 
            new AnalyserMetadata(FieldAccessedExternallyDiagnosticId);
        
        private static readonly DiagnosticDescriptor FieldAccessedExternallyRule =
            new DiagnosticDescriptor(
                FieldAccessedExternallyDiagnosticId,
                FieldAccessedExternallyMetadata.Title,
                FieldAccessedExternallyMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: FieldAccessedExternallyMetadata.Description);

        // --- PublicFieldExposed Rule ---
        private const string PublicFieldExposedDiagnosticId = "PublicFieldExposed";

        private static readonly AnalyserMetadata PublicFieldExposedMetadata =
            new AnalyserMetadata(PublicFieldExposedDiagnosticId);
        
        private static readonly DiagnosticDescriptor PublicFieldExposedRule =
            new DiagnosticDescriptor(
                PublicFieldExposedDiagnosticId,
                PublicFieldExposedMetadata.Title,
                PublicFieldExposedMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                PublicFieldExposedMetadata.Description);

        // --- FieldDoesNotUseLock Rule ---
        private const string FieldDoesNotUseLockId = "FieldDoesNotUseLock";

        private static readonly AnalyserMetadata FieldDoesNotUseLockMetadata =
            new AnalyserMetadata(FieldDoesNotUseLockId);

        private static readonly DiagnosticDescriptor FieldDoesNotUseLockRule =
            new DiagnosticDescriptor(
                FieldDoesNotUseLockId,
                FieldDoesNotUseLockMetadata.Title,
                FieldDoesNotUseLockMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: FieldAccessedExternallyMetadata.Description
            );
        
        // --- InternalFieldNoLock ---
        private const string InternalFieldNoLockId = "InternalFieldNoLock";

        private static readonly AnalyserMetadata InternalFieldNoLockMetadata =
            new AnalyserMetadata(InternalFieldNoLockId);
        
        private static readonly DiagnosticDescriptor InternalFieldNoLockRule =
            new DiagnosticDescriptor(
                InternalFieldNoLockId,
                InternalFieldNoLockMetadata.Title,
                InternalFieldNoLockMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: InternalFieldNoLockMetadata.Description
            );        
        
        // --- Lock Object Exposed via Public Accessor ---
        private const string LockObjectExposedId = "LockObjectExposed";

        private static readonly AnalyserMetadata lockObjectExposedMetadata =
            new AnalyserMetadata(LockObjectExposedId);
        
        private static readonly DiagnosticDescriptor LockObjectExposedRule =
            new DiagnosticDescriptor(
                LockObjectExposedId,
                lockObjectExposedMetadata.Title,
                lockObjectExposedMetadata.MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: lockObjectExposedMetadata.Description
            );
        
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
            [DebuggerStepThrough]
            get =>
                ImmutableArray.Create(
                    FieldAccessedExternallyRule,
                    PublicFieldExposedRule,
                    InternalFieldNoLockRule,
                    FieldDoesNotUseLockRule,
                    LockObjectExposedRule,
                    TestRule
                );
        }
        
        // Internal = The diagnostic message is internally visible inside the class with the field or method.
        // External = The diagnostic rule is externally visible (at the call-site class) but not inside the class with the field or method.
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            // [External] This rule flags field or property accesses at the call site
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            
            // [Internal] This rule flags public fields internally
            context.RegisterSyntaxNodeAction(AnalyzePublicFieldDeclaration, SyntaxKind.FieldDeclaration);
            
            // [Internal] This rule flags public properties with public accessor modifiers internally.
            context.RegisterSyntaxNodeAction(AnalyzePublicPropertyDeclaration, SyntaxKind.PropertyDeclaration);
            
            // [External] This rule flags Methods at the callsite, if they don't use an accessor with a lock.
            context.RegisterSyntaxNodeAction(AnalyzeCallingMemberAccessWithLock, SyntaxKind.SimpleMemberAccessExpression);
            
            // [Internal] This rule flags fields internally, if they have public accessors without synchronization.
            context.RegisterSyntaxNodeAction(AnalyzeInternalFieldAccessWithLock, SyntaxKind.FieldDeclaration);
            
            // [Internal] Finds all locks in a namedType (a class) that are exposed through public accessor.
            context.RegisterSymbolAction(AnalyzeExposedClassLocks, SymbolKind.NamedType);
            
            
        }
        
        // -------------------------------------------------------------------------
        // Internal: LockObjectExposed through public Accessor
        // -------------------------------------------------------------------------
        private static void AnalyzeExposedClassLocks(SymbolAnalysisContext context)
        {
            if (!AnalyzerUtils.IsInThreadSafeClass(context)) return;
            
            var classSymbol = (INamedTypeSymbol)context.Symbol;
            if (classSymbol.TypeKind != TypeKind.Class) return;

            var firstRef = classSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (firstRef == null) return;
            var semanticModel = context.Compilation.GetSemanticModel(firstRef.SyntaxTree);
            
            // 1. Get the map of symbols actually used for locking
            var lockMap = AnalyzerUtils.GetClassLockAssociationDict(classSymbol, semanticModel);
            var allLockSymbols = lockMap.Keys.ToImmutableHashSet(SymbolEqualityComparer.Default);

            if (allLockSymbols.IsEmpty) return;

            // 2. Broaden the search: Check EVERY member of the class
            foreach (var member in classSymbol.GetMembers())
            {
                // ALLOW Methods (GetSyncObject) and Properties
                if (!(member is IMethodSymbol) && !(member is IPropertySymbol)) continue;
    
                // Only flag if the member is Public
                if (member.DeclaredAccessibility != Accessibility.Public) continue;

                var b1 = true;
                
                foreach (var syntaxRef in member.DeclaringSyntaxReferences)
                {
                    var memberSyntax = syntaxRef.GetSyntax();

                    // 3. Find all potential exit points (Returns and Arrows)
                    var returns = memberSyntax.DescendantNodes().OfType<ReturnStatementSyntax>();
                    var arrows = memberSyntax.DescendantNodes().OfType<ArrowExpressionClauseSyntax>();
                        
                    foreach (var ret in returns)
                    {
                        if (ret.Expression == null) continue;
                        CheckAndReportLockObjectExposedRule(context, semanticModel, ret.Expression, allLockSymbols, member, classSymbol.Name);
                    }
                        
                    foreach (var arrow in arrows)
                    {
                        CheckAndReportLockObjectExposedRule(context, semanticModel, arrow.Expression, allLockSymbols, member, classSymbol.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Helper func for AnalyzeExposedClassLocks() to verify if an expression resolves to one of our forbidden lock symbols.
        /// </summary>
        private static void CheckAndReportLockObjectExposedRule(
            SymbolAnalysisContext context, 
            SemanticModel semanticModel, 
            ExpressionSyntax expression, 
            IImmutableSet<ISymbol> lockSymbols,
            ISymbol member,
            string className)
        {
            var returnedSymbol = semanticModel.GetSymbolInfo(expression).Symbol;

            if (returnedSymbol == null || !lockSymbols.Contains(returnedSymbol)) return;
            
            var diagnostic = Diagnostic.Create(
                LockObjectExposedRule, 
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
            // Guard: Only run if annotated with: [ThreadSafe]
            if (!AnalyzerUtils.IsInThreadSafeClass(context)) return;
            
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol;
            
            if (!(symbol is IFieldSymbol) && !(symbol is IPropertySymbol)) return;
            
            var isInSource = symbol.Locations.FirstOrDefault().IsInSource;
            if (!isInSource) return;
            
            var containingType = symbol.ContainingType;
            var accessContainingType = context.ContainingSymbol?.ContainingType;

            // Guard: Skip members in Enums
            if (containingType?.TypeKind == TypeKind.Enum) return;
            
            // Only warn if accessed from outside the declaring type
            if (accessContainingType != null &&
                SymbolEqualityComparer.Default.Equals(containingType, accessContainingType))
                return;
                
            var diagnostic = Diagnostic.Create(
                FieldAccessedExternallyRule,
                memberAccess.Name.GetLocation(),
                $"{symbol.Name} is in source: {symbol.Locations[0].IsInSource} is in metadata {symbol.Locations[0].IsInMetadata}",
                containingType.Name);
            
            context.ReportDiagnostic(diagnostic);

            // Optionally, get the declaring syntax for more precise location
            var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null) return;
                
            var syntax = syntaxRef.GetSyntax(context.CancellationToken);
            var location = syntax.GetLocation();

            // Now report the diagnostic at the precise declaration location
            var declarationDiagnostic = Diagnostic.Create(
                FieldAccessedExternallyRule,
                location,
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

            if (!AnalyzerUtils.IsTargetInThreadSafeClass(symbol)) return;
            
            var memberName = memberAccess.Name.Identifier.Text;
            var className = context.ContainingSymbol?.ContainingType;

            // Only run this logic for Methods
            if (!(context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol is IMethodSymbol methodSymbol)) return;
            
            // Only for source code files (not SDK Libs)
            var isInSource = methodSymbol.Locations.FirstOrDefault().IsInSource;
            if (!isInSource) return;
            
            if (!(methodSymbol.ContainingSymbol is INamedTypeSymbol)) return;
            
            var parentLock = AnalyzerUtils.FindFirstLockFromMethodSymbol(methodSymbol);
            
            // Pt 'dumb' only knows if a Method call has a lock somewhere inside before a method, prop or class boundary is hit
            if (parentLock != null) return;
            
            // No lock found at all!
            var diagnostic = Diagnostic.Create(
                FieldDoesNotUseLockRule,
                memberAccess.Name.GetLocation(),
                memberName,
                className);

            context.ReportDiagnostic(diagnostic);
        }
        
        
        
        /// <summary>
        /// Analyses if a field in a source class can be accessed through any field usages without a lock. Warning is displayed internally in the class.
        /// </summary>
        /// <param name="context"> A FieldDeclarationSyntax node from the root analysis context </param> 
        private static void AnalyzeInternalFieldAccessWithLock(SyntaxNodeAnalysisContext context)
        {
            // Guard Clause: Only run if annotated with: [ThreadSafe]
            if (!AnalyzerUtils.IsInThreadSafeClass(context)) return;
            
            // 1. Find the Field
            var fieldDecl = (FieldDeclarationSyntax)context.Node;
            var className = context.ContainingSymbol?.ContainingType.ContainingSymbol;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!AnalyzerUtils.IsFieldOrPropParentAClass(context))
                return;

            // Get First variable (i.e. 'public int a, b, c' is not allowed)
            var variableDeclaration = AnalyzerUtils.GetFirstVariableInFieldDeclaration(fieldDecl);
                
            var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variableDeclaration, context.CancellationToken) as IFieldSymbol;
            if (fieldSymbol == null) return;
            
            // Const and readonly fields are thread-safe for reading
            if (fieldSymbol.IsConst || fieldSymbol.IsReadOnly) return;
            
            // Get the class containing the field
            // var root = context.SemanticModel.SyntaxTree.GetRoot();
            var classDecl = fieldDecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDecl == null) return;

            // Look for all identifier names in this class
            var fieldUsages = classDecl.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(id => {
                    var symbol = context.SemanticModel.GetSymbolInfo(id).Symbol;
                    return SymbolEqualityComparer.Default.Equals(symbol, fieldSymbol);
                });
            
            foreach (var usage in fieldUsages)
            {
                // Is there a surrounding lock
                var enclosingLock = usage.Ancestors()
                    .OfType<LockStatementSyntax>()
                    .FirstOrDefault();
                
                var incriminatingMethod = usage.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();
                
                if (incriminatingMethod == null) continue;
                
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(incriminatingMethod);
                if (methodSymbol?.DeclaredAccessibility != Accessibility.Public) continue;

                if (enclosingLock != null) continue;
                
                // Is it a constructor
                var isInsideConstructor = usage.Ancestors()
                    .OfType<ConstructorDeclarationSyntax>().Any();
                
                if (isInsideConstructor) continue;
                
                // Is the field being returned or written to directly
                var isReturned = usage.Ancestors().OfType<ReturnStatementSyntax>().Any();
                var isWrittenTo = AnalyzerUtils.IsWriteAccess(usage);
                
                if (!isReturned && !isWrittenTo) continue;
                    
                // REPORT: Field access is not protected!
                var diagnostic = Diagnostic.Create(
                    InternalFieldNoLockRule,
                    usage.GetLocation(),
                    fieldSymbol.Name, // Internal field name
                    className,                         // Declaring class name
                    incriminatingMethod.Identifier     // Method name
                );

                context.ReportDiagnostic(diagnostic);
            }
        }

        // -------------------------------------------------------------------------
        // PublicFieldExposed — detects raw public fields (not properties)
        // -------------------------------------------------------------------------
        private static void AnalyzePublicFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Guard Clause: Only run if annotated with: [ThreadSafe]
            if (!AnalyzerUtils.IsInThreadSafeClass(context)) return;
            
            var fieldDecl = (FieldDeclarationSyntax)context.Node;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!AnalyzerUtils.IsFieldOrPropParentAClass(context))
                return;
            
            // Only care about public fields
            if (!fieldDecl.Modifiers.Any(SyntaxKind.PublicKeyword))
                return;

            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol;
                if (symbol == null) continue;

                // Constants and static readonly fields are generally acceptable
                if (symbol.IsConst || (symbol.IsStatic && symbol.IsReadOnly))
                    continue;

                var detail = symbol.IsReadOnly
                    ? "Field is readonly — consider exposing via a read-only property."
                    : "Field has no accessor control. Consider wrapping it in a property with a restricted setter.";

                var diagnostic = Diagnostic.Create(
                    PublicFieldExposedRule,
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
            if (!AnalyzerUtils.IsInThreadSafeClass(context)) return;
            
            var propDecl = (PropertyDeclarationSyntax)context.Node;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!AnalyzerUtils.IsFieldOrPropParentAClass(context))
                return;
            
            // Only care about public properties
            if (!propDecl.Modifiers.Any(SyntaxKind.PublicKeyword))
                return;

            if (propDecl.AccessorList == null)
                return; // expression-bodied property — read-only by nature, skip

            var accessors = propDecl.AccessorList.Accessors;

            foreach (var accessor in accessors)
            {
                var isSetter = accessor.IsKind(SyntaxKind.SetAccessorDeclaration);
                var isIniter = accessor.IsKind(SyntaxKind.InitAccessorDeclaration);

                if (!isSetter && !isIniter)
                    continue;

                // Auto-generated accessor: no body and no modifiers
                var isAutoGenerated = accessor.Body == null && accessor.ExpressionBody == null;
                var hasNoModifier = !accessor.Modifiers.Any();

                if (isAutoGenerated && hasNoModifier)
                {
                    // Public property with auto-generated public setter — fully open
                    var diagnostic = Diagnostic.Create(
                        PublicFieldExposedRule,
                        accessor.GetLocation(),
                        propDecl.Identifier.Text,
                        "Property has an auto-generated public setter with no access restriction (e.g. 'private set' or 'protected set').");

                    context.ReportDiagnostic(diagnostic);
                }
                else if (!hasNoModifier)
                {
                    // Accessor has an explicit modifier — report it informatively
                    var modifierText = string.Join(" ", accessor.Modifiers.Select(m => m.Text));
                    var accessorKind = isSetter ? "setter" : "init accessor";

                    var diagnostic = Diagnostic.Create(
                        PublicFieldExposedRule,
                        accessor.GetLocation(),
                        propDecl.Identifier.Text,
                        $"Property {accessorKind} is explicitly marked '{modifierText}'.");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
