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
            return AnalyzerUtils.IsInThreadSafeClass(context);
        }
        
        /// <summary>
        /// Determines if the analysis should proceed based on global settings and [ThreadSafe] annotations.
        /// </summary>
        /// <returns>True if the analysis should continue; False if it should be skipped.</returns>
        public static bool ShouldValidateTarget(ISymbol target)
        {
            if (!Environment.USE_ANNOTATION) return true;
            return AnalyzerUtils.IsTargetInThreadSafeClass(target);
        }

        /// <summary>
        /// Overload for Symbol-based analysis.
        /// </summary>
        public static bool ShouldValidate(SymbolAnalysisContext context)
        {
            if (!Environment.USE_ANNOTATION) return true;
            return AnalyzerUtils.IsInThreadSafeClass(context);
        }
        
        
    }
}