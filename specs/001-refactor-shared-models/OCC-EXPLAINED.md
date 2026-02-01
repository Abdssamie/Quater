# How Optimistic Concurrency Control Actually Works

**Date**: 2025-02-01  
**Purpose**: Clarify what OCC does and doesn't do

---

## The Critical Point: OCC Prevents Overwrites

### ❌ Common Misconception

"When two users edit the same record, one user's changes overwrite the other's."

### ✅ Reality with OCC

**No one overwrites anything.** The second user's save is **blocked entirely**.

---

## Step-by-Step Example

### Timeline

```
Time    User A                          User B                      Database
----    ------                          ------                      --------
T1      Read Sample (pH=7.0, v1)                                   pH=7.0, v1
T2                                      Read Sample (pH=7.0, v1)   pH=7.0, v1
T3      Change pH to 7.5                                           pH=7.0, v1
T4      Click Save                                                 pH=7.0, v1
T5      ✅ Save succeeds                                           pH=7.5, v2
T6                                      Change pH to 6.8           pH=7.5, v2
T7                                      Click Save                 pH=7.5, v2
T8                                      ❌ BLOCKED!                pH=7.5, v2
T9                                      Error: "Record modified"   pH=7.5, v2
T10                                     Must reload                pH=7.5, v2
T11                                     Sees pH=7.5 (User A's)     pH=7.5, v2
T12                                     Re-enters pH=6.8           pH=7.5, v2
T13                                     Click Save again           pH=7.5, v2
T14                                     ✅ Save succeeds           pH=6.8, v3
```

### Key Points

1. **User B's first save attempt (T8) is BLOCKED**
   - Their change (pH=6.8) is **never written to database**
   - Database still has User A's value (pH=7.5)
   - User B gets an error message

2. **User B must reload (T10-T11)**
   - They see the current database value (pH=7.5)
   - They see what User A changed
   - They can decide: keep 7.5 or change to 6.8

3. **User B re-enters their change (T12-T14)**
   - Now they're working with the latest version (v2)
   - Their save succeeds because version matches
   - Database updates to pH=6.8, v3

---

## What ConflictBackup Would Store

If we had ConflictBackup table, at T8 it would store:

```json
{
  "ServerVersion": {"pH": 7.5, "version": "v2"},  // What's in database
  "ClientVersion": {"pH": 6.8, "version": "v1"},  // What User B tried to save
  "ConflictDetectedAt": "2025-02-01T14:30:00Z"
}
```

### The Problem

**This data is useless** because:
- User B's change (pH=6.8) was **never saved** - it's just what they typed
- User B can see their own typed value in the UI (it's still in the form)
- User B can see the current DB value by reloading (pH=7.5)
- We're storing a snapshot of "what could have been saved but wasn't"

---

## What Users Actually Need

### Scenario: User B gets conflict error

**Option 1: Simple Message (Recommended)**
```
❌ Error: This record was modified by someone else while you were editing.
Your changes were NOT saved.

Please reload to see the current data, then re-enter your changes if still needed.

[Reload Button]
```

**User Experience**:
1. User clicks Reload
2. Form refreshes with current data (pH=7.5)
3. User sees what changed
4. User decides: keep 7.5 or change to 6.8
5. User re-enters if needed and saves again

**Option 2: Show Diff (If Users Complain)**
```
❌ Conflict Detected

Someone else modified this record while you were editing.

Your attempted changes:
  pH: 7.0 → 6.8

Current database values:
  pH: 7.0 → 7.5 (changed by John Doe at 2:30 PM)

Your changes were NOT saved. What would you like to do?

[Keep Current (7.5)]  [Use My Value (6.8)]  [Cancel]
```

**Implementation** (no ConflictBackup table needed):
```csharp
catch (DbUpdateConcurrencyExcept
{
    var entry = ex.Entries.Single();
    var currentValues = entry.CurrentValues;  // What user tried to save
    var databaseValues = await entry.GetDatabaseValuesAsync();  // Current DB
    var originalValues = entry.OriginalValues;  // What user started with
    
    return new ConflictInfo
    {
        YourChanges = CompareValues(originalValues, currentValues),
        TheirChanges = CompareValues(originalValues, databaseValues),
        CurrentData = databaseValues.ToObject()
    };
}
```

**No ConflictBackup table needed!** EF Core already has all this data in memory.

---

## Whykup is Over-Engineering

### What ConflictBackup Does

1. Stores JSON snapshot of server version
2. Stores JSON snapshot of client version (that was never saved)
3. Stores metadata (when, who, resolution strategy)
4. Creates circular dependency (ConflictBackup → Lab → User → AuditLog → ConflictBackup)

### What We Actually Need

1. **Detect conflict**: ✅ OCC does this automatically
2. **Block overwrite**: ✅ OCC does this automatically
3. **Show user what changed**: ✅ EF Core has this data in memory (no table needed)
4. **Let user decide**: ✅ Simple UI with reload button

### The Math

- **Conflicts happen**: ~0.1% of saves (rare)
- **Users want diff view**: ~10% of conflicts (very rare)
- **Total use case**: 0.01% of all saves

**Cost**: Entire table, circular dependencies, JSON serialization, maintenance  
**Benefit**: Show diff in 0.01% of cases (and we can do this without the table!)  
**Verdict**: Not worth it

---

## Recommended Approach

### Phase 1: MVP (Ship This)

```csharp
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning("Conflict detected for {Entity}", entityId);
    
    return Result.Conflict(
        "This record was modified by someone else. " +
        "Your changes were NOT saved. " +
        "Please reload and try again.");
}
```

**User sees**: Simple error message, clicks reload, re-enters changes.

### Phase 2: If Users Complain (Add Later)

```csharp
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var diff = await GetConflictDiffAsync(entry);
    
    return Result.Conflict(diff);  // Show diff UI
}
```

**User sees**: Diff view showing what changed, can choose which value to keep.

**Still no ConflictBackup table needed!**

---

## Summary

| Aspect | With ConflictBackup | Without ConflictBackup |
|--------|-------------------|----------------------|
| **Prevents overwrites** | ✅ Yes (OCC) | ✅ Yes (OCC) |
| **User sees error** | ✅ Yes | ✅ Yes |
| **User must reload** | ✅ Yes | ✅ Yes |
| **Can show diff** | ✅ Yes (from table) | ✅ Yes (from EF Core memory) |
| **Circular dependency** | ❌ Yes | ✅ No |
| **Extra table** | ❌ Yes | ✅ No |
| **JSON serialization** | ❌ Yes | ✅ No |
| **Maintenance overhead** | ❌ Yes | ✅ No |

**Conclusion**: ConflictBackup adds complexity without adding value. OCC alr prevents overwrites. If users want to see diffs, we can show them without storing anything in a table.

---

**Status**: ✅ Clarified  
**Recommendation**: Remove ConflictBackup table, use simple error message + reload
