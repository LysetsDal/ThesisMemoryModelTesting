# ThreadSafetyAnalyser — AI Conversation Context

This document captures the design conversation about the Roslyn-based `ThreadSafetyAnalyser` and serves as a running context log for AI-assisted development.

---

## Project Overview

The analyser targets classes annotated with `[ThreadSafe]` and uses the `CorrectlySynchronizedAnalyzer` (`DiagnosticAnalyzer`) to detect a range of concurrency hazards at compile time.

---

## Existing Rules in `CorrectlySynchronizedAnalyzer`

### 1. `ConflictingAccessThreadRule` (`ConflictingAccessThread`)
**Trigger:** `AnalyzeConflictingAccessesInThreads` / `AnalyzeConflictingAccessesInTasks`

Scans a class for two or more `new Thread(...)` or `new Task(...)` instantiations in the same method scope. For each pair, it builds an `AccessMap` (via `AnalyzerUtils.GetAccessedFieldsFromExpression`) to find shared mutable state accessed by both. If neither thread holds the same lock object over the conflicting field, a warning is emitted on each thread creation site.

### 2. `VolatileReorderingRule` (`VolatileReordering`)
**Trigger:** `AnalyzeVolatileReordering`

Within every method/lambda body in a `[ThreadSafe]` class, it walks all `IdentifierNameSyntax` nodes in program order. If a volatile **write** is followed by a volatile **read** of a different field (later in textual order, same scope) without an intervening full memory fence (`Thread.MemoryBarrier`, `Interlocked`, etc.), a warning is raised on the read site.

### 3. `LockOnClassInstanceRule` (`LockOnClassInstance`)
**Trigger:** `AnalyzeLockThis`

Uses `LockAssociationUtils.GetClassLocks` to enumerate every `lock(...)` statement in the class. If the lock target resolves to `this` (either syntactically via `ThisExpressionSyntax` or semantically via symbol equality), a warning is raised at the lock expression site, naming the enclosing method.

### 4. `ConflictingAccessRule` (`ConflictingAccessRule`)
**Trigger:** `AnalyzeConflictingAccessesAcrossMembers`

Pairwise comparison of all non-static instance methods. Builds an `AccessMap` per method (via `AnalyzerUtils.GetAccessedFieldsFromMethod`). For each pair sharing mutable state that is **not** guarded by the same lock object, a warning is raised on each method's identifier. Uses a `HashSet<(MethodDeclarationSyntax, ISymbol)>` to deduplicate repeated reports.

---

## Key Utilities

| Utility | Purpose |
|---|---|
| `LockAssociationUtils.GetClassLocks` | Scans all `LockStatementSyntax` nodes in a class; maps lock target symbols ? `LockAssociation` list (each holding the containing member symbol and the `LockStatementSyntax`). Only considers `lock(...)` keyword. |
| `LockAssociationUtils.GetEnclosingLockSymbol` | Walks ancestors of a node to find the nearest enclosing `LockStatementSyntax` and returns its lock-object symbol. |
| `LockAssociationUtils.GetFirstAncestorLockFromSymbol` | Same as above, returns first ancestor lock symbol. |
| `AnalyzerUtils.GetLockObjectsUsedInMember` | Given a `ClassLocks` map and a member symbol, returns the set of lock-object symbols that are used inside that member. |
| `AnalyzerUtils.GetAccessedFieldsFromMethod` | Returns `Dictionary<ISymbol, AccessInfo>` of all fields/properties accessed by a method, including their access type (read/write) and associated lock symbol. |
| `AnalyzerUtils.GetAccessedFieldsFromExpression` | Same as above but starting from a thread/task creation expression. |
| `AnalyzerUtils.FindConflicts` | Returns the set of field symbols that appear in both `AccessMap` dictionaries. |
| `AnalyzerUtils.IsUsingSameLockObject` | Checks if two `AccessInfo` instances reference the same lock object. |
| `AnalyzerUtils.IsInternallySynchronized` | Recursively checks if a method uses `lock`, `Interlocked`, or `Volatile` internally. |

---

## Planned Rule: `MonitorUsageRule` — `Monitor.Enter` / `Monitor.Exit` Detection

### Problem Statement
`Monitor.Enter` and `Monitor.Exit` delineate a critical section but are **not** syntactically co-located like `lock(...)` is. Because the `lock` keyword desugars to `Monitor.Enter`/`Exit` with a `try/finally`, the existing `GetClassLocks` only captures `lock(...)` syntax. A developer using `Monitor.Enter` explicitly bypasses this tracking entirely.

### Challenges
1. **Unpaired calls**: `Enter` and `Exit` may be in different branches, methods, or even not guaranteed to be paired (missing `finally` block).
2. **No syntactic scope boundary**: unlike `LockStatementSyntax`, there is no AST node that wraps the protected region.
3. **Must detect the guarded field accesses between Enter and Exit** — requires control-flow awareness or heuristic program-order analysis.

### Proposed Implementation Approach

#### Detection Strategy (Heuristic / Syntactic)
1. Scan each method body for `InvocationExpressionSyntax` where:
   - The method name is `Enter` or `TryEnter`, and
   - The containing type resolves to `System.Threading.Monitor` (check `KnownTypes` or add `FullMonitorName = "System.Threading.Monitor"`).
2. For each `Monitor.Enter(lockObj)` call, record the **lock object argument** and its **position** in the statement list.
3. Scan forward in program order (within the same block) for a matching `Monitor.Exit(lockObj)`.
4. Collect all field/property accesses in statements between `Enter` and `Exit`.
5. Use the existing `AccessMap` / `AccessInfo` model to represent these accesses under a synthetic lock object (the monitor object symbol).
6. Feed this into the existing conflict detection (`FindConflicts`, `IsUsingSameLockObject`).

#### What To Add
- **`KnownTypes.FullMonitorName`** = `"System.Threading.Monitor"` (add to `KnownTypes.cs`)
- **`LockAssociationUtils.GetMonitorEnterExitPairs()`** — new utility method returning paired enter/exit regions with their lock-object symbol and the syntax span in between.
- **`AnalyzeMonitorUsage()`** — new analyzer action in `CorrectlySynchronizedAnalyzer`.
- **`MonitorUsageRule`** — new `DiagnosticDescriptor` in `CorrectlySynchronizedRules`.

#### Rule Variants to Consider
| Scenario | Flag? |
|---|---|
| `Monitor.Enter` with no corresponding `Exit` in the method | Yes — resource leak / liveness bug |
| `Monitor.Enter` / `Exit` not wrapped in `try/finally` | Yes — exception-safety warning |
| Conflicting field access inside monitor region vs. another method without a lock | Yes — same as `ConflictingAccessRule` |

---

## Conversation Log

### Session 1
**User asked:** Explain the existing rules, discuss how to implement a `Monitor.Enter`/`Monitor.Exit` rule, and keep this as a running context document.

**Summary of findings:**
- All existing rules use the `lock(...)` keyword as their synchronization primitive through `LockStatementSyntax`.
- `LockAssociationUtils.GetClassLocks` is the single source of truth for lock tracking — it must be extended or complemented with a parallel Monitor-aware path.
- The closest existing pattern to reuse is `AnalyzeConflictingAccessesAcrossMembers`: build an `AccessMap` for the guarded region and compare against other methods' maps.
- Main design challenge: inferring the "critical section body" from a pair of invocation expressions rather than a syntactic block node.