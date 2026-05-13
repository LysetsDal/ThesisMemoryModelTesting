using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ThreadSafetClassAnalyser.Model;


namespace ThreadSafetClassAnalyser.Utils
{
    public static class AnalyzerUtils
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
            var lockMapping = new Dictionary<ISymbol, List<LockAssociation>>(SymbolEqualityComparer.Default);
    
            // Get the current tree we are allowed to analyze
            var currentTree = semanticModel.SyntaxTree;

            foreach (var location in classSymbol.DeclaringSyntaxReferences)
            {
                // Only look at the syntax if it belongs to the tree the SemanticModel knows about
                if (location.SyntaxTree != currentTree) continue;

                var classSyntax = location.GetSyntax() as ClassDeclarationSyntax;
                if (classSyntax == null) continue;

                var allLocks = classSyntax.DescendantNodes().OfType<LockStatementSyntax>();

                foreach (var lockStmt in allLocks)
                {
                    // Determine WHAT is being locked (the expression inside the parentheses)
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
                            [GetClassLockAssociationDict] SemanticModel/SyntaxTree mismatch while resolving lock expression.
            
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
    
            return lockMapping.ToImmutableDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.ToImmutableArray(), 
                SymbolEqualityComparer.Default);
        }
        
        /// <summary>
        /// Looks at a method and returns the first lock it finds inside the method. Single layer, doesn't recurse.
        /// </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public static LockStatementSyntax FindFirstDescendantLockFromMethodSymbol(ISymbol methodSymbol)
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
            
            if (lockStmt == null) return null;

            // Get the symbol of what is being locked (e.g., _syncObj)
            return model.GetSymbolInfo(lockStmt.Expression).Symbol;
        }
        
        /// <summary>
        /// Searches the class lock associations to find which lock objects (targets) 
        /// are being used inside a specific member (method, lambda, etc.).
        /// </summary>
        /// <param name="lockMap">The map generated by GetClassLockAssociationDict.</param>
        /// <param name="memberSymbol">The symbol of the method or lambda to check.</param>
        /// <returns>A list of symbols representing the objects being locked.</returns>
        public static IEnumerable<ISymbol> GetLockObjectsUsedInMember(
            ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>> lockMap, 
            ISymbol memberSymbol)
        {
            if (memberSymbol == null) yield break;

            foreach (var entry in lockMap)
            {
                var lockObject = entry.Key;
                var associations = entry.Value;

                // Check if any association in this lock group belongs to our target member
                if (associations.Any(a => SymbolEqualityComparer.Default.Equals(a.MemberContainingLock, memberSymbol)))
                {
                    yield return lockObject;
                }
            }
        }
        
        public static ISymbol GetEnclosingLockSymbol(SyntaxNode node, SemanticModel model)
        {
            var lockStatement = node.Ancestors().OfType<LockStatementSyntax>().FirstOrDefault();
            if (lockStatement == null) return null;

            return model.GetSymbolInfo(lockStatement.Expression).Symbol;
        }
        
        
        // ========================================================================================
        // =========================== CORRECTLY SYNCHRONIZED ANALYSER ============================
        // ========================================================================================
        public static Dictionary<ISymbol, AccessInfo> GetAccessedFields(ObjectCreationExpressionSyntax threadCreation, SemanticModel model)
        {
            // Change instantiation to AccessInfo
            var accessed = new Dictionary<ISymbol, AccessInfo>(SymbolEqualityComparer.Default);
    
            var lambda = threadCreation.ArgumentList?.Arguments.FirstOrDefault()?.Expression;

            if (lambda == null) return accessed;

            // Now the types match for this call
            PopulateAccessesRecursive(
                lambda, 
                model, 
                accessed, 
                new HashSet<ISymbol>(SymbolEqualityComparer.Default),
                null);

            return accessed;
        }

        public static void PopulateAccessesRecursive(
            SyntaxNode node,
            SemanticModel model, 
            IDictionary<ISymbol, AccessInfo> accessed, 
            HashSet<ISymbol> visitedMethods,
            ISymbol currentLockSymbol)
        {
            // 1. Manual Scan for Fields/Properties
            var identifiers = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var id in identifiers)
            {
                var info = model.GetSymbolInfo(id);
                var sym = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

                if (sym == null ||
                    (sym.Kind != SymbolKind.Field && sym.Kind != SymbolKind.Property)) continue;
                
                // Determine if it's a write by checking if it's on the left side of an assignment
                var isWrite = IsWriteAccess(id);
                var effectiveLock = currentLockSymbol ?? GetFirstAncestorLockFromSymbol(id, model);
                    
                UpdateAccessMap(accessed, sym, isWrite ? AccessType.Write : AccessType.Read, effectiveLock);
            }

            // 2. Process Invocations (Stepping into methods)
            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                if (!(model.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)) continue;
                if (!visitedMethods.Add(methodSymbol)) continue;

                // Check if THIS specific call is wrapped in a lock before jumping
                var lockAtCallSite = currentLockSymbol ?? GetFirstAncestorLockFromSymbol(invocation, model);

                foreach (var reference in methodSymbol.DeclaringSyntaxReferences)
                {
                    var methodSyntax = reference.GetSyntax();
                    // Pass the current lock state down into the method body
                    PopulateAccessesRecursive(methodSyntax, model, accessed, visitedMethods, lockAtCallSite);
                }
            }
        }

        public static bool IsWriteAccess(IdentifierNameSyntax identifier)
        {
            SyntaxNode current = identifier;

            // Walk up through member accesses (obj.Field) or element accesses (arr[i])
            // to find the full expression being assigned to.
            while (current.Parent is MemberAccessExpressionSyntax || current.Parent is ElementAccessExpressionSyntax)
            {
                current = current.Parent;
            }

            var parent = current.Parent;

            // 1. Check for assignment: current = value;
            if (parent is AssignmentExpressionSyntax assignment && assignment.Left == current)
            {
                return true;
            }

            // 2. Check for increment/decrement: current++;
            if (parent is PostfixUnaryExpressionSyntax || parent is PrefixUnaryExpressionSyntax)
            {
                return true;
            }

            return false;
        }

        private static void UpdateAccessMap(IDictionary<ISymbol, AccessInfo> map, ISymbol symbol, AccessType type, ISymbol currentLock)
        {
            if (map.TryGetValue(symbol, out var existing))
            {
                if (type == AccessType.Write) existing.AccessType = AccessType.Write;
                
                if (!SymbolEqualityComparer.Default.Equals(existing.LockObject, currentLock))
                {
                    existing.LockObject = null; 
                }
            }
            else
            {
                map[symbol] = new AccessInfo { AccessType = type, LockObject = currentLock };
            }
        }

        public static IEnumerable<ISymbol> FindConflicts(
            Dictionary<ISymbol, AccessInfo> t1, 
            Dictionary<ISymbol, AccessInfo> t2)
        {
            foreach (var field in t1.Keys)
            {
                if (!t2.TryGetValue(field, out var info2)) continue;
                var info1 = t1[field];

                // 1. Basic conflict check (at least one write)
                if (info1.AccessType == AccessType.Write || info2.AccessType == AccessType.Write)
                {
                    yield return field;
                }
            }
        }
        
        /// <summary>
        /// Find all ObjectCreationsExpressions from the <see cref="context"/> nodes Descendant nodes.
        /// </summary>
        /// <param name="context">The current SyntaxNodeAnalysisContext.</param>
        /// <param name="semanticModel">The current nodes Semantic model.</param>
        /// <param name="typeSymbol"> A typePrefix from the <see cref="KnownTypes"/> file (e.g. Thread or Task).</param>
        /// <returns>A list of all ObjectCreationExpressionSyntax of type <see cref="KnownTypes"/> in the class.</returns>
        public static List<ObjectCreationExpressionSyntax> GetObjectCreationsInClass(SyntaxNodeAnalysisContext context, SemanticModel semanticModel, string typeSymbol)
        {
            if (!(context.Node is ClassDeclarationSyntax classDecl)) return null;
            
            return classDecl.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(oc => {
                    var type = semanticModel.GetTypeInfo(oc).Type;
                    return type?.Name.Contains(typeSymbol) == true;
                })
                .ToList();
        }
        
        public static bool HasFullFenceBetween(SyntaxNode root, SyntaxNode start, SyntaxNode end, SemanticModel model)
        {
            var startPos = start.SpanStart;
            var endPos = end.SpanStart;

            // Identify all nodes between the Write and Read in Program Order
            var nodesInBetween = root.DescendantNodes()
                .Where(n => n.SpanStart > startPos && n.SpanStart < endPos);

            foreach (var node in nodesInBetween)
            {
                // Check for Thread.MemoryBarrier() [cite: 240]
                if (node is InvocationExpressionSyntax invocation)
                {
                    var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        var containingType = symbol.ContainingType.ToDisplayString();
                        // Barriers: Thread.MemoryBarrier, Interlocked operations, or entering a lock
                        if (containingType == "System.Threading.Thread" && symbol.Name == "MemoryBarrier") return true;
                        if (containingType == "System.Threading.Interlocked") return true;
                    }
                }

                // Check for lock statements (Full fence on entry/exit) [cite: 159, 200]
                if (node is LockStatementSyntax) return true;
            }

            return false;
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
        
        public static bool IsCorrectlySynchronized(AccessInfo info1, AccessInfo info2)
        {
            return info1.LockObject != null && 
                   info2.LockObject != null && 
                   SymbolEqualityComparer.Default.Equals(info1.LockObject, info2.LockObject);
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