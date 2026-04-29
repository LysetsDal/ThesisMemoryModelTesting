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

        private static readonly DiagnosticDescriptor PublicFieldExposedRule =
            new DiagnosticDescriptor(
                PublicFieldExposedDiagnosticId,
                title: "Public field exposes internal state",
                messageFormat: "Field '{0}' is public. {1}",
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: "Public fields break encapsulation. Use properties with appropriate accessor modifiers instead.");

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
        
        
        // --- Register all supported diagnostics ---
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
            [DebuggerStepThrough()]
            get =>
                ImmutableArray.Create(
                    FieldAccessedExternallyRule,
                    PublicFieldExposedRule,
                    FieldDoesNotUseLockRule
                );
        }
        
        // What events should Roslyn analyser listen to:
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
            
            // Test: 
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessWithLock, SyntaxKind.SimpleMemberAccessExpression);
        }

        // -------------------------------------------------------------------------
        // Existing: FieldAccessedExternally
        // -------------------------------------------------------------------------
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol;

            if (symbol is IFieldSymbol || symbol is IPropertySymbol)
            {
                var containingType = symbol.ContainingType;
                var accessContainingType = context.ContainingSymbol?.ContainingType;

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
        }
        // -------------------------------------------------------------------------
        // Existing: FieldAccessedExternally (WithoutALock)
        // -------------------------------------------------------------------------
        private static void AnalyzeMemberAccessWithLock(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var memberName = memberAccess.Name.Identifier.Text;
            var className = context.ContainingSymbol?.ContainingType;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol;
            // Only run this logic for Methods
            if (!(methodSymbol is IMethodSymbol))
                return;
            
            var isInSource = methodSymbol.Locations.FirstOrDefault().IsInSource;
            if (!isInSource) return;
            
            if (!(methodSymbol.ContainingSymbol is INamedTypeSymbol))
                return;
            
            LockStatementSyntax parentLock =
                SyntaxNodeHelpers.FindSurroundingLockFromMethodSymbol(methodSymbol);

            
            // Pt 'dumb' only knows if a Method call has a lock somewhere inside before a method, prop or class boundary is hit
            if (parentLock == null)
            {
                // No lock found at all!
                var diagnostic = Diagnostic.Create(
                    FieldDoesNotUseLockRule,
                    memberAccess.Name.GetLocation(),
                    memberName,
                    className
                    );

                context.ReportDiagnostic(diagnostic);
                return;
            }
            
            // Get the semantic model specifically for the tree containing the parentLock
            var definitionModel = context.Compilation.GetSemanticModel(parentLock.SyntaxTree);
            
            // 2. Check if it's the RIGHT lock?
        }

        // -------------------------------------------------------------------------
        // PublicFieldExposed — detects raw public fields (not properties)
        // -------------------------------------------------------------------------
        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDecl = (FieldDeclarationSyntax)context.Node;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!SyntaxNodeHelpers.IsFieldOrPropParentAClass(context))
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
        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propDecl = (PropertyDeclarationSyntax)context.Node;
            
            // Rule does not apply to Interfaces, Records or Structs
            if (!SyntaxNodeHelpers.IsFieldOrPropParentAClass(context))
                return;
            
            // Only care about public properties
            if (!propDecl.Modifiers.Any(SyntaxKind.PublicKeyword))
                return;

            if (propDecl.AccessorList == null)
                return; // expression-bodied property — read-only by nature, skip

            var accessors = propDecl.AccessorList.Accessors;

            foreach (var accessor in accessors)
            {
                bool isSetter = accessor.IsKind(SyntaxKind.SetAccessorDeclaration);
                bool isIniter = accessor.IsKind(SyntaxKind.InitAccessorDeclaration);

                if (!isSetter && !isIniter)
                    continue;

                // Auto-generated accessor: no body and no modifiers
                bool isAutoGenerated = accessor.Body == null && accessor.ExpressionBody == null;
                bool hasNoModifier = !accessor.Modifiers.Any();

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
        // -------------------------------------------------------------------------
        // 
        // -------------------------------------------------------------------------
        private static void AnalyseReadonlyClassMember(SyntaxNodeAnalysisContext ctx)
        {
            // 1. Get the symbol from the node (this is the 'meaning' of the code)
            // var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ctx.CancellationToken);

            var classDeclaration = (ClassDeclarationSyntax)ctx.Node;

            var members = classDeclaration.Members;
            
            foreach (MemberDeclarationSyntax member in members)
            {
                var location = member.GetLocation();
                var type = member.GetType();
                var reference = member.GetReference();
            }
        }
    }
}
