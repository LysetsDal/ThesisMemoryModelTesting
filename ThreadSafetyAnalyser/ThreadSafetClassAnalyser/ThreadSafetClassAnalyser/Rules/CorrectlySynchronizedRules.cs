using Microsoft.CodeAnalysis;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable RS2008

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

        // --- ConflictingAccessRule ---
        public const string ConflictingAccessRuleId = "ConflictingAccessRule";
        public static readonly DiagnosticDescriptor ConflictingAccessRule =
            Create(
                ConflictingAccessRuleId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );

        // --- MonitorNotPaired Rule ---
        // Flags a Monitor.Enter call with no matching Monitor.Exit in the same method
        public const string MonitorNotPairedId = "MonitorNotPaired";
        public static readonly DiagnosticDescriptor MonitorNotPairedRule =
            Create(
                MonitorNotPairedId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );

        // --- MonitorNotInFinally Rule ---
        // Flags a Monitor.Enter call that is not guarded by a try/finally block
        public const string MonitorNotInFinallyId = "MonitorNotInFinally";
        public static readonly DiagnosticDescriptor MonitorNotInFinallyRule =
            Create(
                MonitorNotInFinallyId,
                Category.CorrectlySynchronized,
                DiagnosticSeverity.Warning
            );

        // --- MonitorConflictingAccess Rule ---
        // Flags conflicting access between a Monitor-guarded region and another unsynchronized member
        public const string MonitorConflictingAccessId = "MonitorConflictingAccess";
        public static readonly DiagnosticDescriptor MonitorConflictingAccessRule =
            Create(
                MonitorConflictingAccessId,
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