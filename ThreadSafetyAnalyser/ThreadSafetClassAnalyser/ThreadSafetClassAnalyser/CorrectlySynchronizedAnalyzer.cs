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
            // context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            
            context.RegisterSyntaxNodeAction(AnalyzeConflictingAccessesInThreads, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeConflictingAccessesInThreads(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            
            // Check that class was found
            if (classSymbol == null) 
                return;

            // Guard for the [ThreadSafe] annotation
            if (!AnalysisHelpers.IsTargetInThreadSafeClass(classSymbol))
                return;
            
            // Find all thread instantiations in a class
            var threadCreations = AnalysisHelpers.GetThreadCreationsInClass(context, semanticModel);
            if (threadCreations is null) return;
            
            if (threadCreations.Count < 2) return;
            
            // Map each thread to the fields it accesses
            var threadAccessMaps = threadCreations
                .Select(tc => AnalysisHelpers.GetAccessedFields(tc, semanticModel))
                .ToList();
            
            // 4. Compare every thread against every other thread 
            for (var i = 0; i < threadAccessMaps.Count; i++)
            {
                for (var j = i + 1; j < threadAccessMaps.Count; j++)
                {
                    var conflicts = AnalysisHelpers.FindConflicts(threadAccessMaps[i], threadAccessMaps[j]);
            
                    var nameI = AnalysisHelpers.GetThreadName(threadCreations[i]);
                    var nameJ = AnalysisHelpers.GetThreadName(threadCreations[j]);
                    
                    foreach (var conflict in conflicts)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConflictingAccessThreadRule,
                            threadCreations[i].GetLocation(), 
                            conflict.Name,
                            nameI,
                            nameJ));
                        
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConflictingAccessThreadRule,
                            threadCreations[j].GetLocation(), 
                            conflict.Name,
                            nameJ,
                            nameI));
                    }
                }
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
                .SelectMany(f => f.Declaration.Variables)
                .Select(v => semanticModel.GetDeclaredSymbol(v, context.CancellationToken))
                .OfType<IFieldSymbol>()
                .ToImmutableHashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
            
            // Collect all property symbols in the class
            var properties = classDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => semanticModel.GetDeclaredSymbol(p, context.CancellationToken))
                .OfType<IPropertySymbol>()
                .ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default);


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
