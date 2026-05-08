using Microsoft.CodeAnalysis;

namespace ThreadSafetClassAnalyser.Rules
{
    public static class EncapsulationRules
    {
        // --- FieldAccessedExternally Rule ---
        public const string FieldAccessedExternallyDiagnosticId = "FieldAccessedExternally";
        public static readonly DiagnosticDescriptor FieldAccessedExternallyRule =
            Create(
                FieldAccessedExternallyDiagnosticId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );
        
        // --- PublicFieldExposed Rule ---
        public const string PublicFieldExposedDiagnosticId = "PublicFieldExposed";
        public static readonly DiagnosticDescriptor PublicFieldExposedRule =
            Create(
                PublicFieldExposedDiagnosticId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );
        
        // --- FieldDoesNotUseLock Rule ---
        public const string FieldDoesNotUseLockId = "FieldDoesNotUseLock";
        public static readonly DiagnosticDescriptor FieldDoesNotUseLockRule =
            Create(
                FieldDoesNotUseLockId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );
        
        // --- InternalFieldNoLock ---
        public const string InternalFieldNoLockId = "InternalFieldNoLock";
        public static readonly DiagnosticDescriptor InternalFieldNoLockRule =
            Create(
                InternalFieldNoLockId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );
        
        // --- Lock Object Exposed via Public Accessor ---
        public const string LockObjectExposedId = "LockObjectExposed";
        public static readonly DiagnosticDescriptor LockObjectExposedRule =
            Create(
                LockObjectExposedId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );
        
        // --- Test Rule ---
        public const string TestRuleId = "TestRule";
        public static readonly DiagnosticDescriptor TestRuleRule =
            Create(
                TestRuleId,
                Category.Encapsulation,
                DiagnosticSeverity.Warning
            );

        
        private static DiagnosticDescriptor Create(string id, string category, DiagnosticSeverity severity)
        {
            var meta = new AnalyserMetadata(id);
            return new DiagnosticDescriptor(id, meta.Title, meta.MessageFormat, category, severity, true, meta.Description);
        }
    }
}