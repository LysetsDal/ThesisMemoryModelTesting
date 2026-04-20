using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace ThreadSafetClassAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EncapsulationAnalyser : DiagnosticAnalyzer
    {
        private const string Category = "Encapsulation";

        public const string FieldUsedInMethodDiagnosticId = "FieldUsedInMethod";
        private static readonly LocalizableString FieldUsedTitle = new LocalizableResourceString(nameof(Resources.FieldUsedTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldUsedMessageFormat = new LocalizableResourceString(nameof(Resources.FieldUsedMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldUsedDescription = new LocalizableResourceString(nameof(Resources.FieldUsedDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor FieldUsedRule = 
            new DiagnosticDescriptor(
                FieldUsedInMethodDiagnosticId, 
                FieldUsedTitle, 
                FieldUsedMessageFormat, 
                Category, 
                DiagnosticSeverity.Warning, 
                isEnabledByDefault: true, 
                description: FieldUsedDescription);

        public const string FieldAccessedExternallyDiagnosticId = "FieldAccessedExternally";

        private static readonly LocalizableString FieldAccessedExternallyTitle = new LocalizableResourceString(nameof(Resources.FieldAccessedExternallyTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldAccessedExternallyMessageFormat = new LocalizableResourceString(nameof(Resources.FieldAccessedExternallyMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldAccessedExternallyDescription = new LocalizableResourceString(nameof(Resources.FieldAccessedExternallyDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor FieldAccessedExternallyRule =
            new DiagnosticDescriptor(
                FieldAccessedExternallyDiagnosticId,
                FieldAccessedExternallyTitle,
                FieldAccessedExternallyMessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: FieldAccessedExternallyDescription);

        //Add your rules here.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
            [DebuggerStepThrough()]
            get { 
                return ImmutableArray.Create(
                    FieldUsedRule,
                    FieldAccessedExternallyRule
                ); 
            } 
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register the Actions here.
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }
        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node; //Cast the node so it is workable.

            //Need this to get the symbols for the fields and properties in the class, so we can compare them to the identifiers used in the methods.
            var semanticModel = context.SemanticModel;

            // Collect all field symbols in the class
            var fieldSymbols = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables) //int a, b, c; // One FieldDeclarationSyntax, three VariableDeclaratorSyntax nodes
                .Select(v => semanticModel.GetDeclaredSymbol(v, context.CancellationToken))
                .OfType<IFieldSymbol>()
                .ToImmutableHashSet();
            // Collect all property symbols in the class
            var properties = classDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => semanticModel.GetDeclaredSymbol(p, context.CancellationToken))
                .OfType<IPropertySymbol>()
                .ToImmutableHashSet();


            //Get all method declarations in the class
            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();

            foreach (var methodDecl in methodDeclarations)
            {
                if (methodDecl.Body == null)
                    continue;

                var identifierNames = methodDecl.Body.DescendantNodes().OfType<IdentifierNameSyntax>();
                foreach (var identifierName in identifierNames)
                {
                    var symbol = semanticModel.GetSymbolInfo(identifierName, context.CancellationToken).Symbol;

                    // Check if the symbol is a field or property of this class
                    if (symbol is IFieldSymbol fieldSymbol && fieldSymbols.Contains(fieldSymbol))
                    {
                        // Diagnostic at usage
                        var usageDiagnostic = Diagnostic.Create(
                            FieldUsedRule,
                            identifierName.GetLocation(),
                            identifierName.Identifier.ValueText,
                            methodDecl.Identifier.Text);

                        context.ReportDiagnostic(usageDiagnostic);

                        // Diagnostic at declaration
                        var declarationDiagnostic = Diagnostic.Create(
                            FieldUsedRule,
                            fieldSymbol.Locations[0],
                            fieldSymbol.Name,
                            methodDecl.Identifier.Text);

                        context.ReportDiagnostic(declarationDiagnostic);
                    }
                    else if (symbol is IPropertySymbol propertySymbol && properties.Contains(propertySymbol))
                    {
                        // Diagnostic at usage
                        var usageDiagnostic = Diagnostic.Create(
                            FieldUsedRule,
                            identifierName.GetLocation(),
                            identifierName.Identifier.ValueText,
                            methodDecl.Identifier.Text);

                        context.ReportDiagnostic(usageDiagnostic);

                        // Diagnostic at declaration
                        var declarationDiagnostic = Diagnostic.Create(
                            FieldUsedRule,
                            propertySymbol.Locations[0],
                            propertySymbol.Name,
                            methodDecl.Identifier.Text);

                        context.ReportDiagnostic(declarationDiagnostic);
                    }
                }
            }
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name, context.CancellationToken).Symbol;

            if (symbol is IFieldSymbol || symbol is IPropertySymbol)
            {
                var containingType = symbol.ContainingType;
                var accessContainingType = context.ContainingSymbol?.ContainingType;

                // Only warn if accessed from outside the declaring type
                if (accessContainingType == null || !SymbolEqualityComparer.Default.Equals(containingType, accessContainingType))
                {
                    var diagnostic = Diagnostic.Create(
                        FieldAccessedExternallyRule,
                        memberAccess.Name.GetLocation(),
                        $"{symbol.Name} is in soruce: {symbol.Locations[0].IsInSource} is in metadata {symbol.Locations[0].IsInMetadata}",
                        containingType.Name);

                    context.ReportDiagnostic(diagnostic);

                    // Optionally, get the declaring syntax for more precise location
                    var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxRef != null)
                    {
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
            }
        }
    }
}
