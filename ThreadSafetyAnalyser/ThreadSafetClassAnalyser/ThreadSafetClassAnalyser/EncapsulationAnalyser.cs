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
                    FieldAccessedExternallyRule
                ); 
            } 
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register the Actions here.
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
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
