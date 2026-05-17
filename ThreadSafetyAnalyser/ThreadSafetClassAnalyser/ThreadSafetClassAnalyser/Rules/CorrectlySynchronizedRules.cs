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
        
        // --- LockOnClassInstance ---
        public const string LockOnClassInstanceId = "LockOnClassInstance";
        public static readonly DiagnosticDescriptor LockOnClassInstanceRule =
            Create(
                LockOnClassInstanceId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );
        
        // --- VolatileReordering Rule ---
        public const string VolatileReorderingId = "VolatileReordering";
        public static readonly DiagnosticDescriptor VolatileReorderingRule =
            Create(
                VolatileReorderingId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );

        public const string ConflictingAccessRuleId = "ConflictingAccessRule";

        public static readonly DiagnosticDescriptor ConflictingAccessRule =
            Create(
                ConflictingAccessRuleId,
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