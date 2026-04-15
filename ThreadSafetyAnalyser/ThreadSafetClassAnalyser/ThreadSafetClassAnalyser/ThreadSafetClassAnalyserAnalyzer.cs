using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThreadSafetClassAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ThreadSafetClassAnalyserAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Naming";
        public const string DiagnosticId = "ThreadSafetClassAnalyser";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public const string FieldUsedInMethodDiagnosticId = "FieldUsedInMethod";
        private static readonly LocalizableString FieldUsedTitle = new LocalizableResourceString(nameof(Resources.FieldUsedTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldUsedMessageFormat = new LocalizableResourceString(nameof(Resources.FieldUsedMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString FieldUsedDescription = new LocalizableResourceString(nameof(Resources.FieldUsedDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor FieldUsedRule = new DiagnosticDescriptor(FieldUsedInMethodDiagnosticId, FieldUsedTitle, FieldUsedMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: FieldUsedDescription);

        //Add your rules here.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(
            Rule, 
            FieldUsedRule); 
            } 
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //Register the Actions here.
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        //When looking for what to register against it is veryhelpful to make use of the "Syntax Visualizer" in the "View"->"Other Windows"->"Syntax Visualizer".
        //Or Ctrl+Shift+P and search for the term.

        //From Template, Checks every Symbol in the project. If the Name has any lowercase letters, it produces a Diagnostic.
        //The important part is that it is linked to the rule above to showcase what the rule or message should be for such errors.
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
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

            //For debugging
            var fields = string.Join(", ", fieldSymbols.Select(f => f.Name));
            var props = string.Join(", ", properties.Select(p => p.Name));

            //Get all method declarations in the class
            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();

            foreach (var methodDecl in methodDeclarations)
            {
                if (methodDecl.Body == null)
                    continue;

                // Find all IdentifierNameSyntax nodes in the method body
                var identifierNames = methodDecl.Body.DescendantNodes().OfType<IdentifierNameSyntax>();
                foreach (var identifierName in identifierNames)
                {
                    //Insert som logic here that tries to point back to the symbols and create a diagnostic for those aswell.
                    //So we get a warning both in the method but also on the field / property declaration. That points to the place where there is a misuse.

                    //If that is possible introduce logic that if it is contained in a lock statement, it is not a problem. So we can exclude those cases.
                    var diagnostic = Diagnostic.Create(
                            FieldUsedRule,
                            identifierName.GetLocation(),
                            $"{identifierName.Identifier.ValueText} (Fields: {fields} Properties: {props}",
                            methodDecl.Identifier.Text);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
