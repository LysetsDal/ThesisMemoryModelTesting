using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThreadSafetClassAnalyser.Model
{
    public class ThreadAnalysisInfo
    {
        // The Lambda or Method Symbol of the thread body
        public ISymbol BodySymbol { get; set;}
        
        // The variable name (e.g., "t1")
        public string Name { get; set; }
        
        // The fields/properties accessed by this thread
        public Dictionary<ISymbol, AccessInfo> AccessMap { get; set; }
        
        // Symbols of objects used in lock() statements within this thread
        public IEnumerable<ISymbol> UsedLockObjects { get; set; }
        
        // The method declaration containing this thread
        public MethodDeclarationSyntax MethodScope { get; set; }
        
        // The original syntax node (for GetLocation() calls)
        public ObjectCreationExpressionSyntax Syntax { get; set; }
    }
}