using Microsoft.CodeAnalysis;

namespace ThreadSafetClassAnalyser.Rules
{
    public static class SafePublicationRules
    {
        // --- Diagnostic: field is not safely published (not readonly/volatile) ---
        public const string UnsafeFieldDiagnosticId = "SP001";
        public static readonly DiagnosticDescriptor UnsafeFieldRule =
            Create(
                UnsafeFieldDiagnosticId,
                Category.SafePublication,
                DiagnosticSeverity.Warning
            );


        // SP002 (DerivedInitializerReadsBase) was removed: the C# compiler already reports
        // CS0236 ("A field initializer cannot reference the non-static field, method, or
        // property '<member>'") for exactly this scenario, making SP002 redundant.
        // See: https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0236

        // --- Diagnostic: virtual method call in constructor (SP003) ---
        public const string VirtualCallInCtorDiagnosticId = "SP003";
        public static readonly DiagnosticDescriptor VirtualCallInCtorRule =
            Create(
                VirtualCallInCtorDiagnosticId,
                Category.SafePublication,
                DiagnosticSeverity.Warning
            );
        
        private static DiagnosticDescriptor Create(string id, string category, DiagnosticSeverity severity)
        {
            var meta = new AnalyserMetadata(id);
            return new DiagnosticDescriptor(id, meta.Title, meta.MessageFormat, category, severity, true, meta.Description);
        }
    }
}