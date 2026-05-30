using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThreadSafetClassAnalyser.Utils
{
    public static class ThreadSafeValidator
    {
        /// <summary>
        /// Determines if the analysis should proceed based on global settings and [ThreadSafe] annotations.
        /// </summary>
        /// <returns>True if the analysis should continue; False if it should be skipped.</returns>
        public static bool ShouldValidate(SyntaxNodeAnalysisContext context)
        {
            if (!Environment.USE_ANNOTATION) return true;
            return IsInThreadSafeClass(context);
        }
        
        /// <summary>
        /// Determines if the analysis should proceed based on global settings and [ThreadSafe] annotations.
        /// </summary>
        /// <returns>True if the analysis should continue; False if it should be skipped.</returns>
        public static bool ShouldValidateTarget(ISymbol target)
        {
            if (!Environment.USE_ANNOTATION) return true;
            return IsTargetInThreadSafeClass(target);
        }

        /// <summary>
        /// Overload for Symbol-based analysis.
        /// </summary>
        public static bool ShouldValidate(SymbolAnalysisContext context)
        {
            if (!Environment.USE_ANNOTATION) return true;
            return IsInThreadSafeClass(context);
        }
        
        /// <summary>
        /// Determines if the current analysis context is within a class marked with [ThreadSafe].
        /// Used for methods registered with 'RegisterSyntaxNodeAction()' in the Initialize method.
        /// </summary>
        /// <param name="context">A SyntaxNodeAnalysisContext</param>
        private static bool IsInThreadSafeClass(SyntaxNodeAnalysisContext context)
        {
            var classSymbol = context.ContainingSymbol?.ContainingType;
            return GetThreadSafeAttribute(classSymbol) != null;
        }


        /// <summary>
        /// Determines if the current symbol analysis context is for a class marked with [ThreadSafe].
        /// Used for methods registered with 'RegisterSymbolAction()' and 'SymbolKind.NamedType' in the Initialize method.
        /// </summary>
        /// <param name="context">A SymbolAnalysisContext</param>
        private static bool IsInThreadSafeClass(SymbolAnalysisContext context)
        {
            return GetThreadSafeAttribute(context.Symbol) != null;
        }
        
        /// <summary>
        /// Checks if the target of a member access belongs to a [ThreadSafe] class.
        /// Should be used for call-site warnings to check if the target class is annotated.
        /// </summary>
        private static bool IsTargetInThreadSafeClass(ISymbol targetSymbol)
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
    }
}