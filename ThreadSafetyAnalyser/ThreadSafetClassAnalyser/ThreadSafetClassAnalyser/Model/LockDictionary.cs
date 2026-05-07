using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThreadSafetClassAnalyser.Model
{
    /// <summary>
    /// Represents an association between a code member and a specific lock statement.
    /// This pair links a <see cref="MemberContainingLock"/> (e.g., a method) to the <see cref="Lock"/> statement found within it.
    /// </summary>
    /// <remarks>
    /// This is used by the analyzer to generate a map over which synchronization objects protect which members.
    /// </remarks>
    public readonly struct LockAssociation
    {
        /// <summary>
        /// Gets the symbol representing the method, property accessor, or constructor 
        /// that contains the lock statement.
        /// </summary>
        public ISymbol MemberContainingLock { get; }
    
        /// <summary>
        /// Gets the syntax node for the lock statement found within the <see cref="MemberContainingLock"/>.
        /// </summary>
        public LockStatementSyntax Lock { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockAssociation"/> struct.
        /// </summary>
        /// <param name="memberContainingLock">The symbol of the enclosing method, property, or constructor.</param>
        /// <param name="lock">The syntax node of the lock statement being mapped.</param>
        /// <param name="location">The location of the member.</param>
        public LockAssociation(ISymbol memberContainingLock, LockStatementSyntax @lock)
        {
            MemberContainingLock = memberContainingLock;
            Lock = @lock;
        }
    }
}

