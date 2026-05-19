using System.Collections.Generic;
using System.Linq;
using Annotations;
using DummyApp.Model;
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable InconsistentNaming
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable RedundantCast
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeThisQualifier
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeDefaultValueWhenTypeNotEvident
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable RedundantAssignment
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UseCollectionExpression
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace DummyApp.Src;

/// <summary>
/// Excerpt from Audit.NET repo see https://github.com/thepirat000/Audit.NET#
/// </summary>
[ThreadSafe]
public class ComplexClass
{
    private readonly object _lock = new();
    
    private List<AuditEvent> _events = new(); 
    
    public object InsertEvent(AuditEvent auditEvent)
    {
        lock (_lock)
        {
            _events.Add(auditEvent);
            var index = _events.Count - 1;
            return index;
        }
    }
    
    public void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        var index = (int)eventId;
        lock (_lock)
            _events[index] = auditEvent;
    }
    
    public AuditEvent GetEvent(object eventId)
    {
        var index = (int)eventId;
        lock (_lock)
            return _events[index];
    }

    /// <summary>
    /// Returns a read-only collection of audit events currently stored in memory.
    /// </summary>
    public IList<AuditEvent> GetAllEvents()
    {
        lock (_lock)
            return _events.AsReadOnly();
    }

    /// <summary>
    /// Returns a read-only collection of audit events currently stored in memory, filtered by the given audit event type.
    /// </summary>
    public IList<T> GetAllEventsOfType<T>()
        where T : AuditEvent
    {
        lock (_lock)
            return _events.OfType<T>().ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Removes all audit events currently in memory.
    /// </summary>
    public void ClearEvents()
    {
        lock (_lock)
            _events.Clear();
    }
    
    private AuditEvent _clientSend;
    private readonly object _lockerSend = new ();
    private AuditEvent _clientReceive;
    private readonly object _lockerReceive = new ();
    private int resSend;
    private int resReceive;
    
    // Should flag warning ConflictingAccessRule 
    public AuditEvent GetSendClient()
    {
        lock (_lockerSend)
        {
            if (_clientSend == null)
            {
                _clientSend = new AuditEvent();
                if (true)
                {
                    resSend = _clientSend.Duration;
                }
            }
        }
        return _clientSend;
        // Should flag InternalFieldNoLock warning
    }
    
    // Should flag warning ConflictingAccessRule 
    private AuditEvent GetReceiveClient()
    {
        lock (_lockerSend)
        {
            if (_clientReceive == null)
            {
                _clientReceive = new AuditEvent();
                if (true)
                {
                    resReceive = _clientSend.Duration;
                }
            }
        }
        return _clientSend;
    }
}
