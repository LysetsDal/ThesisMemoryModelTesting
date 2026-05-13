using Annotations;
using DummyApp.Model;

namespace DummyApp.Src;

/// <summary>
/// Excerpt from Audit.NET repo see https://github.com/thepirat000/Audit.NET#
/// </summary>
// [ThreadSafe]
public class ComplexClass
{
    
    private readonly object _lock = new();
    private readonly object _lock2 = new();
    
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
    private readonly object _lockerSend = new object();
    private AuditEvent _clientReceive;
    private readonly object _lockerReceive = new object();
    private int resSend;
    private int resReceive;
    
    public AuditEvent GetSendClient()
    {
        // lock (_lockerSend)
        // {
            if (_clientSend == null)
            {
                _clientSend = new AuditEvent();
                if (true)
                {
                    resSend = _clientSend.Duration;
                }
            }
        // }
        return _clientSend;
    }
    
    private AuditEvent GetReceiveClient()
    {
        lock (_lockerReceive)
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
