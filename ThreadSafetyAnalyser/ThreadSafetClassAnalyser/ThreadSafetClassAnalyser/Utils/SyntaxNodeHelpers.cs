using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;


namespace ThreadSafetClassAnalyser.Utils
{
    public static class SyntaxNodeHelpers
    {
        /// <summary>
        /// Helper method that determines if a field, prop or other member belongs to a class.
        /// </summary>
        /// <param name="ctx">
        /// The current context from a Syntax Node 
        /// </param>
        public static bool IsFieldOrPropParentAClass(SyntaxNodeAnalysisContext ctx)
        {
            // ContainingSymbol refers to the field/property itself
            // ContainingType refers to the class/struct/interface it lives in
            var containingType = ctx.ContainingSymbol?.ContainingType;
            
            return containingType != null && containingType.TypeKind == TypeKind.Class;
        }
        
        /// <summary>
        /// Helper method that walks up the syntax tree, to determine if a SyntaxNode's parent is surrounded by a lock. Stops if we hit a method or class boundary. 
        /// </summary>
        /// <param name="node">
        /// The entry point / point of access in the tree we want to walk up from. 
        /// </param>
        /// <returns>
        /// The Surrounding paramLockStatementSyntax or null (if nothing was found before a method/class boundary) 
        /// </returns>
        public static LockStatementSyntax GetParentLockFromSyntaxNode(SyntaxNode node)
        {
            var current = node.Parent;

            while (current != null)
            {
                // If we find a lock, return it
                if (current is LockStatementSyntax lockStatement)
                    return lockStatement;

                // Optimization: Stop searching if we exit the method or property body
                if (current is MethodDeclarationSyntax || 
                    current is AccessorDeclarationSyntax || 
                    current is ConstructorDeclarationSyntax)
                    break;

                current = current.Parent;
            }

            return null;
        }
        
        
        /// <summary>
        /// Looks ar a method outside -> in, returns the first sorrounding lock it finds inside the method.
        /// </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static LockStatementSyntax FindSurroundingLockFromMethodSymbol(ISymbol methodSymbol)
        {
            var containingMethodSyntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();

            LockStatementSyntax parentLock = null;
            if (containingMethodSyntaxRef != null)
            {
                var methodDecl = containingMethodSyntaxRef.GetSyntax();
                parentLock = methodDecl.DescendantNodes()
                    .OfType<LockStatementSyntax>()
                    .FirstOrDefault();
            }

            return parentLock;
        }
        
        /// <summary>
        /// Finds the first enclosing lock (inside -> out) from a Syntax node
        /// </summary>
        /// <param name="node"></param> the node to traverse up the tree from.
        /// <returns></returns>
        public static LockStatementSyntax GetEnclosingLock(SyntaxNode node)
        {
            // .Ancestors() walks UP the tree from the current node to the Root
            return node.Ancestors().OfType<LockStatementSyntax>().FirstOrDefault();
        }
        
        /// <summary>
        /// Helper method that retrieves the symbol inside the lock statement to find out what object is being used to lock. E.g. in 'lock(_syncLock)', it returns the symbol _syncLock.
        /// </summary>
        /// <param name="lockStatement">
        /// A lock stamtnet from the Syntax Tree
        /// </param>
        /// /// <param name="semanticModel">
        /// The semantic model that the lockStatement exists in.
        /// </param>
        /// <returns>
        /// The Symbol (ISymbol) of the object used inside the lock stament 
        /// </returns>
        /// <remarks>
        /// If you encounter an 'System.ArgumentException: Syntax node is not within syntax tree', you are passing the wrong semanticModel.
        /// </remarks>
        public static ISymbol GetLockObjectSymbol(LockStatementSyntax lockStatement, SemanticModel semanticModel)
        {
            if (lockStatement == null) return null;

            // The 'Expression' is what is inside the parentheses: lock(expression)
            var lockExpression = lockStatement.Expression;
    
            // Get the symbol (Field, Property, or Local Variable)
            var symbolInfo = semanticModel.GetSymbolInfo(lockExpression);
            return symbolInfo.Symbol;
        }
    }
}