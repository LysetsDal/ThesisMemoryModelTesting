
using Microsoft.CodeAnalysis;

namespace ThreadSafetClassAnalyser.Model
{
    public enum AccessType { Read, Write }

    public class AccessInfo
    {
        public AccessType AccessType { get; set; }
        
        public ISymbol LockObject { get; set; }
    }
}