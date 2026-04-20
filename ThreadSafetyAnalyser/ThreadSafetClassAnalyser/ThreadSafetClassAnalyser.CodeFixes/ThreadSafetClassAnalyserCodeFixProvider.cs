using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ThreadSafetClassAnalyser
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThreadSafetClassAnalyserCodeFixProvider)), Shared]
    public class ThreadSafetClassAnalyserCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ThreadSafetClassAnalyserAnalyzer.DiagnosticId,
                EncapsulationAnalyser.FieldUsedInMethodDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            if (diagnostic.Id == ThreadSafetClassAnalyserAnalyzer.DiagnosticId)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitle,
                        createChangedSolution: c => MakeUppercaseAsync(context.Document, declaration, c),
                        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                    diagnostic);
            }
            else if (diagnostic.Id == EncapsulationAnalyser.FieldUsedInMethodDiagnosticId)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.MethodCheck,
                        createChangedSolution: c => WarnIfFieldUsedInMethodAsync(context.Document, declaration, c),
                        equivalenceKey: nameof(CodeFixResources.MethodCheck)),
                    diagnostic);
            }
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }

        private async Task<Solution> WarnIfFieldUsedInMethodAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Find all field declarations in the class
            var fieldDeclarations = typeDecl.Members.OfType<FieldDeclarationSyntax>().ToList();

            // Find all method declarations in the class
            var methodDeclarations = typeDecl.Members.OfType<MethodDeclarationSyntax>().ToList();

            // For each field, check if it is used in any method
            foreach (var fieldDecl in fieldDeclarations)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                    if (fieldSymbol == null)
                        continue;

                    foreach (var methodDecl in methodDeclarations)
                    {
                        var dataFlow = semanticModel.AnalyzeDataFlow(methodDecl.Body);
                        if (dataFlow.ReadInside.Contains(fieldSymbol) || dataFlow.WrittenInside.Contains(fieldSymbol))
                        {
                            // Here you would trigger a warning or register a code fix.
                            // For now, just break out as a placeholder.
                            // You can later add logic to modify the solution or annotate the code.
                            break;
                        }
                    }
                }
            }

            // For now, return the original solution (no changes made)
            return document.Project.Solution;
        }
    }
}
