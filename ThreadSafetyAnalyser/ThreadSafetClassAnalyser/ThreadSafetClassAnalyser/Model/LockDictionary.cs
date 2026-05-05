using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Represents an association between a code member (method, property accessor or constructor) and a specific lock statement 
/// found within its body.
/// </summary>
public readonly struct LockAssociation
{
    /// <summary>
    /// Gets the symbol representing the method, property accessor, or constructor 
    /// that contains the lock statement.
    /// </summary>
    public ISymbol Member { get; }
    
    /// <summary>
    /// Gets the syntax node for the lock statement found within the <see cref="Member"/>.
    /// </summary>
    public LockStatementSyntax Lock { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockAssociation"/> struct.
    /// </summary>
    /// <param name="member">The symbol of the enclosing method, property, or constructor.</param>
    /// <param name="lock">The syntax node of the lock statement being mapped.</param>
    public LockAssociation(ISymbol member, LockStatementSyntax @lock)
    {
        Member = member;
        Lock = @lock;
    }
}