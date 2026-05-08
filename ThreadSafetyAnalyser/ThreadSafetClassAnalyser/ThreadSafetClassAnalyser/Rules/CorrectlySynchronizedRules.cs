using Microsoft.CodeAnalysis;

namespace ThreadSafetClassAnalyser.Rules
{
    public static class CorrectlySynchronizedRules
    {
        public const string FieldUsedDiagnosticId = "FieldUsedDiagnostic";
        
        // --- PublicFieldExposed Rule ---
        public const string ConflictingAccessThreadId = "ConflictingAccessThread";
        public static readonly DiagnosticDescriptor ConflictingAccessThreadRule =
            Create(
                ConflictingAccessThreadId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );
        
        private static DiagnosticDescriptor Create(string id, string category, DiagnosticSeverity severity)
        {
            var meta = new AnalyserMetadata(id);
            return new DiagnosticDescriptor(id, meta.Title, meta.MessageFormat, category, severity, true, meta.Description);
        }
    }
}