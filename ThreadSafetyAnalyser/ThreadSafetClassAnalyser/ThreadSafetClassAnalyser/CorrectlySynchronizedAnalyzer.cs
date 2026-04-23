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
    public class CorrectlySynchronizedAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "CorrectlySynchronized";

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
        
        //Add your rules here.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            [DebuggerStepThrough()]
            get
            {
                return ImmutableArray.Create(
                    FieldUsedRule
                );
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register the Actions here.
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
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

    }
}
