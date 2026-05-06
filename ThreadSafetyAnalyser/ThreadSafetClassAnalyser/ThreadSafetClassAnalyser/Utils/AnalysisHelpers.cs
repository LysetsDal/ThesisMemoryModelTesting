using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using ThreadSafetClassAnalyser.Model;


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
        /// Used for methods registered with 'RegisterSyntaxNodeAction()' in the Initialize method.
        /// </summary>
        /// <param name="context">A SyntaxNodeAnalysisContext</param>
        public static bool IsInThreadSafeClass(SyntaxNodeAnalysisContext context)
        {
            // For syntax actions, the ContainingSymbol's type is the class
            var classSymbol = context.ContainingSymbol?.ContainingType;
            return GetThreadSafeAttribute(classSymbol) != null;
        }

        /// <summary>
        /// Determines if the current symbol analysis context is for a class marked with [ThreadSafe].
        /// Used for methods registered with 'RegisterSymbolAction()' and 'SymbolKind.NamedType' in the Initialize method.
        /// </summary>
        /// <param name="context">A SymbolAnalysisContext</param>
        public static bool IsInThreadSafeClass(SymbolAnalysisContext context)
        {
            // For symbol actions, the Symbol itself is often the class (NamedType)
            // var classSymbol = context.Symbol as INamedTypeSymbol ?? context.Symbol.ContainingType;
            return GetThreadSafeAttribute(context.Symbol) != null;
        }
        
        /// <summary>
        /// Checks if the target of a member access belongs to a [ThreadSafe] class.
        /// Should be used for call-site warnings to check if the target class is annotated.
        /// </summary>
        public static bool IsTargetInThreadSafeClass(ISymbol targetSymbol)
        {
            return GetThreadSafeAttribute(targetSymbol) != null;
        }
        
        /// <summary>
        /// Checks if a symbol's containing type (i.e. the class) is annotated with the [ThreadSafe] attribute.
        /// </summary>
        /// <param name="symbol">The symbol to inspect.</param>
        /// <returns>The AttributeData if found, otherwise null.</returns>
        private static AttributeData GetThreadSafeAttribute(ISymbol symbol)
        {
            if (symbol == null) return null;

            // If it's the class itself (INamedTypeSymbol), use it.
            // If it's a field/method, use the ContainingType.
            var typeToInspect = symbol as INamedTypeSymbol ?? symbol.ContainingType;

            return typeToInspect?.GetAttributes().FirstOrDefault(attr =>
            {
                var displayString = attr.AttributeClass?.ToDisplayString();
                
                return displayString == KnownTypes.ThreadSafe || 
                       attr.AttributeClass?.Name == KnownTypes.ThreadSafeShort ||
                       attr.AttributeClass?.Name == "ThreadSafe";
            });
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
        
        /// <summary>
        /// Returns a dictionary of Locks, and a LockAssociation list of all members where this lock is used as a target (e.g. lock($target) { ... } )
        /// </summary>
        /// <param name="classSymbol">The Symbol of the class to find locks in</param>
        /// <param name="semanticModel">The Semantic model of the classSymbol</param>
        /// <returns>A dictionary of [Key: LockSymbols, Value: <see cref="LockAssociation"/>]</returns>
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

                    if (lockObjSymbol == null) continue;
                    
                    if (!lockMapping.ContainsKey(lockObjSymbol))
                    {
                        lockMapping[lockObjSymbol] = new List<LockAssociation>();
                    }
                        
                    // Use the primary constructor of your new struct
                    lockMapping[lockObjSymbol].Add(new LockAssociation(memberSymbol, lockStmt));
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
        
        // ========================================================================================
        // =========================== CORRECTLY SYNCHRONIZED ANALYSER ============================
        // ========================================================================================
        public static Dictionary<ISymbol, AccessType> GetAccessedFields(ObjectCreationExpressionSyntax threadCreation, SemanticModel model)
        {
            var accessed = new Dictionary<ISymbol, AccessType>(SymbolEqualityComparer.Default);
            var lambda = threadCreation.ArgumentList?.Arguments.FirstOrDefault()?.Expression;

            if (lambda == null) return accessed;

            // Recursively find accesses inside the Thread lambda and any methods it calls
            PopulateAccessesRecursive(lambda, model, accessed, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

            return accessed;
        }

        private static void PopulateAccessesRecursive(SyntaxNode node, SemanticModel model, IDictionary<ISymbol, AccessType> accessed, HashSet<ISymbol> visitedMethods)
        {
            // 1. Manual Scan for Fields/Properties
            var identifiers = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var id in identifiers)
            {
                var info = model.GetSymbolInfo(id);
                var sym = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

                if (sym != null && (sym.Kind == SymbolKind.Field || sym.Kind == SymbolKind.Property))
                {
                    // Determine if it's a write by checking if it's on the left side of an assignment
                    var isWrite = IsWriteAccess(id);
                    UpdateAccessMap(accessed, sym, isWrite ? AccessType.Write : AccessType.Read);
                }
            }

            // 2. Process Invocations (Stepping into methods)
            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                // Guard: Only step into methods
                if (!(model.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
                    continue;
                
                if (!visitedMethods.Add(methodSymbol)) continue;

                foreach (var reference in methodSymbol.DeclaringSyntaxReferences)
                {
                    var methodSyntax = reference.GetSyntax();
                    PopulateAccessesRecursive(methodSyntax, model, accessed, visitedMethods);
                }
            }
        }

        private static bool IsWriteAccess(IdentifierNameSyntax identifier)
        {
            var parent = identifier.Parent;
    
            // Check if it's the Left part of an assignment: otherWork = 42;
            if (parent is AssignmentExpressionSyntax assignment && assignment.Left == identifier)
                return true;

            // Check for increment/decrement: otherWork++;
            if (parent is PostfixUnaryExpressionSyntax || parent is PrefixUnaryExpressionSyntax)
                return true;

            return false;
        }

        private static void UpdateAccessMap(IDictionary<ISymbol, AccessType> map, ISymbol symbol, AccessType type)
        {
            // If we already marked it as a Write, don't downgrade it to a Read
            if (map.TryGetValue(symbol, out var existing) && existing == AccessType.Write)
                return;

            map[symbol] = type;
        }

        public static IEnumerable<ISymbol> FindConflicts(Dictionary<ISymbol, AccessType> t1, Dictionary<ISymbol, AccessType> t2)
        {
            foreach (var field in t1.Keys)
            {
                if (!t2.TryGetValue(field, out var t2Access)) continue;
                
                // Conflict if: (T1 write) OR (T2 write)
                if (t1[field] == AccessType.Write || t2Access == AccessType.Write)
                {
                    yield return field;
                }
            }
        }

        public enum AccessType { Read, Write }

        /// <summary>
        /// Find all Thread Instantiations in a ClassDeclaration SyntaxNode 
        /// </summary>
        /// <param name="context">The current SyntaxNodeAnalysisContext</param>
        /// <param name="semanticModel">The current nodes Semantic Model</param>
        /// <returns></returns>
        public static List<ObjectCreationExpressionSyntax> GetThreadCreationsInClass(SyntaxNodeAnalysisContext context, SemanticModel semanticModel)
        {
            if (!(context.Node is ClassDeclarationSyntax classDecl)) return null;
            
            return classDecl.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(oc => {
                    var type = semanticModel.GetTypeInfo(oc).Type;
                    return type?.Name.Contains(KnownTypes.Thread) == true;
                })
                .ToList();
        }
        
        
        public static string GetThreadName(ObjectCreationExpressionSyntax creation)
        {
            // Check if the thread is part of an assignment: var t1 = new Thread(...)
            if (creation.Parent is EqualsValueClauseSyntax evc && evc.Parent is VariableDeclaratorSyntax vds)
            {
                return vds.Identifier.Text;
            }
            return "Anonymous Thread";
        }
    }
}