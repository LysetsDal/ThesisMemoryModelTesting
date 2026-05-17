using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThreadSafetClassAnalyser.Model
{
/// <summary>
    /// Encapsulates the locking infrastructure of a class, mapping specific lock symbols 
    /// to the code blocks where they are used as synchronization targets.
    /// </summary>
    public class ClassLocks
    {
        private readonly ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>> _map;

        /// <summary>
        /// Gets an empty instance of <see cref="ClassLocks"/>.
        /// </summary>
        public static ClassLocks Empty { get; } = new ClassLocks(ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>>.Empty);

        public ClassLocks(ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>> map)
        {
            _map = map ?? ImmutableDictionary<ISymbol, ImmutableArray<LockAssociation>>.Empty;
        }

        /// <summary>
        /// Gets all distinct objects/symbols that are used as a lock target within the class.
        /// </summary>
        public IEnumerable<ISymbol> Keys => _map.Keys;
        
        /// <summary>
        /// Gets all collections of lock associations tracked within the class.
        /// </summary>
        public IEnumerable<ImmutableArray<LockAssociation>> Values => _map.Values;

        /// <summary>
        /// Gets the total number of distinct lock objects tracked.
        /// </summary>
        public int Count => _map.Count;

        /// <summary>
        /// Gets the associations mapped to a specific lock object. Returns an empty array if the lock object is unknown.
        /// </summary>
        public ImmutableArray<LockAssociation> this[ISymbol lockObject] => 
            _map.TryGetValue(lockObject, out var associations) ? associations : ImmutableArray<LockAssociation>.Empty;

        /// <summary>
        /// Tries to get the lock associations for the specified lock object symbol.
        /// </summary>
        public bool TryGetAssociations(ISymbol lockObject, out ImmutableArray<LockAssociation> associations)
        {
            return _map.TryGetValue(lockObject, out associations);
        }

        /// <summary>
        /// Exposes standard KeyValuePair enumeration over the underlying map.
        /// </summary>
        public IEnumerator<KeyValuePair<ISymbol, ImmutableArray<LockAssociation>>> GetEnumerator() => _map.GetEnumerator();
    }
    
    
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

