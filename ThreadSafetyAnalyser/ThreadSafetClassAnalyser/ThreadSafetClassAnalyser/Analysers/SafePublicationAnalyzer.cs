using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ThreadSafetClassAnalyser.Rules;
using ThreadSafetClassAnalyser.Utils;
// ReSharper disable UnusedType.Global
// ReSharper disable UseNegatedPatternMatching
// ReSharper disable ConvertIfStatementToSwitchStatement

namespace ThreadSafetClassAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SafePublicationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(
                SafePublicationRules.UnsafeFieldRule, 
                SafePublicationRules.VirtualCallInCtorRule,
                SafePublicationRules.ThisReferenceEscapeRule
            );
        

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorForVirtualCalls, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorForThisEscape, SyntaxKind.ConstructorDeclaration);
        }
        
        /// <summary>
        /// SP1: Implementation to detect if 'this' is published before the constructor completes.
        /// </summary>
        private static void AnalyzeConstructorForThisEscape(SyntaxNodeAnalysisContext context)
        {
            var ctorDecl = (ConstructorDeclarationSyntax)context.Node;
            var classDecl = ctorDecl.Parent as ClassDeclarationSyntax;
            if (classDecl == null) return;
            
            if (!ThreadSafeValidator.ShouldValidate(context)) return;

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return;

            var thisExpressions = ctorDecl.DescendantNodes().OfType<ThisExpressionSyntax>();

            foreach (var thisExpr in thisExpressions)
            {
                // --- Scenario 1: Static Assignment (e.g., _global = this;) ---
                if (thisExpr.Parent is AssignmentExpressionSyntax assignment && assignment.Right == thisExpr)
                {
                    var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                    if (leftSymbol != null && leftSymbol.IsStatic)
                    {
                        ReportThisEscape(context, thisExpr, classSymbol.Name, "a static assignment", leftSymbol.Name);
                        continue; // Found an escape, move to next 'this'
                    }
                }

                // --- Scenario 2: Method/Constructor Argument (e.g., SomeMethod(this);) ---
                // We walk up to find the ArgumentSyntax and then its parent (the call)
                var argument = thisExpr.Parent as ArgumentSyntax;
                if (argument?.Parent is ArgumentListSyntax argList)
                {
                    var callNode = argList.Parent;
                    if (callNode != null)
                    {
                        var symbol = semanticModel.GetSymbolInfo(callNode).Symbol;
                        if (symbol is IMethodSymbol method)
                        {
                            // If the method belongs to another class or is a constructor, it's an escape
                            var isExternal = !SymbolEqualityComparer.Default.Equals(method.ContainingType, classSymbol);
                            var isCtor = method.MethodKind == MethodKind.Constructor;

                            if (isExternal || isCtor)
                            {
                                ReportThisEscape(context, thisExpr, classSymbol.Name, "an external call", method.Name);
                                continue;
                            }
                        }
                    }
                }
                
                // --- Scenario 3: Event Registration (e.g., Global.OnData += this.OnData;) ---
                if (thisExpr.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Parent is AssignmentExpressionSyntax eventAssignment &&
                    eventAssignment.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    var eventSymbol = semanticModel.GetSymbolInfo(eventAssignment.Left).Symbol;
                    if (eventSymbol is IEventSymbol)
                    {
                        ReportThisEscape(context, thisExpr, classSymbol.Name, "an event registration", eventSymbol.Name);
                    }
                }
            }
        }

        private static void ReportThisEscape(
            SyntaxNodeAnalysisContext context, 
            SyntaxNode node, 
            string className, 
            string escapeType, 
            string targetName)
        {
            var diagnostic = Diagnostic.Create(
                SafePublicationRules.ThisReferenceEscapeRule,
                node.GetLocation(),
                className,   // {0}
                escapeType,  // {1}
                targetName); // {2}

            context.ReportDiagnostic(diagnostic);
        }
    

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {

            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel; // Safe and efficient
            // Get the symbol for the logical class
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return;

            // Use the validator with the SyntaxNode context
            if (!ThreadSafeValidator.ShouldValidateTarget(classSymbol)) return;

            // --- SP001: Check all instance fields for safe publication ---
            foreach (var fieldDecl in classDecl.Members.OfType<FieldDeclarationSyntax>())
            {
                // Skip static, const, readonly, and volatile fields � they are safely published
                if (fieldDecl.Modifiers.Any(m =>
                        m.IsKind(SyntaxKind.StaticKeyword) ||
                        m.IsKind(SyntaxKind.ConstKeyword) ||
                        m.IsKind(SyntaxKind.ReadOnlyKeyword) ||
                        m.IsKind(SyntaxKind.VolatileKeyword)))
                    continue;

                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol;
                    if (fieldSymbol == null || fieldSymbol.IsConst)
                        continue;

                    // Skip fields whose type is immutable � their state cannot change after construction
                    if (IsImmutableType(fieldSymbol.Type))
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        SafePublicationRules.UnsafeFieldRule,
                        variable.GetLocation(),
                        fieldSymbol.Name));
                }
            }
        }

        private static void AnalyzeConstructorForVirtualCalls(SyntaxNodeAnalysisContext context)
        {
            var ctorDecl = (ConstructorDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;// Safe and efficient
            var classDecl = ctorDecl.Parent as ClassDeclarationSyntax;

            // Guard: constructor may be inside a record, struct, or other non-class declaration
            if (classDecl == null) return;

            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol == null) return;

            if (!ThreadSafeValidator.ShouldValidateTarget(classSymbol)) return;

            if (classSymbol == null || classSymbol.IsSealed)
                return; // sealed class: no derived class can override, safe

            // Guard: constructor may have no body (e.g. extern or expression-bodied)
            if (ctorDecl.Body == null) return;

            // Find all invocation expressions in the constructor body
            var invocations = ctorDecl.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
                if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol))
                    continue;

                // Flag calls to virtual or abstract methods declared on this class or its base types
                if (!methodSymbol.IsVirtual && !methodSymbol.IsAbstract && !methodSymbol.IsOverride)
                    continue;

                // Ensure the method is accessible to derived classes (could be overridden)
                if (methodSymbol.DeclaredAccessibility == Accessibility.Private)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    SafePublicationRules.VirtualCallInCtorRule,
                    invocation.GetLocation(),
                    classSymbol.Name,
                    methodSymbol.Name));
            }
        }

        /// <summary>
        /// Returns true if <paramref name="type"/> is considered immutable:
        /// its observable state cannot change after construction, making it safely published.
        /// Follows the Microsoft definition: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/immutability
        /// </summary>
        private static bool IsImmutableType(ITypeSymbol type)
        {
            if (type == null || type.Kind == SymbolKind.ErrorType)
                return false;

            // Well-known immutable BCL types (string, primitives, Guid, Uri etc.)
            if (IsWellKnownImmutableType(type))
                return true;

            // Enum types are immutable by nature (named integer constants, no fields)
            if (type.TypeKind == TypeKind.Enum)
                return true;

            // Interfaces are never considered immutable � they are contracts only.
            // Even IReadOnlyList<T> does not guarantee the backing object is immutable.
            if (type.TypeKind == TypeKind.Interface)
                return false;

            // A readonly struct (C# 8+): compiler enforces no mutation of instance state
            if (type.TypeKind == TypeKind.Struct && type.IsReadOnly)
                return true;

            if (!(type is INamedTypeSymbol namedType))
                return false;

            // Records are immutable by the Microsoft definition:
            // the compiler generates init-only properties and no public mutating members.
            if (namedType.IsRecord)
                return true;

            // Per the Microsoft definition � a type is immutable if it has:
            //   - No public properties or fields, OR
            //   - Only read-only properties (no setter), OR
            //   - Only init-only setters on properties
            // AND all instance fields are readonly or const.
            var instanceFields = namedType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic);

            var allFieldsReadOnly = instanceFields.All(f => f.IsReadOnly || f.IsConst);

            var instanceProperties = namedType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic);

            var allPropertiesReadOnlyOrInitOnly = instanceProperties.All(p =>
                p.IsReadOnly ||                                          // get-only: { get; }
                p.SetMethod == null ||                                   // no setter at all
                p.SetMethod.IsInitOnly);                                 // init-only: { get; init; }

            return allFieldsReadOnly && allPropertiesReadOnlyOrInitOnly;
        }

        private static bool IsWellKnownImmutableType(ITypeSymbol type)
        {
            // All primitive value types and string are immutable
            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Char:
                case SpecialType.System_DateTime:   
                    return true;
            }

            // Additional well-known immutable BCL types not covered by SpecialType
            var fullName = type.ToDisplayString();
            switch (fullName)
            {
                case "System.Guid":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                case "System.Uri":
                case "System.Version":
                case "System.Numerics.BigInteger":
                    return true;
            }

            return false;
        }
    }
}