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
        private const bool UseAnnotation = false;

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
            if (UseAnnotation && !AnalysisHelpers.IsTargetInThreadSafeClass(classSymbol))
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
                    var conflicts = 
                        AnalysisHelpers.FindConflicts(threadAccessMaps[i], threadAccessMaps[j]);
                    
                    var symbolI = semanticModel.GetSymbolInfo(threadCreations[i].ArgumentList.Arguments[0].Expression).Symbol;
                    var symbolJ = semanticModel.GetSymbolInfo(threadCreations[j].ArgumentList.Arguments[0].Expression).Symbol;
                    
                    var classLocks =
                        AnalysisHelpers.GetClassLockAssociationDict(classSymbol, semanticModel);

                    var locksUsedByI = AnalysisHelpers.GetLockObjectsUsedInMember(classLocks, symbolI);
                    var locksUsedByJ = AnalysisHelpers.GetLockObjectsUsedInMember(classLocks, symbolJ);

                    // 5. Check for a common lock object (The Intersection)
                    var sharesCommonLock = locksUsedByI.Intersect(locksUsedByJ, SymbolEqualityComparer.Default).Any();

                    // If they share a lock, they are synchronized. Skip reporting diagnostics for this pair.
                    if (sharesCommonLock) continue;
                    
                    var nameI = AnalysisHelpers.GetThreadName(threadCreations[i]);
                    var nameJ = AnalysisHelpers.GetThreadName(threadCreations[j]);

                    var stop = true;
                    foreach (var conflict in conflicts)
                    {
                        var infoI = threadAccessMaps[i][conflict];
                        var infoJ = threadAccessMaps[j][conflict];
                        
                        if (infoI.LockObject != null && 
                            infoJ.LockObject != null && 
                            SymbolEqualityComparer.Default.Equals(infoI.LockObject, infoJ.LockObject))
                        {
                            continue; 
                        }
                        
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
    }
}
