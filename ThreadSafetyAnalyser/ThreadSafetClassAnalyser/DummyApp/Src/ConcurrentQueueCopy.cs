// ReSharper disable RedundantUsingDirective
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Annotations;

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
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace DummyApp.Src;

  /// <summary>Represents a thread-safe first in-first out (FIFO) collection.</summary>
  /// <typeparam name="T">The type of the elements contained in the queue.</typeparam>
  [ThreadSafe]
  public class ConcurrentQueueCopy<T> : 
    IProducerConsumerCollection<T>,
    IEnumerable<T>,
    IEnumerable,
    ICollection,
    IReadOnlyCollection<T>
  {
    #nullable disable
    private readonly object _crossSegmentLock;
    private volatile ConcurrentQueueSegmentCopy<T> _tail;
    private volatile ConcurrentQueueSegmentCopy<T> _head;

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> class.</summary>
    public ConcurrentQueueCopy()
    {
      this._crossSegmentLock = new object();
      this._tail = this._head = new ConcurrentQueueSegmentCopy<T>(32);
    }

    #nullable enable
    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> class that contains elements copied from the specified collection.</summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</param>
    /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection" /> argument is null.</exception>
    public ConcurrentQueueCopy(IEnumerable<T> collection)
    {
      if (collection == null)
        ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.collection);
      this._crossSegmentLock = new object();
      int boundedLength = 32;
      if (collection is ICollection<T> objs)
      {
        int count = objs.Count;
        if (count > boundedLength)
          boundedLength = (int) Math.Min(BitOperations.RoundUpToPowerOf2((uint) count), 1048576U);
      }
      this._tail = this._head = new ConcurrentQueueSegmentCopy<T>(boundedLength);
      foreach (T obj in collection)
        this.Enqueue(obj);
    }

    #nullable disable
    /// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from the <see cref="T:System.Collections.Concurrent.ConcurrentBag`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is a null reference (Nothing in Visual Basic).</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than zero.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" /> is multidimensional. -or- <paramref name="array" /> does not have zero-based indexing. -or- <paramref name="index" /> is equal to or greater than the length of the <paramref name="array" /> -or- The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />. -or- The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />.</exception>
    void ICollection.CopyTo(Array array, int index)
    {
      if (array is T[] array1)
      {
        this.CopyTo(array1, index);
      }
      else
      {
        if (array == null)
          ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.array);
        this.ToArray().CopyTo(array, index);
      }
    }

    /// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized with the SyncRoot.</summary>
    /// <returns>Always returns <see langword="false" /> to indicate access is not synchronized.</returns>
    bool ICollection.IsSynchronized => false;

    #nullable enable
    /// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />. This property is not supported.</summary>
    /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported.</exception>
    /// <returns>Returns <see langword="null" />.</returns>
    object ICollection.SyncRoot
    {
      get
      {
        ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.array);
        return (object) null;
      }
    }

    #nullable disable
    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

    /// <summary>Attempts to add an object to the <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection`1" />.</summary>
    /// <param name="item">The object to add to the <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection`1" />. The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if the object was added successfully; otherwise, <see langword="false" />.</returns>
    bool IProducerConsumerCollection<T>.TryAdd(T item)
    {
      this.Enqueue(item);
      return true;
    }

    /// <summary>Attempts to remove and return an object from the <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection`1" />.</summary>
    /// <param name="item">When this method returns, if the operation was successful, <paramref name="item" /> contains the object removed. If no object was available to be removed, the value is unspecified.</param>
    /// <returns>
    /// <see langword="true" /> if an element was removed and returned successfully; otherwise, <see langword="false" />.</returns>
    bool IProducerConsumerCollection<T>.TryTake([MaybeNullWhen(false)] out T item)
    {
      return this.TryDequeue(out item);
    }

    /// <summary>Gets a value that indicates whether the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> is empty.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> is empty; otherwise, <see langword="false" />.</returns>
    public bool IsEmpty => !this.TryPeek(out T _, false);

    #nullable enable
    /// <summary>Copies the elements stored in the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> to a new array.</summary>
    /// <returns>A new array containing a snapshot of elements copied from the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</returns>
    public T[] ToArray()
    {
      ConcurrentQueueSegmentCopy<T> head;
      int headHead;
      ConcurrentQueueSegmentCopy<T> tail;
      int tailTail;
      this.SnapForObservation(out head, out headHead, out tail, out tailTail);
      T[] array = new T[ConcurrentQueueCopy<T>.GetCount(head, headHead, tail, tailTail)];
      using (IEnumerator<T> enumerator = ConcurrentQueueCopy<T>.Enumerate(head, headHead, tail, tailTail))
      {
        int num = 0;
        while (enumerator.MoveNext())
          array[num++] = enumerator.Current;
      }
      return array;
    }

    /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</summary>
    /// <returns>The number of elements contained in the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</returns>
    public int Count
    {
      get
      {
        SpinWait spinWait = new SpinWait();
        ConcurrentQueueSegmentCopy<T> head1;
        ConcurrentQueueSegmentCopy<T> tail1;
        int head2;
        int tail2;
        int head3;
        int tail3;
        while (true)
        {
          head1 = this._head;
          tail1 = this._tail;
          head2 = Volatile.Read(ref head1._headAndTail.Head);
          tail2 = Volatile.Read(ref head1._headAndTail.Tail);
          if (head1 == tail1)
          {
            if (head1 == this._head && tail1 == this._tail && head2 == Volatile.Read(ref head1._headAndTail.Head) && tail2 == Volatile.Read(ref head1._headAndTail.Tail))
              break;
          }
          else if (head1.NextSegmentCopy == tail1)
          {
            head3 = Volatile.Read(ref tail1._headAndTail.Head);
            tail3 = Volatile.Read(ref tail1._headAndTail.Tail);
            if (head1 == this._head && tail1 == this._tail && head2 == Volatile.Read(ref head1._headAndTail.Head) && tail2 == Volatile.Read(ref head1._headAndTail.Tail) && head3 == Volatile.Read(ref tail1._headAndTail.Head) && tail3 == Volatile.Read(ref tail1._headAndTail.Tail))
              goto label_6;
          }
          else
          {
            lock (this._crossSegmentLock)
            {
              if (head1 == this._head)
              {
                if (tail1 == this._tail)
                {
                  int head4 = Volatile.Read(ref tail1._headAndTail.Head);
                  int tail4 = Volatile.Read(ref tail1._headAndTail.Tail);
                  if (head2 == Volatile.Read(ref head1._headAndTail.Head))
                  {
                    if (tail2 == Volatile.Read(ref head1._headAndTail.Tail))
                    {
                      if (head4 == Volatile.Read(ref tail1._headAndTail.Head))
                      {
                        if (tail4 == Volatile.Read(ref tail1._headAndTail.Tail))
                        {
                          int count = ConcurrentQueueCopy<T>.GetCount(head1, head2, tail2) + ConcurrentQueueCopy<T>.GetCount(tail1, head4, tail4);
                          for (ConcurrentQueueSegmentCopy<T> nextSegmentCopy = head1.NextSegmentCopy; nextSegmentCopy != tail1; nextSegmentCopy = nextSegmentCopy.NextSegmentCopy)
                            count += nextSegmentCopy._headAndTail.Tail - nextSegmentCopy.FreezeOffset;
                          return count;
                        }
                      }
                    }
                  }
                }
              }
            }
          }
          spinWait.SpinOnce();
        }
        return ConcurrentQueueCopy<T>.GetCount(head1, head2, tail2);
label_6:
        return ConcurrentQueueCopy<T>.GetCount(head1, head2, tail2) + ConcurrentQueueCopy<T>.GetCount(tail1, head3, tail3);
      }
    }

    #nullable disable
    private static int GetCount(ConcurrentQueueSegmentCopy<T> s, int head, int tail)
    {
      if (head == tail || head == tail - s.FreezeOffset)
        return 0;
      head &= s._slotsMask;
      tail &= s._slotsMask;
      return head >= tail ? s._slots.Length - head + tail : tail - head;
    }

    private static long GetCount(
      ConcurrentQueueSegmentCopy<T> head,
      int headHead,
      ConcurrentQueueSegmentCopy<T> tail,
      int tailTail)
    {
      long count = 0;
      int num1 = (head == tail ? tailTail : Volatile.Read(ref head._headAndTail.Tail)) - head.FreezeOffset;
      if (headHead < num1)
      {
        headHead &= head._slotsMask;
        int num2 = num1 & head._slotsMask;
        count += headHead < num2 ? (long) (num2 - headHead) : (long) (head._slots.Length - headHead + num2);
      }
      if (head != tail)
      {
        for (ConcurrentQueueSegmentCopy<T> nextSegmentCopy = head.NextSegmentCopy; nextSegmentCopy != tail; nextSegmentCopy = nextSegmentCopy.NextSegmentCopy)
          count += (long) (nextSegmentCopy._headAndTail.Tail - nextSegmentCopy.FreezeOffset);
        count += (long) (tailTail - tail.FreezeOffset);
      }
      return count;
    }

    #nullable enable
    /// <summary>Copies the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> elements to an existing one-dimensional <see cref="T:System.Array" />, starting at the specified array index.</summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is a null reference (Nothing in Visual Basic).</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than zero.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> is equal to or greater than the length of the <paramref name="array" /> -or- The number of elements in the source <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.</exception>
    public void CopyTo(T[] array, int index)
    {
      if (array == null)
        ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.array);
      if (index < 0)
        ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.index);
      ConcurrentQueueSegmentCopy<T> head;
      int headHead;
      ConcurrentQueueSegmentCopy<T> tail;
      int tailTail;
      this.SnapForObservation(out head, out headHead, out tail, out tailTail);
      long count = ConcurrentQueueCopy<T>.GetCount(head, headHead, tail, tailTail);
      if ((long) index > (long) array.Length - count)
        ThrowHelperCopy.ThrowArgumentNullException(ExceptionArgumentCopy.chars);
      int num = index;
      using (IEnumerator<T> enumerator = ConcurrentQueueCopy<T>.Enumerate(head, headHead, tail, tailTail))
      {
        while (enumerator.MoveNext())
          array[num++] = enumerator.Current;
      }
    }

    /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</summary>
    /// <returns>An enumerator for the contents of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</returns>
    public IEnumerator<T> GetEnumerator()
    {
      ConcurrentQueueSegmentCopy<T> head;
      int headHead;
      ConcurrentQueueSegmentCopy<T> tail;
      int tailTail;
      this.SnapForObservation(out head, out headHead, out tail, out tailTail);
      return ConcurrentQueueCopy<T>.Enumerate(head, headHead, tail, tailTail);
    }

    #nullable disable
    private void SnapForObservation(
      out ConcurrentQueueSegmentCopy<T> head,
      out int headHead,
      out ConcurrentQueueSegmentCopy<T> tail,
      out int tailTail)
    {
      lock (this._crossSegmentLock)
      {
        head = this._head;
        tail = this._tail;
        ConcurrentQueueSegmentCopy<T> concurrentQueueSegmentCopy = head;
        while (true)
        {
          concurrentQueueSegmentCopy._preservedForObservation = true;
          if (concurrentQueueSegmentCopy != tail)
            concurrentQueueSegmentCopy = concurrentQueueSegmentCopy.NextSegmentCopy;
          else
            break;
        }
        tail.EnsureFrozenForEnqueues();
        headHead = Volatile.Read(ref head._headAndTail.Head);
        tailTail = Volatile.Read(ref tail._headAndTail.Tail);
      }
    }

    private static T GetItemWhenAvailable(ConcurrentQueueSegmentCopy<T> segmentCopy, int i)
    {
      int num = i + 1 & segmentCopy._slotsMask;
      SpinWait spinWait = new SpinWait();
      while ((Volatile.Read(ref segmentCopy._slots[i].SequenceNumber) & segmentCopy._slotsMask) != num)
        spinWait.SpinOnce();
      return segmentCopy._slots[i].Item;
    }

    private static IEnumerator<T> Enumerate(
      ConcurrentQueueSegmentCopy<T> head,
      int headHead,
      ConcurrentQueueSegmentCopy<T> tail,
      int tailTail)
    {
      int headTail = (head == tail ? tailTail : Volatile.Read(ref head._headAndTail.Tail)) - head.FreezeOffset;
      int i1;
      if (headHead < headTail)
      {
        headHead &= head._slotsMask;
        headTail &= head._slotsMask;
        if (headHead < headTail)
        {
          for (i1 = headHead; i1 < headTail; ++i1)
            yield return ConcurrentQueueCopy<T>.GetItemWhenAvailable(head, i1);
        }
        else
        {
          for (i1 = headHead; i1 < head._slots.Length; ++i1)
            yield return ConcurrentQueueCopy<T>.GetItemWhenAvailable(head, i1);
          for (i1 = 0; i1 < headTail; ++i1)
            yield return ConcurrentQueueCopy<T>.GetItemWhenAvailable(head, i1);
        }
      }
      if (head != tail)
      {
        ConcurrentQueueSegmentCopy<T> s;
        for (s = head.NextSegmentCopy; s != tail; s = s.NextSegmentCopy)
        {
          i1 = s._headAndTail.Tail - s.FreezeOffset;
          for (int i2 = 0; i2 < i1; ++i2)
            yield return ConcurrentQueueCopy<T>.GetItemWhenAvailable(s, i2);
        }
        s = (ConcurrentQueueSegmentCopy<T>) null;
        tailTail -= tail.FreezeOffset;
        for (i1 = 0; i1 < tailTail; ++i1)
          yield return ConcurrentQueueCopy<T>.GetItemWhenAvailable(tail, i1);
      }
    }

    #nullable enable
    /// <summary>Adds an object to the end of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</summary>
    /// <param name="item">The object to add to the end of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />. The value can be a null reference (Nothing in Visual Basic) for reference types.</param>
    public void Enqueue(T item)
    {
      if (this._tail.TryEnqueue(item))
        return;
      this.EnqueueSlow(item);
    }

    #nullable disable
    private void EnqueueSlow(T item)
    {
      while (true)
      {
        ConcurrentQueueSegmentCopy<T> tail = this._tail;
        if (!tail.TryEnqueue(item))
        {
          lock (this._crossSegmentLock)
          {
            if (tail == this._tail)
            {
              tail.EnsureFrozenForEnqueues();
              ConcurrentQueueSegmentCopy<T> concurrentQueueSegmentCopy = new ConcurrentQueueSegmentCopy<T>(tail._preservedForObservation ? 32 : Math.Min(tail.Capacity * 2, 1048576));
              tail.NextSegmentCopy = concurrentQueueSegmentCopy;
              this._tail = concurrentQueueSegmentCopy;
            }
          }
        }
        else
          break;
      }
    }

    #nullable enable
    /// <summary>Tries to remove and return the object at the beginning of the concurrent queue.</summary>
    /// <param name="result">When this method returns, if the operation was successful, <paramref name="result" /> contains the object removed. If no object was available to be removed, the value is unspecified.</param>
    /// <returns>
    /// <see langword="true" /> if an element was removed and returned from the beginning of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> successfully; otherwise, <see langword="false" />.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T result)
    {
      ConcurrentQueueSegmentCopy<T> head = this._head;
      if (head.TryDequeue(out result))
        return true;
      if (head.NextSegmentCopy != null)
        return this.TryDequeueSlow(out result);
      result = default (T);
      return false;
    }

    #nullable disable
    private bool TryDequeueSlow([MaybeNullWhen(false)] out T item)
    {
      while (true)
      {
        ConcurrentQueueSegmentCopy<T> head = this._head;
        if (!head.TryDequeue(out item))
        {
          if (head.NextSegmentCopy != null)
          {
            if (!head.TryDequeue(out item))
            {
              lock (this._crossSegmentLock)
              {
                if (head == this._head)
                  this._head = head.NextSegmentCopy;
              }
            }
            else
              goto label_5;
          }
          else
            goto label_3;
        }
        else
          break;
      }
      return true;
label_3:
      item = default (T);
      return false;
label_5:
      return true;
    }

    #nullable enable
    /// <summary>Tries to return an object from the beginning of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> without removing it.</summary>
    /// <param name="result">When this method returns, <paramref name="result" /> contains an object from the beginning of the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" /> or an unspecified value if the operation failed.</param>
    /// <returns>
    /// <see langword="true" /> if an object was returned successfully; otherwise, <see langword="false" />.</returns>
    public bool TryPeek([MaybeNullWhen(false)] out T result) => this.TryPeek(out result, true);

    #nullable disable
    private bool TryPeek([MaybeNullWhen(false)] out T result, bool resultUsed)
    {
      ConcurrentQueueSegmentCopy<T> concurrentQueueSegment1 = this._head;
      do
      {
        ConcurrentQueueSegmentCopy<T> concurrentQueueSegment2 = Volatile.Read<ConcurrentQueueSegmentCopy<T>>(ref concurrentQueueSegment1.NextSegmentCopy);
        if (concurrentQueueSegment1.TryPeek(out result, resultUsed))
          return true;
        if (concurrentQueueSegment2 != null)
          concurrentQueueSegment1 = concurrentQueueSegment2;
      }
      while (Volatile.Read<ConcurrentQueueSegmentCopy<T>>(ref concurrentQueueSegment1.NextSegmentCopy) != null);
      result = default (T);
      return false;
    }

    /// <summary>Removes all objects from the <see cref="T:System.Collections.Concurrent.ConcurrentQueue`1" />.</summary>
    public void Clear()
    {
      lock (this._crossSegmentLock)
      {
        this._tail.EnsureFrozenForEnqueues();
        this._tail = this._head = new ConcurrentQueueSegmentCopy<T>(32);
      }
    }
  }
  
  internal sealed class ConcurrentQueueSegmentCopy<T>
  {
    internal readonly ConcurrentQueueSegmentCopy<T>.Slot[] _slots;
    internal readonly int _slotsMask;
    internal PaddedHeadAndTailCopy _headAndTail;
    internal bool _preservedForObservation;
    internal bool _frozenForEnqueues;
    internal ConcurrentQueueSegmentCopy<T> NextSegmentCopy;

    internal ConcurrentQueueSegmentCopy(int boundedLength)
    {
      this._slots = new ConcurrentQueueSegmentCopy<T>.Slot[boundedLength];
      this._slotsMask = boundedLength - 1;
      for (int index = 0; index < this._slots.Length; ++index)
        this._slots[index].SequenceNumber = index;
    }

    internal int Capacity => this._slots.Length;

    internal int FreezeOffset => this._slots.Length * 2;

    internal void EnsureFrozenForEnqueues()
    {
      if (this._frozenForEnqueues)
        return;
      this._frozenForEnqueues = true;
      Interlocked.Add(ref this._headAndTail.Tail, this.FreezeOffset);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
      ConcurrentQueueSegmentCopy<T>.Slot[] slots = this._slots;
      SpinWait spinWait = new SpinWait();
      while (true)
      {
        int comparand;
        int num1;
        do
        {
          comparand = Volatile.Read(ref this._headAndTail.Head);
          int index = comparand & this._slotsMask;
          num1 = Volatile.Read(ref slots[index].SequenceNumber) - (comparand + 1);
          if (num1 == 0)
          {
            if (Interlocked.CompareExchange(ref this._headAndTail.Head, comparand + 1, comparand) == comparand)
            {
              item = slots[index].Item;
              if (!Volatile.Read(ref this._preservedForObservation))
              {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                  slots[index].Item = default (T);
                Volatile.Write(ref slots[index].SequenceNumber, comparand + slots.Length);
              }
              return true;
            }
          }
        }
        while (num1 >= 0);
        bool frozenForEnqueues = this._frozenForEnqueues;
        int num2 = Volatile.Read(ref this._headAndTail.Tail);
        if (num2 - comparand > 0 && (!frozenForEnqueues || num2 - this.FreezeOffset - comparand > 0))
          spinWait.SpinOnce(-1);
        else
          break;
      }
      item = default (T);
      return false;
    }

    public bool TryPeek([MaybeNullWhen(false)] out T result, bool resultUsed)
    {
      if (resultUsed)
      {
        this._preservedForObservation = true;
        Interlocked.MemoryBarrier();
      }
      ConcurrentQueueSegmentCopy<T>.Slot[] slots = this._slots;
      SpinWait spinWait = new SpinWait();
      while (true)
      {
        int num1;
        int num2;
        do
        {
          num1 = Volatile.Read(ref this._headAndTail.Head);
          int index = num1 & this._slotsMask;
          num2 = Volatile.Read(ref slots[index].SequenceNumber) - (num1 + 1);
          if (num2 == 0)
          {
            result = resultUsed ? slots[index].Item : default (T);
            return true;
          }
        }
        while (num2 >= 0);
        bool frozenForEnqueues = this._frozenForEnqueues;
        int num3 = Volatile.Read(ref this._headAndTail.Tail);
        if (num3 - num1 > 0 && (!frozenForEnqueues || num3 - this.FreezeOffset - num1 > 0))
          spinWait.SpinOnce(-1);
        else
          break;
      }
      result = default (T);
      return false;
    }

    public bool TryEnqueue(T item)
    {
      ConcurrentQueueSegmentCopy<T>.Slot[] slots = this._slots;
      int num;
      do
      {
        int comparand = Volatile.Read(ref this._headAndTail.Tail);
        int index = comparand & this._slotsMask;
        num = Volatile.Read(ref slots[index].SequenceNumber) - comparand;
        if (num == 0)
        {
          if (Interlocked.CompareExchange(ref this._headAndTail.Tail, comparand + 1, comparand) == comparand)
          {
            slots[index].Item = item;
            Volatile.Write(ref slots[index].SequenceNumber, comparand + 1);
            return true;
          }
        }
      }
      while (num >= 0);
      return false;
    }
    
    internal struct Slot
    {
      public T Item;
      public int SequenceNumber;
    }
  }
  
  internal enum ExceptionArgumentCopy
  {
    obj,
    dictionary,
    array,
    info,
    key,
    text,
    values,
    value,
    startIndex,
    task,
    bytes,
    byteIndex,
    byteCount,
    ch,
    chars,
    charIndex,
    charCount,
    s,
    input,
    ownedMemory,
    list,
    index,
    capacity,
    collection,
    item,
    converter,
    match,
    count,
    action,
    comparison,
    exceptions,
    exception,
    pointer,
    start,
    format,
    formats,
    culture,
    comparer,
    comparable,
    source,
    length,
    comparisonType,
    manager,
    sourceBytesToCopy,
    callBack,
    creationOptions,
    function,
    scheduler,
    continuation,
    continuationAction,
    continuationFunction,
    tasks,
    asyncResult,
    beginMethod,
    endMethod,
    endFunction,
    cancellationToken,
    continuationOptions,
    delay,
    millisecondsDelay,
    millisecondsTimeout,
    stateMachine,
    timeout,
    type,
    sourceIndex,
    sourceArray,
    destinationIndex,
    destinationArray,
    pHandle,
    handle,
    other,
    newSize,
    lowerBounds,
    lengths,
    len,
    keys,
    indices,
    index1,
    index2,
    index3,
    length1,
    length2,
    length3,
    endIndex,
    elementType,
    arrayIndex,
    year,
    codePoint,
    str,
    options,
    prefix,
    suffix,
    buffer,
    buffers,
    offset,
    stream,
    anyOf,
    overlapped,
    minimumBytes,
  }

  [StructLayout(LayoutKind.Explicit, Size = 384)]
  internal struct PaddedHeadAndTailCopy
  {
    [FieldOffset(128)]
    public int Head;
    [FieldOffset(256)]
    public int Tail;
  }

  // ThrowHelper.ThrowArgumentNullException(ExceptionArgumentCopy.array);

  internal static class ThrowHelperCopy
  {
    public static void ThrowArgumentNullException(ExceptionArgumentCopy argument)
    {
      
    }
  }