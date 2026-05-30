using System;
using System.Collections.Generic;

namespace DummyApp.Model;
/// <summary>
/// Represents the output of the audit process
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Indicates the change type (i.e. CustomerOrder Update)
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// The environment information
    /// </summary>
    public string Environment { get; set; }
    
    /// <summary>
    /// The current distributed tracing activity information 
    /// </summary>
    public object Activity { get; set; }

    /// <summary>
    /// The extension data. 
    /// This will be serialized as the keys being properties of the current object.
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; }

    /// <summary>
    /// The tracked target.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Comments.
    /// </summary>
    public List<string> Comments { get; set; }

    /// <summary>
    /// The date then the event started
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The timestamp when the event started
    /// </summary>
    public long? StartTimestamp { get; set; }

    /// <summary>
    /// The date then the event finished
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The timestamp when the event finished
    /// </summary>
    public long? EndTimestamp { get; set; }
    
    ///<summary>
    /// The duration of the operation in milliseconds.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// A weak reference to the audit scope associated with this event.
    /// </summary>
    private readonly WeakReference _auditScope = new WeakReference(null);
    
}