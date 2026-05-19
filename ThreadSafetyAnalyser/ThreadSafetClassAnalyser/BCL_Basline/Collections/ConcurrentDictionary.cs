// ReSharper disable RedundantUsingDirective
using System.Collections;
using System.Collections.Concurrent;
using Annotations;

// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable RedundantNameQualifier
// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable ArrangeThisQualifier
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable RedundantJumpStatement
// ReSharper disable RedundantExplicitNullableCreation
// ReSharper disable InvalidXmlDocComment
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ByRefArgumentIsVolatileField
// ReSharper disable RedundantNullableDirective
// ReSharper disable NotResolvedInText
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace BCL_Basline.Collections;

// Decompiled with JetBrains decompiler
// Type: System.Collections.Concurrent.ConcurrentDictionary`2
// Assembly: System.Collections.Concurrent, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 34BD86B0-DEC9-449C-AF31-862E0862CD56
// Assembly location: /usr/local/share/dotnet/shared/Microsoft.NETCore.App/8.0.20/System.Collections.Concurrent.dll
// XML documentation location: /usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.3/ref/net8.0/System.Collections.Concurrent.xml

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ReturnTypeCanBeNotNullable
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable RedundantBaseConstructorCall

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable UnusedMember.Global
// ReSharper disable NonAtomicCompoundOperator
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable UsePatternMatching
// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable UseCollectionExpression
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable EmptyEmbeddedStatement
// ReSharper disable NotAccessedOutParameterVariable
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable ArrangeDefaultValueWhenTypeNotEvident
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantCast

#nullable enable

/// <summary>Represents a thread-safe collection of key/value pairs that can be accessed by multiple threads concurrently.</summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[DebuggerTypeProxy(typeof (IDictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
[ThreadSafe]
public class ConcurrentDictionary<TKey, TValue> : 
  IDictionary<TKey, TValue>,
  ICollection<KeyValuePair<TKey, TValue>>,
  IEnumerable<KeyValuePair<TKey, TValue>>,
  IEnumerable,
  IDictionary,
  ICollection,
  IReadOnlyDictionary<TKey, TValue>,
  IReadOnlyCollection<KeyValuePair<TKey, TValue>>
  where TKey : notnull
{
  #nullable disable
  private volatile ConcurrentDictionary<TKey, TValue>.Tables _tables;
  private int _budget;
  private readonly bool _growLockArray;
  private readonly bool _comparerIsDefaultForClasses;

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that is empty, has the default concurrency level, has the default initial capacity, and uses the default comparer for the key type.</summary>
  public ConcurrentDictionary()
    : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, 31, true, (IEqualityComparer<TKey>) null)
  {
  }

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that is empty, has the specified concurrency level and capacity, and uses the default comparer for the key type.</summary>
  /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> concurrently, or in .NET 8+ only, -1 to indicate the default concurrency level.</param>
  /// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> can contain.</param>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  ///        <paramref name="concurrencyLevel" /> is less than 1.
  /// 
  /// -or-
  /// 
  /// <paramref name="capacity" /> is less than 0.</exception>
  public ConcurrentDictionary(int concurrencyLevel, int capacity)
    : this(concurrencyLevel, capacity, false, (IEqualityComparer<TKey>) null)
  {
  }

  #nullable enable
  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that contains elements copied from the specified <see cref="T:System.Collections.Generic.IEnumerable`1" />, has the default concurrency level, has the default initial capacity, and uses the default comparer for the key type.</summary>
  /// <param name="collection">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are copied to the new <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="collection" /> or any of its keys is  <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentException">
  /// <paramref name="collection" /> contains one or more duplicate keys.</exception>
  public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
    : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, collection, (IEqualityComparer<TKey>) null)
  {
  }

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that is empty, has the default concurrency level and capacity, and uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</summary>
  /// <param name="comparer">The equality comparison implementation to use when comparing keys.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="comparer" /> is <see langword="null" />.</exception>
  public ConcurrentDictionary(IEqualityComparer<TKey>? comparer)
    : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, 31, true, comparer)
  {
  }

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that contains elements copied from the specified <see cref="T:System.Collections.IEnumerable" /> has the default concurrency level, has the default initial capacity, and uses the specified  <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</summary>
  /// <param name="collection">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are copied to the new <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
  /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing keys.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="collection" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
  public ConcurrentDictionary(
    IEnumerable<KeyValuePair<TKey, TValue>> collection,
    IEqualityComparer<TKey>? comparer)
    : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, ConcurrentDictionary<TKey, TValue>.GetCapacityFromCollection(collection), comparer)
  {
    ArgumentNullException.ThrowIfNull((object) collection, nameof (collection));
    this.InitializeFromCollection(collection);
  }

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that contains elements copied from the specified <see cref="T:System.Collections.IEnumerable" />, and uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</summary>
  /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> concurrently, or in .NET 8+ only, -1 to indicate the default concurrency level.</param>
  /// <param name="collection">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are copied to the new <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
  /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing keys.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="collection" /> or <paramref name="comparer" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  /// <paramref name="concurrencyLevel" /> is less than 1.</exception>
  /// <exception cref="T:System.ArgumentException">
  /// <paramref name="collection" /> contains one or more duplicate keys.</exception>
  public ConcurrentDictionary(
    int concurrencyLevel,
    IEnumerable<KeyValuePair<TKey, TValue>> collection,
    IEqualityComparer<TKey>? comparer)
    : this(concurrencyLevel, ConcurrentDictionary<TKey, TValue>.GetCapacityFromCollection(collection), false, comparer)
  {
    ArgumentNullException.ThrowIfNull((object) collection, nameof (collection));
    this.InitializeFromCollection(collection);
  }

  /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> class that is empty, has the specified concurrency level, has the specified initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</summary>
  /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> concurrently, or in .NET 8+ only, -1 to indicate the default concurrency level.</param>
  /// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> can contain.</param>
  /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing keys.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="comparer" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  /// <paramref name="concurrencyLevel" /> or <paramref name="capacity" /> is less than 1.</exception>
  public ConcurrentDictionary(
    int concurrencyLevel,
    int capacity,
    IEqualityComparer<TKey>? comparer)
    : this(concurrencyLevel, capacity, false, comparer)
  {
  }

  #nullable disable
  internal ConcurrentDictionary(
    int concurrencyLevel,
    int capacity,
    bool growLockArray,
    IEqualityComparer<TKey> comparer)
  {
    if (concurrencyLevel <= 0)
      concurrencyLevel = concurrencyLevel == -1 ? ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel : throw new ArgumentOutOfRangeException(nameof (concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositiveOrNegativeOne);
    ArgumentOutOfRangeException.ThrowIfNegative<int>(capacity, nameof (capacity));
    if (capacity < concurrencyLevel)
      capacity = concurrencyLevel;
    capacity = HashHelpers.GetPrime(capacity);
    object[] locks = new object[concurrencyLevel];
    locks[0] = (object) locks;
    for (int index = 1; index < locks.Length; ++index)
      locks[index] = new object();
    int[] countPerLock = new int[locks.Length];
    ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets = new ConcurrentDictionary<TKey, TValue>.VolatileNode[capacity];
    if (typeof (TKey).IsValueType)
    {
      if (comparer != null && comparer == EqualityComparer<TKey>.Default)
        comparer = (IEqualityComparer<TKey>) null;
    }
    else
    {
      if (comparer == null)
        comparer = (IEqualityComparer<TKey>) EqualityComparer<TKey>.Default;
      if (typeof (TKey) == typeof (string))
      {
        IEqualityComparer<string> stringComparer = NonRandomizedStringEqualityComparer.GetStringComparer((object) comparer);
        if (stringComparer != null)
        {
          comparer = (IEqualityComparer<TKey>) stringComparer;
          goto label_19;
        }
      }
      if (comparer == EqualityComparer<TKey>.Default)
        this._comparerIsDefaultForClasses = true;
    }
label_19:
    this._tables = new ConcurrentDictionary<TKey, TValue>.Tables(buckets, locks, countPerLock, comparer);
    this._growLockArray = growLockArray;
    this._budget = buckets.Length / locks.Length;
  }

  private static int GetCapacityFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
  {
    int capacityFromCollection;
    switch (collection)
    {
      case ICollection<KeyValuePair<TKey, TValue>> keyValuePairs1:
        capacityFromCollection = Math.Max(31, keyValuePairs1.Count);
        break;
      case IReadOnlyCollection<KeyValuePair<TKey, TValue>> keyValuePairs2:
        capacityFromCollection = Math.Max(31, keyValuePairs2.Count);
        break;
      default:
        capacityFromCollection = 31;
        break;
    }
    return capacityFromCollection;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private int GetHashCode(IEqualityComparer<TKey> comparer, TKey key)
  {
    return typeof (TKey).IsValueType ? (comparer != null ? comparer.GetHashCode(key) : key.GetHashCode()) : (!this._comparerIsDefaultForClasses ? comparer.GetHashCode(key) : key.GetHashCode());
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool NodeEqualsKey(
    IEqualityComparer<TKey> comparer,
    ConcurrentDictionary<TKey, TValue>.Node node,
    TKey key)
  {
    return typeof (TKey).IsValueType && comparer == null ? EqualityComparer<TKey>.Default.Equals(node._key, key) : comparer.Equals(node._key, key);
  }

  private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
  {
    foreach (KeyValuePair<TKey, TValue> keyValuePair in collection)
    {
      if ((object) keyValuePair.Key == null)
        ThrowHelper.ThrowKeyNullException();
      if (!this.TryAddInternal(this._tables, keyValuePair.Key, new int?(), keyValuePair.Value, false, false, out TValue _))
        throw new ArgumentException(SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
    }
    if (this._budget != 0)
      return;
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    this._budget = tables._buckets.Length / tables._locks.Length;
  }

  #nullable enable
  /// <summary>Attempts to add the specified key and value to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <param name="key">The key of the element to add.</param>
  /// <param name="value">The value of the element to add. The value can be  <see langword="null" /> for reference types.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is  <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>
  /// <see langword="true" /> if the key/value pair was added to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> successfully; <see langword="false" /> if the key already exists.</returns>
  public bool TryAdd(TKey key, TValue value)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    return this.TryAddInternal(this._tables, key, new int?(), value, false, true, out TValue _);
  }

  /// <summary>Determines whether the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains the specified key.</summary>
  /// <param name="key">The key to locate in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> contains an element with the specified key; otherwise, <see langword="false" />.</returns>
  public bool ContainsKey(TKey key) => this.TryGetValue(key, out TValue _);

  /// <summary>Attempts to remove and return the value that has the specified key from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <param name="key">The key of the element to remove and return.</param>
  /// <param name="value">When this method returns, contains the object removed from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />, or the default value of  the <see langword="TValue" /> type if <paramref name="key" /> does not exist.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is  <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the object was removed successfully; otherwise, <see langword="false" />.</returns>
  public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    return this.TryRemoveInternal(key, out value, false, default (TValue));
  }

  /// <summary>Removes a key and value from the dictionary.</summary>
  /// <param name="item">The <see cref="T:System.Collections.Generic.KeyValuePair`2" /> representing the key and value to remove.</param>
  /// <exception cref="T:System.ArgumentNullException">The <see cref="P:System.Collections.Generic.KeyValuePair`2.Key" /> property of <paramref name="item" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the key and value represented by <paramref name="item" /> are successfully found and removed; otherwise, <see langword="false" />.</returns>
  public bool TryRemove(KeyValuePair<TKey, TValue> item)
  {
    if ((object) item.Key == null)
      ThrowHelper.ThrowArgumentNullException(nameof (item), SR.ConcurrentDictionary_ItemKeyIsNull);
    return this.TryRemoveInternal(item.Key, out TValue _, true, item.Value);
  }

  #nullable disable
  private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)] out TValue value, bool matchValue, TValue oldValue)
  {
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashCode = this.GetHashCode(comparer, key);
    while (true)
    {
      object[] locks = tables._locks;
      uint lockNo;
      ref ConcurrentDictionary<TKey, TValue>.Node local = ref ConcurrentDictionary<TKey, TValue>.GetBucketAndLock(tables, hashCode, out lockNo);
      lock (locks[(int) lockNo])
      {
        if (tables != this._tables)
        {
          tables = this._tables;
          if (comparer != tables._comparer)
          {
            comparer = tables._comparer;
            hashCode = this.GetHashCode(comparer, key);
          }
        }
        else
        {
          ConcurrentDictionary<TKey, TValue>.Node node1 = (ConcurrentDictionary<TKey, TValue>.Node) null;
          for (ConcurrentDictionary<TKey, TValue>.Node node2 = local; node2 != null; node2 = node2._next)
          {
            if (hashCode == node2._hashcode && ConcurrentDictionary<TKey, TValue>.NodeEqualsKey(comparer, node2, key))
            {
              if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, node2._value))
              {
                value = default (TValue);
                return false;
              }
              if (node1 == null)
                Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref local, node2._next);
              else
                node1._next = node2._next;
              value = node2._value;
              --tables._countPerLock[(int) lockNo];
              return true;
            }
            node1 = node2;
          }
          break;
        }
      }
    }
    value = default (TValue);
    return false;
  }

  #nullable enable
  /// <summary>Attempts to get the value associated with the specified key from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <param name="key">The key of the value to get.</param>
  /// <param name="value">When this method returns, contains the object from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> that has the specified key, or the default value of the type if the operation failed.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is  <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the key was found in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />; otherwise, <see langword="false" />.</returns>
  public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = tables._comparer;
    if (typeof (TKey).IsValueType && comparer == null)
    {
      int hashCode = key.GetHashCode();
      for (ConcurrentDictionary<TKey, TValue>.Node node = ConcurrentDictionary<TKey, TValue>.GetBucket(tables, hashCode); node != null; node = node._next)
      {
        if (hashCode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
        {
          value = node._value;
          return true;
        }
      }
    }
    else
    {
      int hashCode = this.GetHashCode(comparer, key);
      for (ConcurrentDictionary<TKey, TValue>.Node node = ConcurrentDictionary<TKey, TValue>.GetBucket(tables, hashCode); node != null; node = node._next)
      {
        if (hashCode == node._hashcode && comparer.Equals(node._key, key))
        {
          value = node._value;
          return true;
        }
      }
    }
    value = default (TValue);
    return false;
  }

  #nullable disable
  private static bool TryGetValueInternal(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    TKey key,
    int hashcode,
    [MaybeNullWhen(false)] out TValue value)
  {
    IEqualityComparer<TKey> comparer = tables._comparer;
    if (typeof (TKey).IsValueType && comparer == null)
    {
      for (ConcurrentDictionary<TKey, TValue>.Node node = ConcurrentDictionary<TKey, TValue>.GetBucket(tables, hashcode); node != null; node = node._next)
      {
        if (hashcode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
        {
          value = node._value;
          return true;
        }
      }
    }
    else
    {
      for (ConcurrentDictionary<TKey, TValue>.Node node = ConcurrentDictionary<TKey, TValue>.GetBucket(tables, hashcode); node != null; node = node._next)
      {
        if (hashcode == node._hashcode && comparer.Equals(node._key, key))
        {
          value = node._value;
          return true;
        }
      }
    }
    value = default (TValue);
    return false;
  }

  #nullable enable
  /// <summary>Updates the value associated with <paramref name="key" /> to <paramref name="newValue" /> if the existing value with <paramref name="key" /> is equal to <paramref name="comparisonValue" />.</summary>
  /// <param name="key">The key of the value that is compared with <paramref name="comparisonValue" /> and possibly replaced.</param>
  /// <param name="newValue">The value that replaces the value of the element that has the specified <paramref name="key" /> if the comparison results in equality.</param>
  /// <param name="comparisonValue">The value that is compared with the value of the element that has the specified <paramref name="key" />.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the value with <paramref name="key" /> was equal to <paramref name="comparisonValue" /> and was replaced with <paramref name="newValue" />; otherwise, <see langword="false" />.</returns>
  public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    return this.TryUpdateInternal(this._tables, key, new int?(), newValue, comparisonValue);
  }

  #nullable disable
  private bool TryUpdateInternal(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    TKey key,
    int? nullableHashcode,
    TValue newValue,
    TValue comparisonValue)
  {
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashcode = nullableHashcode ?? this.GetHashCode(comparer, key);
    EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
    while (true)
    {
      object[] locks = tables._locks;
      uint lockNo;
      ref ConcurrentDictionary<TKey, TValue>.Node local = ref ConcurrentDictionary<TKey, TValue>.GetBucketAndLock(tables, hashcode, out lockNo);
      lock (locks[(int) lockNo])
      {
        if (tables != this._tables)
        {
          tables = this._tables;
          if (comparer != tables._comparer)
          {
            comparer = tables._comparer;
            hashcode = this.GetHashCode(comparer, key);
          }
        }
        else
        {
          ConcurrentDictionary<TKey, TValue>.Node node1 = (ConcurrentDictionary<TKey, TValue>.Node) null;
          for (ConcurrentDictionary<TKey, TValue>.Node node2 = local; node2 != null; node2 = node2._next)
          {
            if (hashcode == node2._hashcode && ConcurrentDictionary<TKey, TValue>.NodeEqualsKey(comparer, node2, key))
            {
              if (!equalityComparer.Equals(node2._value, comparisonValue))
                return false;
              if (!typeof (TValue).IsValueType || ConcurrentDictionaryTypeProps<TValue>.IsWriteAtomic)
              {
                node2._value = newValue;
              }
              else
              {
                ConcurrentDictionary<TKey, TValue>.Node node3 = new ConcurrentDictionary<TKey, TValue>.Node(node2._key, newValue, hashcode, node2._next);
                if (node1 == null)
                  Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref local, node3);
                else
                  node1._next = node3;
              }
              return true;
            }
            node1 = node2;
          }
          return false;
        }
      }
    }
  }

  /// <summary>Removes all keys and values from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  public void Clear()
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      if (this.AreAllBucketsEmpty())
        return;
      ConcurrentDictionary<TKey, TValue>.Tables tables1 = this._tables;
      ConcurrentDictionary<TKey, TValue>.Tables tables2 = new ConcurrentDictionary<TKey, TValue>.Tables(new ConcurrentDictionary<TKey, TValue>.VolatileNode[HashHelpers.GetPrime(31)], tables1._locks, new int[tables1._countPerLock.Length], tables1._comparer);
      this._tables = tables2;
      this._budget = Math.Max(1, tables2._buckets.Length / tables2._locks.Length);
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  /// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an array, starting at the specified array index.</summary>
  /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.ICollection" />. The array must have zero-based indexing.</param>
  /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="array" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  /// <paramref name="index" /> is less than 0.</exception>
  /// <exception cref="T:System.ArgumentException">
  ///         <paramref name="index" /> is equal to or greater than the length of the <paramref name="array" />.
  /// 
  /// -or-
  /// 
  /// The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.</exception>
  void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(
    KeyValuePair<TKey, TValue>[] array,
    int index)
  {
    ArgumentNullException.ThrowIfNull((object) array, nameof (array));
    ArgumentOutOfRangeException.ThrowIfNegative<int>(index, nameof (index));
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      int countNoLocks = this.GetCountNoLocks();
      if (array.Length - countNoLocks < index)
        throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
      this.CopyToPairs(array, index);
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  #nullable enable
  /// <summary>Copies the key and value pairs stored in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> to a new array.</summary>
  /// <returns>A new array containing a snapshot of key and value pairs copied from the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</returns>
  public KeyValuePair<TKey, TValue>[] ToArray()
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      int countNoLocks = this.GetCountNoLocks();
      if (countNoLocks == 0)
        return Array.Empty<KeyValuePair<TKey, TValue>>();
      KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[countNoLocks];
      this.CopyToPairs(array, 0);
      return array;
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  #nullable disable
  private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
  {
    foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in this._tables._buckets)
    {
      for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = node._next)
      {
        array[index] = new KeyValuePair<TKey, TValue>(node._key, node._value);
        ++index;
      }
    }
  }

  private void CopyToEntries(DictionaryEntry[] array, int index)
  {
    foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in this._tables._buckets)
    {
      for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = node._next)
      {
        array[index] = new DictionaryEntry((object) node._key, (object) node._value);
        ++index;
      }
    }
  }

  private void CopyToObjects(object[] array, int index)
  {
    foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in this._tables._buckets)
    {
      for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = node._next)
      {
        array[index] = (object) new KeyValuePair<TKey, TValue>(node._key, node._value);
        ++index;
      }
    }
  }

  #nullable enable
  /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <returns>An enumerator for the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</returns>
  public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
  {
    return (IEnumerator<KeyValuePair<TKey, TValue>>) new ConcurrentDictionary<TKey, TValue>.Enumerator(this);
  }

  #nullable disable
  private bool TryAddInternal(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    TKey key,
    int? nullableHashcode,
    TValue value,
    bool updateIfExists,
    bool acquireLock,
    out TValue resultingValue)
  {
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashcode = nullableHashcode ?? this.GetHashCode(comparer, key);
    bool resizeDesired;
    bool forceRehashIfNonRandomized;
    while (true)
    {
      object[] locks = tables._locks;
      uint lockNo;
      ref ConcurrentDictionary<TKey, TValue>.Node local = ref ConcurrentDictionary<TKey, TValue>.GetBucketAndLock(tables, hashcode, out lockNo);
      resizeDesired = false;
      forceRehashIfNonRandomized = false;
      bool lockTaken = false;
      try
      {
        if (acquireLock)
          Monitor.Enter(locks[(int) lockNo], ref lockTaken);
        if (tables != this._tables)
        {
          tables = this._tables;
          if (comparer != tables._comparer)
          {
            comparer = tables._comparer;
            hashcode = this.GetHashCode(comparer, key);
          }
        }
        else
        {
          uint num = 0;
          ConcurrentDictionary<TKey, TValue>.Node node1 = (ConcurrentDictionary<TKey, TValue>.Node) null;
          for (ConcurrentDictionary<TKey, TValue>.Node node2 = local; node2 != null; node2 = node2._next)
          {
            if (hashcode == node2._hashcode && ConcurrentDictionary<TKey, TValue>.NodeEqualsKey(comparer, node2, key))
            {
              if (updateIfExists)
              {
                if (!typeof (TValue).IsValueType || ConcurrentDictionaryTypeProps<TValue>.IsWriteAtomic)
                {
                  node2._value = value;
                }
                else
                {
                  ConcurrentDictionary<TKey, TValue>.Node node3 = new ConcurrentDictionary<TKey, TValue>.Node(node2._key, value, hashcode, node2._next);
                  if (node1 == null)
                    Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref local, node3);
                  else
                    node1._next = node3;
                }
                resultingValue = value;
              }
              else
                resultingValue = node2._value;
              return false;
            }
            node1 = node2;
            if (!typeof (TKey).IsValueType)
              ++num;
          }
          ConcurrentDictionary<TKey, TValue>.Node node4 = new ConcurrentDictionary<TKey, TValue>.Node(key, value, hashcode, local);
          Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref local, node4);
          checked { ++tables._countPerLock[unchecked ((int) lockNo)]; }
          if (tables._countPerLock[(int) lockNo] > this._budget)
            resizeDesired = true;
          if (!typeof (TKey).IsValueType)
          {
            if (num > 100U)
            {
              if (comparer is NonRandomizedStringEqualityComparer)
              {
                forceRehashIfNonRandomized = true;
                break;
              }
              break;
            }
            break;
          }
          break;
        }
      }
      finally
      {
        if (lockTaken)
          Monitor.Exit(locks[(int) lockNo]);
      }
    }
    if (resizeDesired | forceRehashIfNonRandomized)
      this.GrowTable(tables, resizeDesired, forceRehashIfNonRandomized);
    resultingValue = value;
    return true;
  }

  #nullable enable
  /// <summary>Gets or sets the value associated with the specified key.</summary>
  /// <param name="key">The key of the value to get or set.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is  <see langword="null" />.</exception>
  /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key" /> does not exist in the collection.</exception>
  /// <returns>The value of the key/value pair at the specified index.</returns>
  public TValue this[TKey key]
  {
    get
    {
      TValue obj;
      if (!this.TryGetValue(key, out obj))
        ConcurrentDictionary<TKey, TValue>.ThrowKeyNotFoundException(key);
      return obj;
    }
    set
    {
      if ((object) key == null)
        ThrowHelper.ThrowKeyNullException();
      this.TryAddInternal(this._tables, key, new int?(), value, true, true, out TValue _);
    }
  }

  #nullable disable
  [DoesNotReturn]
  private static void ThrowKeyNotFoundException(TKey key)
  {
    throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, (object) key.ToString()));
  }

  #nullable enable
  /// <summary>Gets the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> that is used to determine equality of keys for the dictionary.</summary>
  /// <returns>The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> generic interface implementation that is used to determine equality of keys for the current <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> and to provide hash values for the keys.</returns>
  public IEqualityComparer<TKey> Comparer
  {
    get
    {
      IEqualityComparer<TKey> comparer = this._tables._comparer;
      if (typeof (TKey) == typeof (string))
      {
        IEqualityComparer<string> equalityComparer1 = comparer is NonRandomizedStringEqualityComparer equalityComparer2 ? equalityComparer2.GetUnderlyingEqualityComparer() : (IEqualityComparer<string>) null;
        if (equalityComparer1 != null)
          return (IEqualityComparer<TKey>) equalityComparer1;
      }
      return comparer ?? (IEqualityComparer<TKey>) EqualityComparer<TKey>.Default;
    }
  }

  /// <summary>Gets the number of key/value pairs contained in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The number of key/value pairs contained in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</returns>
  public int Count
  {
    get
    {
      int locksAcquired = 0;
      try
      {
        this.AcquireAllLocks(ref locksAcquired);
        return this.GetCountNoLocks();
      }
      finally
      {
        this.ReleaseLocks(locksAcquired);
      }
    }
  }

  private int GetCountNoLocks()
  {
    int countNoLocks = 0;
    foreach (int num in this._tables._countPerLock)
      checked { countNoLocks += num; }
    return countNoLocks;
  }

  /// <summary>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function if the key does not already exist. Returns the new value, or the existing value if the key exists.</summary>
  /// <param name="key">The key of the element to add.</param>
  /// <param name="valueFactory">The function used to generate a value for the key.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> or <paramref name="valueFactory" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
  public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    if (valueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (valueFactory));
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    int hashCode = this.GetHashCode(tables._comparer, key);
    TValue resultingValue;
    if (!ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out resultingValue))
      this.TryAddInternal(tables, key, new int?(hashCode), valueFactory(key), false, true, out resultingValue);
    return resultingValue;
  }

  /// <summary>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function and an argument if the key does not already exist, or returns the existing value if the key exists.</summary>
  /// <param name="key">The key of the element to add.</param>
  /// <param name="valueFactory">The function used to generate a value for the key.</param>
  /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory" />.</param>
  /// <typeparam name="TArg">The type of an argument to pass into <paramref name="valueFactory" />.</typeparam>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is a <see langword="null" /> reference (Nothing in Visual Basic).</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
  public TValue GetOrAdd<TArg>(
    TKey key,
    Func<TKey, TArg, TValue> valueFactory,
    TArg factoryArgument)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    if (valueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (valueFactory));
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    int hashCode = this.GetHashCode(tables._comparer, key);
    TValue resultingValue;
    if (!ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out resultingValue))
      this.TryAddInternal(tables, key, new int?(hashCode), valueFactory(key, factoryArgument), false, true, out resultingValue);
    return resultingValue;
  }

  /// <summary>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist. Returns the new value, or the existing value if the key exists.</summary>
  /// <param name="key">The key of the element to add.</param>
  /// <param name="value">The value to be added, if the key does not already exist.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
  public TValue GetOrAdd(TKey key, TValue value)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    int hashCode = this.GetHashCode(tables._comparer, key);
    TValue resultingValue;
    if (!ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out resultingValue))
      this.TryAddInternal(tables, key, new int?(hashCode), value, false, true, out resultingValue);
    return resultingValue;
  }

  /// <summary>Uses the specified functions and argument to add a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist, or to update a key/value pair in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key already exists.</summary>
  /// <param name="key">The key to be added or whose value should be updated.</param>
  /// <param name="addValueFactory">The function used to generate a value for an absent key.</param>
  /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value.</param>
  /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory" /> and <paramref name="updateValueFactory" />.</param>
  /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory" /> and <paramref name="updateValueFactory" />.</typeparam>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" />, <paramref name="addValueFactory" />, or <paramref name="updateValueFactory" /> is a null reference (Nothing in Visual Basic).</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The new value for the key. This will be either be the result of <paramref name="addValueFactory" /> (if the key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).</returns>
  public TValue AddOrUpdate<TArg>(
    TKey key,
    Func<TKey, TArg, TValue> addValueFactory,
    Func<TKey, TValue, TArg, TValue> updateValueFactory,
    TArg factoryArgument)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    if (addValueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (addValueFactory));
    if (updateValueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (updateValueFactory));
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashCode = this.GetHashCode(comparer, key);
    while (true)
    {
      do
      {
        do
        {
          TValue comparisonValue;
          if (ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out comparisonValue))
          {
            TValue newValue = updateValueFactory(key, comparisonValue, factoryArgument);
            if (this.TryUpdateInternal(tables, key, new int?(hashCode), newValue, comparisonValue))
              return newValue;
          }
          else
          {
            TValue resultingValue;
            if (this.TryAddInternal(tables, key, new int?(hashCode), addValueFactory(key, factoryArgument), false, true, out resultingValue))
              return resultingValue;
          }
        }
        while (tables == this._tables);
        tables = this._tables;
      }
      while (comparer == tables._comparer);
      comparer = tables._comparer;
      hashCode = this.GetHashCode(comparer, key);
    }
  }

  /// <summary>Uses the specified functions to add a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist, or to update a key/value pair in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key already exists.</summary>
  /// <param name="key">The key to be added or whose value should be updated.</param>
  /// <param name="addValueFactory">The function used to generate a value for an absent key.</param>
  /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" />, <paramref name="addValueFactory" />, or <paramref name="updateValueFactory" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The new value for the key. This will be either be the result of <paramref name="addValueFactory" /> (if the key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).</returns>
  public TValue AddOrUpdate(
    TKey key,
    Func<TKey, TValue> addValueFactory,
    Func<TKey, TValue, TValue> updateValueFactory)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    if (addValueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (addValueFactory));
    if (updateValueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (updateValueFactory));
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashCode = this.GetHashCode(comparer, key);
    while (true)
    {
      do
      {
        do
        {
          TValue comparisonValue;
          if (ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out comparisonValue))
          {
            TValue newValue = updateValueFactory(key, comparisonValue);
            if (this.TryUpdateInternal(tables, key, new int?(hashCode), newValue, comparisonValue))
              return newValue;
          }
          else
          {
            TValue resultingValue;
            if (this.TryAddInternal(tables, key, new int?(hashCode), addValueFactory(key), false, true, out resultingValue))
              return resultingValue;
          }
        }
        while (tables == this._tables);
        tables = this._tables;
      }
      while (comparer == tables._comparer);
      comparer = tables._comparer;
      hashCode = this.GetHashCode(comparer, key);
    }
  }

  /// <summary>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> if the key does not already exist, or updates a key/value pair in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function if the key already exists.</summary>
  /// <param name="key">The key to be added or whose value should be updated.</param>
  /// <param name="addValue">The value to be added for an absent key.</param>
  /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> or <paramref name="updateValueFactory" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  /// <returns>The new value for the key. This will be either be <paramref name="addValue" /> (if the key was absent) or the result of <paramref name="updateValueFactory" /> (if the key was present).</returns>
  public TValue AddOrUpdate(
    TKey key,
    TValue addValue,
    Func<TKey, TValue, TValue> updateValueFactory)
  {
    if ((object) key == null)
      ThrowHelper.ThrowKeyNullException();
    if (updateValueFactory == null)
      ThrowHelper.ThrowArgumentNullException(nameof (updateValueFactory));
    ConcurrentDictionary<TKey, TValue>.Tables tables = this._tables;
    IEqualityComparer<TKey> comparer = tables._comparer;
    int hashCode = this.GetHashCode(comparer, key);
    while (true)
    {
      do
      {
        do
        {
          TValue comparisonValue;
          if (ConcurrentDictionary<TKey, TValue>.TryGetValueInternal(tables, key, hashCode, out comparisonValue))
          {
            TValue newValue = updateValueFactory(key, comparisonValue);
            if (this.TryUpdateInternal(tables, key, new int?(hashCode), newValue, comparisonValue))
              return newValue;
          }
          else
          {
            TValue resultingValue;
            if (this.TryAddInternal(tables, key, new int?(hashCode), addValue, false, true, out resultingValue))
              return resultingValue;
          }
        }
        while (tables == this._tables);
        tables = this._tables;
      }
      while (comparer == tables._comparer);
      comparer = tables._comparer;
      hashCode = this.GetHashCode(comparer, key);
    }
  }

  /// <summary>Gets a value that indicates whether the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> is empty.</summary>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> is empty; otherwise, <see langword="false" />.</returns>
  public bool IsEmpty
  {
    get
    {
      if (!this.AreAllBucketsEmpty())
        return false;
      int locksAcquired = 0;
      try
      {
        this.AcquireAllLocks(ref locksAcquired);
        return this.AreAllBucketsEmpty();
      }
      finally
      {
        this.ReleaseLocks(locksAcquired);
      }
    }
  }

  #nullable disable
  /// <summary>Adds the specified key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.</summary>
  /// <param name="key">The object to use as the key of the element to add.</param>
  /// <param name="value">The object to use as the value of the element to add.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
  {
    if (!this.TryAdd(key, value))
      throw new ArgumentException(SR.ConcurrentDictionary_KeyAlreadyExisted);
  }

  /// <summary>Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.</summary>
  /// <param name="key">The key of the element to remove.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the element is successfully removed; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
  bool IDictionary<TKey, TValue>.Remove(TKey key) => this.TryRemove(key, out TValue _);

  #nullable enable
  /// <summary>Gets a collection containing the keys in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
  /// <returns>A collection of keys in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
  public ICollection<TKey> Keys => (ICollection<TKey>) this.GetKeys();

  /// <summary>Gets a collection containing the keys in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
  /// <returns>A collection containing the keys in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
  IEnumerable<TKey> IReadOnlyDictionary<
  #nullable disable
  TKey, TValue>.Keys => (IEnumerable<TKey>) this.GetKeys();

  #nullable enable
  /// <summary>Gets a collection that contains the values in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
  /// <returns>A collection that contains the values in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
  public ICollection<TValue> Values => (ICollection<TValue>) this.GetValues();

  /// <summary>Gets a collection that contains the values in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
  /// <returns>A collection that contains the values in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
  IEnumerable<TValue> IReadOnlyDictionary<
  #nullable disable
  TKey, TValue>.Values => (IEnumerable<TValue>) this.GetValues();

  /// <summary>Adds an item to the collection.</summary>
  /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair`2" /> to add to the dictionary.</param>
  /// <exception cref="T:System.ArgumentNullException">The <see cref="P:System.Collections.Generic.KeyValuePair`2.Key" /> of <paramref name="keyValuePair" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.OverflowException">The <see cref="T:System.Collections.Generic.Dictionary`2" /> contains too many elements.</exception>
  /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</exception>
  void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
  {
    ((IDictionary<TKey, TValue>) this).Add(keyValuePair.Key, keyValuePair.Value);
  }

  /// <summary>Gets whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains an element with the specified key.</summary>
  /// <param name="keyValuePair">The key to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.ICollection`1" /> contains an element with the specified key; otherwise, <see langword="false" />.</returns>
  bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
  {
    TValue x;
    return this.TryGetValue(keyValuePair.Key, out x) && EqualityComparer<TValue>.Default.Equals(x, keyValuePair.Value);
  }

  /// <summary>Gets a value that indicates whether the <see cref="T:System.Collections.ICollection" /> is read-only.</summary>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.ICollection" /> is read-only; otherwise, <see langword="false" />.</returns>
  bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

  /// <summary>Removes the specified key/value pair from the collection.</summary>
  /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair`2" /> to remove.</param>
  /// <exception cref="T:System.ArgumentNullException">The <see cref="P:System.Collections.Generic.KeyValuePair`2.Key" /> property of <paramref name="keyValuePair" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the element is successfully removed; otherwise, <see langword="false" />. This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
  bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
  {
    return this.TryRemove(keyValuePair);
  }

  /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</summary>
  /// <returns>An enumerator for the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</returns>
  IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

  /// <summary>Adds the specified key and value to the dictionary.</summary>
  /// <param name="key">The object to use as the key.</param>
  /// <param name="value">The object to use as the value.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentException">
  ///        <paramref name="key" /> is of a type that is not assignable to the key type  of the <see cref="T:System.Collections.Generic.Dictionary`2" />.
  /// 
  /// -or-
  /// 
  /// <paramref name="value" /> is of a type that is not assignable to the type of values in the <see cref="T:System.Collections.Generic.Dictionary`2" />.
  /// 
  /// -or-
  /// 
  /// A value with the same key already exists in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</exception>
  /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
  void IDictionary.Add(object key, object value)
  {
    if (key == null)
      ThrowHelper.ThrowKeyNullException();
    if (!(key is TKey key1))
      throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
    ConcurrentDictionary<TKey, TValue>.ThrowIfInvalidObjectValue(value);
    ((IDictionary<TKey, TValue>) this).Add(key1, (TValue) value);
  }

  /// <summary>Gets a value that indicates the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.</summary>
  /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, <see langword="false" />.</returns>
  bool IDictionary.Contains(object key)
  {
    if (key == null)
      ThrowHelper.ThrowKeyNullException();
    return key is TKey key1 && this.ContainsKey(key1);
  }

  /// <summary>Provides a <see cref="T:System.Collections.IDictionaryEnumerator" /> for the <see cref="T:System.Collections.Generic.IDictionary`2" />.</summary>
  /// <returns>A <see cref="T:System.Collections.IDictionaryEnumerator" /> for the <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
  IDictionaryEnumerator IDictionary.GetEnumerator()
  {
    return (IDictionaryEnumerator) new ConcurrentDictionary<TKey, TValue>.DictionaryEnumerator(this);
  }

  /// <summary>Gets a value that indicates whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> has a fixed size.</summary>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.IDictionary`2" /> has a fixed size; otherwise, <see langword="false" />. For <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />, this property always returns <see langword="false" />.</returns>
  bool IDictionary.IsFixedSize => false;

  /// <summary>Gets a value that indicates whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> is read-only.</summary>
  /// <returns>
  /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.IDictionary`2" /> is read-only; otherwise, <see langword="false" />. For <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />, this property always returns <see langword="false" />.</returns>
  bool IDictionary.IsReadOnly => false;

  #nullable enable
  /// <summary>Gets an <see cref="T:System.Collections.ICollection" /> that contains the keys of the  <see cref="T:System.Collections.Generic.IDictionary`2" />.</summary>
  /// <returns>An interface that contains the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
  ICollection IDictionary.Keys => (ICollection) this.GetKeys();

  #nullable disable
  /// <summary>Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary" />.</summary>
  /// <param name="key">The key of the element to remove.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is <see langword="null" />.</exception>
  void IDictionary.Remove(object key)
  {
    if (key == null)
      ThrowHelper.ThrowKeyNullException();
    if (!(key is TKey key1))
      return;
    this.TryRemove(key1, out TValue _);
  }

  #nullable enable
  /// <summary>Gets an <see cref="T:System.Collections.ICollection" /> that contains the values in the <see cref="T:System.Collections.IDictionary" />.</summary>
  /// <returns>An interface that contains the values in the <see cref="T:System.Collections.IDictionary" />.</returns>
  ICollection IDictionary.Values => (ICollection) this.GetValues();

  /// <summary>Gets or sets the value associated with the specified key.</summary>
  /// <param name="key">The key of the value to get or set.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="key" /> is  <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentException">A value is being assigned, and <paramref name="key" /> is of a type that is not assignable to the key type or the value type of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</exception>
  /// <returns>The value associated with the specified key, or  <see langword="null" /> if <paramref name="key" /> is not in the dictionary or <paramref name="key" /> is of a type that is not assignable to the key type of the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" />.</returns>
  object? IDictionary.this[
  #nullable disable
  object key]
  {
    get
    {
      if (key == null)
        ThrowHelper.ThrowKeyNullException();
      TValue obj;
      return key is TKey key1 && this.TryGetValue(key1, out obj) ? (object) obj : (object) null;
    }
    set
    {
      if (key == null)
        ThrowHelper.ThrowKeyNullException();
      if (!(key is TKey key1))
        throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
      ConcurrentDictionary<TKey, TValue>.ThrowIfInvalidObjectValue(value);
      this[key1] = (TValue) value;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static void ThrowIfInvalidObjectValue(object value)
  {
    if (value != null)
    {
      if (value is TValue)
        return;
      ThrowHelper.ThrowValueNullException();
    }
    else
    {
      if ((object) default (TValue) == null)
        return;
      ThrowHelper.ThrowValueNullException();
    }
  }

  /// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an array, starting at the specified array index.</summary>
  /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.ICollection" />. The array must have zero-based indexing.</param>
  /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
  /// <exception cref="T:System.ArgumentNullException">
  /// <paramref name="array" /> is <see langword="null" />.</exception>
  /// <exception cref="T:System.ArgumentOutOfRangeException">
  /// <paramref name="index" /> is less than 0.</exception>
  /// <exception cref="T:System.ArgumentException">
  ///        <paramref name="index" /> is equal to or greater than the length of the <paramref name="array" />.
  /// 
  /// -or-
  /// 
  /// The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.</exception>
  void ICollection.CopyTo(Array array, int index)
  {
    ArgumentNullException.ThrowIfNull((object) array, nameof (array));
    ArgumentOutOfRangeException.ThrowIfNegative<int>(index, nameof (index));
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      int countNoLocks = this.GetCountNoLocks();
      if (array.Length - countNoLocks < index)
        throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
      switch (array)
      {
        case KeyValuePair<TKey, TValue>[] array1:
          this.CopyToPairs(array1, index);
          break;
        case DictionaryEntry[] array2:
          this.CopyToEntries(array2, index);
          break;
        case object[] array3:
          this.CopyToObjects(array3, index);
          break;
        default:
          throw new ArgumentException(SR.ConcurrentDictionary_ArrayIncorrectType, nameof (array));
      }
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  /// <summary>Gets a value that indicates whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized with the SyncRoot.</summary>
  /// <returns>
  /// <see langword="true" /> if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, <see langword="false" />. For <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> this property always returns <see langword="false" />.</returns>
  bool ICollection.IsSynchronized => false;

  #nullable enable
  /// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />. This property is not supported.</summary>
  /// <exception cref="T:System.NotSupportedException">This property is not supported.</exception>
  /// <returns>Always returns null.</returns>
  object ICollection.SyncRoot
  {
    get => throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
  }

  private bool AreAllBucketsEmpty()
  {
    return !this._tables._countPerLock.AsSpan<int>().ContainsAnyExcept<int>(0);
  }

  #nullable disable
  private void GrowTable(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    bool resizeDesired,
    bool forceRehashIfNonRandomized)
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireFirstLock(ref locksAcquired);
      if (tables != this._tables)
        return;
      int length1 = tables._buckets.Length;
      IEqualityComparer<TKey> equalityComparer = (IEqualityComparer<TKey>) null;
      if (forceRehashIfNonRandomized && tables._comparer is NonRandomizedStringEqualityComparer comparer)
        equalityComparer = (IEqualityComparer<TKey>) comparer.GetUnderlyingEqualityComparer();
      if (resizeDesired)
      {
        if (equalityComparer == null && this.GetCountNoLocks() < tables._buckets.Length / 4)
        {
          this._budget = 2 * this._budget;
          if (this._budget >= 0)
            return;
          this._budget = int.MaxValue;
          return;
        }
        int min;
        if ((min = tables._buckets.Length * 2) < 0 || (length1 = HashHelpers.GetPrime(min)) > Array.MaxLength)
        {
          length1 = Array.MaxLength;
          this._budget = int.MaxValue;
        }
      }
      object[] objArray = tables._locks;
      if (this._growLockArray && tables._locks.Length < 1024)
      {
        objArray = new object[tables._locks.Length * 2];
        Array.Copy((Array) tables._locks, (Array) objArray, tables._locks.Length);
        for (int length2 = tables._locks.Length; length2 < objArray.Length; ++length2)
          objArray[length2] = new object();
      }
      ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets = new ConcurrentDictionary<TKey, TValue>.VolatileNode[length1];
      int[] countPerLock = new int[objArray.Length];
      ConcurrentDictionary<TKey, TValue>.Tables tables1 = new ConcurrentDictionary<TKey, TValue>.Tables(buckets, objArray, countPerLock, equalityComparer ?? tables._comparer);
      ConcurrentDictionary<TKey, TValue>.AcquirePostFirstLock(tables, ref locksAcquired);
      ConcurrentDictionary<TKey, TValue>.Node next;
      foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in tables._buckets)
      {
        for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = next)
        {
          int hashcode = equalityComparer == null ? node._hashcode : equalityComparer.GetHashCode(node._key);
          next = node._next;
          uint lockNo;
          ref ConcurrentDictionary<TKey, TValue>.Node local = ref ConcurrentDictionary<TKey, TValue>.GetBucketAndLock(tables1, hashcode, out lockNo);
          local = new ConcurrentDictionary<TKey, TValue>.Node(node._key, node._value, hashcode, local);
          checked { ++countPerLock[unchecked ((int) lockNo)]; }
        }
      }
      this._budget = Math.Max(1, buckets.Length / objArray.Length);
      this._tables = tables1;
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  private static int DefaultConcurrencyLevel => Environment.ProcessorCount;

  private void AcquireAllLocks(ref int locksAcquired)
  {
    if (CDSCollectionETWBCLProvider.Log.IsEnabled())
      CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(this._tables._buckets.Length);
    this.AcquireFirstLock(ref locksAcquired);
    ConcurrentDictionary<TKey, TValue>.AcquirePostFirstLock(this._tables, ref locksAcquired);
  }

  private void AcquireFirstLock(ref int locksAcquired)
  {
    Monitor.Enter(this._tables._locks[0]);
    locksAcquired = 1;
  }

  private static void AcquirePostFirstLock(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    ref int locksAcquired)
  {
    object[] locks = tables._locks;
    for (int index = 1; index < locks.Length; ++index)
    {
      Monitor.Enter(locks[index]);
      ++locksAcquired;
    }
  }

  private void ReleaseLocks(int locksAcquired)
  {
    object[] locks = this._tables._locks;
    for (int index = 0; index < locksAcquired; ++index)
      Monitor.Exit(locks[index]);
  }

  private ReadOnlyCollection<TKey> GetKeys()
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      int countNoLocks = this.GetCountNoLocks();
      if (countNoLocks == 0)
        return ReadOnlyCollection<TKey>.Empty;
      TKey[] list = new TKey[countNoLocks];
      int index = 0;
      foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in this._tables._buckets)
      {
        for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = node._next)
        {
          list[index] = node._key;
          ++index;
        }
      }
      return new ReadOnlyCollection<TKey>((IList<TKey>) list);
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  private ReadOnlyCollection<TValue> GetValues()
  {
    int locksAcquired = 0;
    try
    {
      this.AcquireAllLocks(ref locksAcquired);
      int countNoLocks = this.GetCountNoLocks();
      if (countNoLocks == 0)
        return ReadOnlyCollection<TValue>.Empty;
      TValue[] list = new TValue[countNoLocks];
      int index = 0;
      foreach (ConcurrentDictionary<TKey, TValue>.VolatileNode bucket in this._tables._buckets)
      {
        for (ConcurrentDictionary<TKey, TValue>.Node node = bucket._node; node != null; node = node._next)
        {
          list[index] = node._value;
          ++index;
        }
      }
      return new ReadOnlyCollection<TValue>((IList<TValue>) list);
    }
    finally
    {
      this.ReleaseLocks(locksAcquired);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static ConcurrentDictionary<TKey, TValue>.Node GetBucket(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    int hashcode)
  {
    ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets = tables._buckets;
    return IntPtr.Size == 8 ? buckets[(int) HashHelpers.FastMod((uint) hashcode, (uint) buckets.Length, tables._fastModBucketsMultiplier)]._node : buckets[(int) ((uint) hashcode % (uint) buckets.Length)]._node;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static ref ConcurrentDictionary<TKey, TValue>.Node GetBucketAndLock(
    ConcurrentDictionary<TKey, TValue>.Tables tables,
    int hashcode,
    out uint lockNo)
  {
    ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets = tables._buckets;
    uint index = IntPtr.Size != 8 ? (uint) hashcode % (uint) buckets.Length : HashHelpers.FastMod((uint) hashcode, (uint) buckets.Length, tables._fastModBucketsMultiplier);
    lockNo = index % (uint) tables._locks.Length;
    return ref buckets[(int) index]._node;
  }

  private sealed class Enumerator : 
    IEnumerator<KeyValuePair<TKey, TValue>>,
    IEnumerator,
    IDisposable
  {
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;
    private ConcurrentDictionary<TKey, TValue>.VolatileNode[] _buckets;
    private ConcurrentDictionary<TKey, TValue>.Node _node;
    private int _i;
    private int _state;

    public Enumerator(ConcurrentDictionary<TKey, TValue> dictionary)
    {
      this._dictionary = dictionary;
      this._i = -1;
    }

    public KeyValuePair<TKey, TValue> Current { get; private set; }

    object IEnumerator.Current => (object) this.Current;

    public void Reset()
    {
      this._buckets = (ConcurrentDictionary<TKey, TValue>.VolatileNode[]) null;
      this._node = (ConcurrentDictionary<TKey, TValue>.Node) null;
      this.Current = new KeyValuePair<TKey, TValue>();
      this._i = -1;
      this._state = 0;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
      switch (this._state)
      {
        case 0:
          this._buckets = this._dictionary._tables._buckets;
          this._i = -1;
          goto case 1;
        case 1:
          ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets = this._buckets;
          int index = ++this._i;
          if ((uint) index < (uint) buckets.Length)
          {
            this._node = buckets[index]._node;
            this._state = 2;
            goto case 2;
          }
          else
            break;
        case 2:
          ConcurrentDictionary<TKey, TValue>.Node node = this._node;
          if (node != null)
          {
            this.Current = new KeyValuePair<TKey, TValue>(node._key, node._value);
            this._node = node._next;
            return true;
          }
          goto case 1;
      }
      this._state = 3;
      return false;
    }
  }

  private struct VolatileNode
  {
    internal volatile ConcurrentDictionary<TKey, TValue>.Node _node;
  }

  private sealed class Node
  {
    internal readonly TKey _key;
    internal TValue _value;
    internal volatile ConcurrentDictionary<TKey, TValue>.Node _next;
    internal readonly int _hashcode;

    internal Node(
      TKey key,
      TValue value,
      int hashcode,
      ConcurrentDictionary<TKey, TValue>.Node next)
    {
      this._key = key;
      this._value = value;
      this._next = next;
      this._hashcode = hashcode;
    }
  }

  private sealed class Tables
  {
    internal readonly IEqualityComparer<TKey> _comparer;
    internal readonly ConcurrentDictionary<TKey, TValue>.VolatileNode[] _buckets;
    internal readonly ulong _fastModBucketsMultiplier;
    internal readonly object[] _locks;
    internal readonly int[] _countPerLock;

    internal Tables(
      ConcurrentDictionary<TKey, TValue>.VolatileNode[] buckets,
      object[] locks,
      int[] countPerLock,
      IEqualityComparer<TKey> comparer)
    {
      this._buckets = buckets;
      this._locks = locks;
      this._countPerLock = countPerLock;
      this._comparer = comparer;
      if (IntPtr.Size != 8)
        return;
      this._fastModBucketsMultiplier = HashHelpers.GetFastModMultiplier((uint) buckets.Length);
    }
  }

  private sealed class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
  {
    private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

    internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
    {
      this._enumerator = dictionary.GetEnumerator();
    }

    public DictionaryEntry Entry
    {
      // Changed prop 
      get
      {
        KeyValuePair<TKey, TValue> current = this._enumerator.Current;
        // Standard C# implementation - avoids decompiler-specific __Boxed types
        return new DictionaryEntry(current.Key, current.Value);
      }
    }

    public object Key => (object) this._enumerator.Current.Key;

    public object Value => (object) this._enumerator.Current.Value;

    public object Current => (object) this.Entry;

    public bool MoveNext() => this._enumerator.MoveNext();

    public void Reset() => this._enumerator.Reset();
  }
}

// ================================================
// ========== STUBS TO SATISFY ANALYZER ===========
// ================================================

internal static class HashHelpers
{
    public const int HashCollisionThreshold = 100;
    public static int GetPrime(int capacity) => capacity; 
    public static ulong GetFastModMultiplier(uint divisor) => 0;
    public static uint FastMod(uint value, uint divisor, ulong multiplier) => value % divisor;
}

internal class NonRandomizedStringEqualityComparer
{
    public static IEqualityComparer<string>? GetStringComparer(object comparer) => null;
    public IEqualityComparer<string> GetUnderlyingEqualityComparer() => EqualityComparer<string>.Default;
}

internal static class ThrowHelper
{
    public static void ThrowKeyNullException() => throw new ArgumentNullException("key");
    public static void ThrowValueNullException() => throw new ArgumentNullException("value");
    public static void ThrowArgumentNullException(string name, string res) => throw new ArgumentNullException(name);
    public static void ThrowArgumentNullException(string name) => throw new ArgumentNullException(name);
}

internal static class SR
{
    public const string ConcurrentDictionary_ConcurrencyLevelMustBePositiveOrNegativeOne = "Concurrency level must be positive or -1.";
    public const string ConcurrentDictionary_SourceContainsDuplicateKeys = "Source contains duplicate keys.";
    public const string ConcurrentDictionary_ItemKeyIsNull = "Item key is null.";
    public const string ConcurrentDictionary_ArrayNotLargeEnough = "Array not large enough.";
    public const string ConcurrentDictionary_ArrayIncorrectType = "Array type is incorrect.";
    public const string ConcurrentDictionary_KeyAlreadyExisted = "Key already exists.";
    public const string ConcurrentDictionary_TypeOfKeyIncorrect = "Type of key is incorrect.";
    public const string ConcurrentCollection_SyncRoot_NotSupported = "SyncRoot is not supported.";
    public const string Arg_KeyNotFoundWithKey = "Key not found: {0}";
    public static string Format(string format, object arg) => string.Format(format, arg);
}

internal class CDSCollectionETWBCLProvider
{
    public static readonly CDSCollectionETWBCLProvider Log = new CDSCollectionETWBCLProvider();
    public bool IsEnabled() => false;
    public void ConcurrentDictionary_AcquiringAllLocks(int bucketCount) { }
}

internal static class ConcurrentDictionaryTypeProps<T>
{
    // Section 12.6.6 of ECMA-335: Reference types and primitives <= pointer size are atomic
    internal static readonly bool IsWriteAtomic = !typeof(T).IsValueType || MarshalSizeOf(typeof(T)) <= IntPtr.Size;

    private static int MarshalSizeOf(Type t)
    {
        if (t == typeof(int) || t == typeof(uint) || t == typeof(float)) return 4;
        if (t == typeof(long) || t == typeof(ulong) || t == typeof(double)) return 8;
        return 99; // Default to non-atomic for unknown structs
    }
}

// Decompiled helper for DictionaryEntry boxing
// internal class __Boxed<T>
// {
//   // These methods allow the syntax ((object) key) to work if the decompiler
//   // emitted specific calls, but for your baseline, we just need the symbol to exist.
//   public T Value;
//   public __Boxed(T value) => Value = value;
// }

internal struct __Boxed<T>
{
  public static __Boxed<T> FromObject(object obj) => default;
  public static object ToObject(__Boxed<T> boxed) => new object();
}

internal sealed class IDictionaryDebugView<TKey, TValue>
{
    public IDictionaryDebugView(object dictionary) { }
}

