using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ThreadSafetClassAnalyser.Model;

namespace ThreadSafetClassAnalyser.Utils
{
    /// <summary>
    /// Utils for extracting information about where locks are declared in a class, and mapping which
    /// methods or expressions are encapsulated by those locks.
    /// </summary>
    public static class LockAssociationUtils
    {
        /// <summary>
        /// Scans a class declaration and returns a <see cref="ClassLocks"/> collection mapping lock targets to their members.
        /// </summary>
        /// <param name="classSymbol">The Symbol of the class to find locks in.</param>
        /// <param name="semanticModel">The Semantic model of the classSymbol.</param>
        /// <returns>A structured <see cref="ClassLocks"/> wrapper mapping.</returns>
        public static ClassLocks GetClassLocks(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
        {
            var lockMapping = new Dictionary<ISymbol, List<LockAssociation>>(SymbolEqualityComparer.Default);
            var currentTree = semanticModel.SyntaxTree;

            foreach (var location in classSymbol.DeclaringSyntaxReferences)
            {
                if (location.SyntaxTree != currentTree) continue;

                var classSyntax = location.GetSyntax() as ClassDeclarationSyntax;
                if (classSyntax == null) continue;

                var allLocks = classSyntax.DescendantNodes().OfType<LockStatementSyntax>();

                foreach (var lockStmt in allLocks)
                {
                    ISymbol lockObjSymbol;
                    try
                    {
                        lockObjSymbol = semanticModel.GetSymbolInfo(lockStmt.Expression).Symbol;
                    }
                    catch (ArgumentException ex)
                    {
                        var lockExprText = lockStmt.Expression.ToString();
                        var lockLocation = lockStmt.GetLocation();
                        var syntaxTreePath = lockStmt.SyntaxTree.FilePath;
                        var modelTreePath = semanticModel.SyntaxTree.FilePath;
                        var treeMatch = lockStmt.SyntaxTree == semanticModel.SyntaxTree;

                        throw new InvalidOperationException(
                            $@"
                            [GetClassLocks] SemanticModel/SyntaxTree mismatch while resolving lock expression.
            
                              Class       : {classSymbol.ToDisplayString()}
                              Lock expr   : {lockExprText}
                              Lock at     : {lockLocation.GetLineSpan()}
                              Node tree   : {syntaxTreePath}
                              Model tree  : {modelTreePath}
                              Trees match : {treeMatch}
                            ", ex);
                    }
                    if (lockObjSymbol == null) continue;

                    var enclosingMember = lockStmt.Ancestors()
                        .FirstOrDefault(a => a is MemberDeclarationSyntax || a is AccessorDeclarationSyntax || a is LambdaExpressionSyntax);

                    ISymbol memberSymbol = null;
                    if (enclosingMember != null)
                    {
                        memberSymbol = (enclosingMember is LambdaExpressionSyntax lambda)
                            ? semanticModel.GetSymbolInfo(lambda).Symbol
                            : semanticModel.GetDeclaredSymbol(enclosingMember);
                    }
            
                    if (!lockMapping.ContainsKey(lockObjSymbol))
                    {
                        lockMapping[lockObjSymbol] = new List<LockAssociation>();
                    }
            
                    lockMapping[lockObjSymbol].Add(new LockAssociation(memberSymbol, lockStmt));
                }
            }
    
            var immutableMap = lockMapping.ToImmutableDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.ToImmutableArray(), 
                SymbolEqualityComparer.Default);

            return new ClassLocks(immutableMap);
        }
        
        /// <summary>
        /// Looks at a method and returns the first lock it finds inside the method. Single layer, doesn't recurse.
        /// </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static LockStatementSyntax FindFirstDescendantLockFromMethodSymbol(ISymbol methodSymbol)
        {
            var containingMethodSyntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();

            if (containingMethodSyntaxRef == null) return null;
            var methodDecl = containingMethodSyntaxRef.GetSyntax();
            var parentLock = methodDecl.DescendantNodes()
                .OfType<LockStatementSyntax>()
                .FirstOrDefault();

            return parentLock;
        }
                
        /// <summary>
        /// Searches upwards through the syntax tree from the specified node to find the 
        /// nearest enclosing <see cref="LockStatementSyntax"/>. If found, it retrieves 
        /// the symbol of the object used as the lock target.
        /// </summary>
        /// <param name="node">The syntax node from which to begin searching upwards.</param>
        /// <param name="model">The semantic model used to resolve the symbol of the lock expression.</param>
        /// <returns>
        /// The <see cref="ISymbol"/> representing the object being locked e.g. 'object _lock = new()', 
        /// or <c>null</c> if the node is not contained within a lock statement or the symbol cannot be resolved.
        /// </returns>
        public static ISymbol GetFirstAncestorLockFromSymbol(SyntaxNode node, SemanticModel model)
        {
            var lockStmt = node
                .Ancestors()
                .OfType<LockStatementSyntax>()
                .FirstOrDefault();
            
            return lockStmt == null ? null :
                // Get the symbol of what is being locked (e.g., _syncObj)
                model.GetSymbolInfo(lockStmt.Expression).Symbol;
        }
        
        public static ISymbol GetEnclosingLockSymbol(SyntaxNode node, SemanticModel model)
        {
            var lockStatement = node.Ancestors().OfType<LockStatementSyntax>().FirstOrDefault();
            return lockStatement == null ? null : model.GetSymbolInfo(lockStatement.Expression).Symbol;
        }
        
    }
}