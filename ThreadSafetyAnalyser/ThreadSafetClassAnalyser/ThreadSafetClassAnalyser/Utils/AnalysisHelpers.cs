using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;


namespace ThreadSafetClassAnalyser.Utils
{
    public static class AnalysisHelpers
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
        /// Determines if the current analysis context is within a class marked with [ThreadSafe].
        /// </summary>
        public static bool IsInThreadSafeClass(SyntaxNodeAnalysisContext context)
        {
            // For syntax actions, the ContainingSymbol's type is the class
            var classSymbol = context.ContainingSymbol?.ContainingType;
            return GetThreadSafeAttribute(classSymbol) != null;
        }

        /// <summary>
        /// Determines if the current symbol analysis context is for a class marked with [ThreadSafe].
        /// </summary>
        public static bool IsInThreadSafeClass(SymbolAnalysisContext context)
        {
            // For symbol actions, the Symbol itself is often the class (NamedType)
            var classSymbol = context.Symbol as INamedTypeSymbol ?? context.Symbol.ContainingType;
            return GetThreadSafeAttribute(classSymbol) != null;
        }
        
        /// <summary>
        /// Checks if a symbol's containing type (i.e. the class) is annotated with the [ThreadSafe] attribute.
        /// </summary>
        /// <param name="symbol">The symbol to inspect.</param>
        /// <returns>The AttributeData if found, otherwise null.</returns>
        public static AttributeData GetThreadSafeAttribute(ISymbol symbol)
        {
            if (symbol == null) return null;

            // If it's a member (field/method), get its containing type (the class)
            var namedType = symbol as INamedTypeSymbol ?? symbol.ContainingType;

            return namedType?.GetAttributes().FirstOrDefault(attr => 
                attr.AttributeClass?.ToDisplayString() == "Annotations.ThreadSafeAttribute");
        }

        /// <summary>
        /// Gets the first variable in a field declaration.
        /// </summary>
        /// <param name="fieldDecl"> The field declaration you want to get a variable name from</param>
        /// <returns> A variable declaration syntax context </returns>
        /// <remarks> If multiple symbol names are given (i.e. int a, b;) it will return the first one.</remarks>
        public static VariableDeclaratorSyntax GetFirstVariableInFieldDeclaration(FieldDeclarationSyntax fieldDecl)
        {
            return fieldDecl.Declaration.Variables.FirstOrDefault();
        }
        

        public static ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>> 
            GetClassLockAssociationDict(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
        {
            // Use the custom LockAssociation struct instead of Tuple
            var lockMapping = new Dictionary<ISymbol, List<LockAssociation>>(SymbolEqualityComparer.Default);
            
            foreach (var location in classSymbol.DeclaringSyntaxReferences)
            {
                var classSyntax = location.GetSyntax() as ClassDeclarationSyntax;
                if (classSyntax == null) continue;

                // Find every lock statement inside this class (handles partial classes via DeclaringSyntaxReferences)
                var allLocks = classSyntax.DescendantNodes().OfType<LockStatementSyntax>();

                foreach (var lockStmt in allLocks)
                {
                    // Determine WHAT is being locked (the expression inside the parentheses)
                    var lockObjSymbol = semanticModel.GetSymbolInfo(lockStmt.Expression).Symbol;
                    
                    // Determine the Enclosing Member (Method, Property Accessor, Constructor, etc.)
                    var enclosingMember = lockStmt.Ancestors()
                        .FirstOrDefault(a => a is MemberDeclarationSyntax || a is AccessorDeclarationSyntax);
                    
                    // Get the Symbol for the member containing the lock
                    ISymbol memberSymbol = null;
                    if (enclosingMember != null)
                    {
                        memberSymbol = semanticModel.GetDeclaredSymbol(enclosingMember);
                    }
                    
                    if (lockObjSymbol != null)
                    {
                        if (!lockMapping.ContainsKey(lockObjSymbol))
                        {
                            lockMapping[lockObjSymbol] = new List<LockAssociation>();
                        }
                        
                        // Use the primary constructor of your new struct
                        lockMapping[lockObjSymbol].Add(new LockAssociation(memberSymbol, lockStmt));
                    }
                }
            }
            
            // Convert the dictionary to an immutable version for safe analyzer use
            return lockMapping.ToImmutableDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.ToImmutableArray(), 
                SymbolEqualityComparer.Default);
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
            if (containingMethodSyntaxRef == null) return parentLock;
            var methodDecl = containingMethodSyntaxRef.GetSyntax();
            parentLock = methodDecl.DescendantNodes()
                .OfType<LockStatementSyntax>()
                .FirstOrDefault();

            return parentLock;
        }
        
        /// <summary>
        /// Finds the first enclosing lock from the current Syntax node  (inside -> out).
        /// </summary>
        /// <param name="node"></param> the symbol to traverse up the tree from.
        /// <returns></returns>
        public static LockStatementSyntax GetEnclosingLockFromCurrentNode(SyntaxNode node)
        {
            // .Ancestors() walks UP the tree from the current node to the Root
            var ancestors = node.Ancestors();
            var res = ancestors.OfType<LockStatementSyntax>().FirstOrDefault(); 
            return res;
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