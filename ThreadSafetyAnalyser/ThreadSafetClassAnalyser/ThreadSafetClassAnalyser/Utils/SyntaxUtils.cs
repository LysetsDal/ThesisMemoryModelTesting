using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThreadSafetClassAnalyser.Utils
{
    /// <summary>
    /// Pure structural utils that inspect basic syntax properties, accessibility, attributes, or names.
    /// </summary>
    public static class SyntaxUtils
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
        /// Determines whether a given <see cref="ISymbol"/> represents a mutable instance/static field or property.
        /// </summary>
        /// <param name="symbol">The symbol to evaluate.</param>
        /// <returns>
        /// <see langword="true"/> if the symbol is a writable field (not read-only or constant) 
        /// or a writable property; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsMutableFieldOrProperty(ISymbol symbol)
        {
            if (symbol is IFieldSymbol fieldSymbol)
            {
                return !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst;
            }
            if (symbol is IPropertySymbol propertySymbol)
            {
                return !propertySymbol.IsReadOnly && propertySymbol.SetMethod != null;
            }
            return false;
        }
        
        public static bool IsMethodPrivate(IMethodSymbol method)
        {
            if (method == null)
                return true;
            
            return method.DeclaredAccessibility != Accessibility.Public;
        }
        
        public static string GetThreadName(ObjectCreationExpressionSyntax creation)
        {
            if (creation.Parent is EqualsValueClauseSyntax evc && evc.Parent is VariableDeclaratorSyntax vds)
            {
                return vds.Identifier.Text;
            }
            return "Anonymous Thread";
        }
        
                
        /// <summary>
        /// Helper to resolve a name for the current code block context.
        /// </summary>
        public static string GetBodyName(SyntaxNode body)
        {
            if (body is MethodDeclarationSyntax method) 
                return method.Identifier.Text;

            if (!(body is AccessorDeclarationSyntax accessor)) return "anonymous method";
            var prop = accessor.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            var propName = prop?.Identifier.Text ?? "UnknownProperty";
            return $"{(accessor.Kind() == SyntaxKind.GetAccessorDeclaration ? "get" : "set")} of {propName}";
        }
    }
}