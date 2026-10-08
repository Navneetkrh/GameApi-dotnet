# .NET Concurrency + DB — When to Lock, When Not To

## 0. The 3 kinds of work

| Work | Do | Don't |
|---|---|---|
| I/O (DB, HTTP, file) | `await db.Games.ToListAsync()` | `new Thread()`, `Task.Run` for I/O, `.Result/.Wait()` |
| CPU (hash, resize, JSON big) | `await Task.Run(() => Hash(pw))`, `Parallel.ForEach` | block pool thread in request path |
| Background | `BackgroundService` + `PeriodicTimer` | fire-and-forget `async void` (exceptions lost) |

ASP.NET Core has no `SynchronizationContext`. After `await`, any pool thread resumes. Thread ID changes = normal.

## 1. ThreadPool rules that bite

* 1 pool per process. `GetMinThreads` = cores. Grows 1-2/sec under queue pressure.
* Starvation = queue grows + threads climb 2-3x cores + CPU low. Fix: remove blocking.
* Never: `Task.Result`, `Task.Wait()`, `GetAwaiter().GetResult()` in request path, `Thread.Sleep` (use `await Task.Delay`), `lock` containing `await`.

```csharp
// BAD — blocks pool thread during DB wait, starves under load:
var x = db.Games.ToListAsync().Result;
// GOOD:
var x = await db.Games.ToListAsync();
```

## 2. Primitives — pick by need

```csharp
// same-thread sync, no await inside:
private static readonly object _gate = new();
lock (_gate) { _dict["k"] = v; }

// async-compatible gate (can await inside):
private static readonly SemaphoreSlim _s = new(1,1);
await _s.WaitAsync();
try { await DoAsync(); } finally { _s.Release(); }

// atomic counter, no lock:
Interlocked.Increment(ref _hits);

// shared map/queue, no lock:
ConcurrentDictionary<string, GameDto> cache = new();
Channel<string> queue = Channel.CreateUnbounded<string>();

// CPU parallelism:
Parallel.ForEach(ids, id => Compute(id));
```

* `lock/Monitor`: sync only. Cheap thin-lock → OS event if contended.
* `SemaphoreSlim(1,1)`: async mutex. `ReaderWriterLockSlim`: many readers / one writer (sync only).
* `Mutex`: cross-process. Slow. Only for single-instance apps.
* `Interlocked`: single atomic op. Fastest.
* `ConcurrentDictionary/Queue/Stack/Bag`: lock-free-ish internally. Prefer over `lock(dict)`.
* `Channel<T>`: producer/consumer queue for background workers. Prefer over hand-rolled queue + lock.

## 3. DB rules (NavNet)

* `AddDbContext` = **scoped**: 1 `AppDbContext` per request. `DbContext` is NOT thread-safe. Never singleton, never static, never shared across threads, never `lock(db)`.
* 2 concurrent requests = 2 contexts = safe. DB handles row isolation via transactions/locks internally.
* Multi-step atomicity: use explicit transaction:
```csharp
await using var tx = await db.Database.BeginTransactionAsync();
db.Games.Add(game); await db.SaveChangesAsync();
await tx.CommitAsync();
```
* Conflicting writes (2x register same username): rely on constraint, not lock:
```csharp
// AppDbContext.cs:16 HasIndex(u => u.Username).IsUnique()
try { db.Users.Add(u); await db.SaveChangesAsync(); }
catch (DbUpdateException) { return Results.Conflict("username taken"); }
```
* Lost update (2x edit same game): add `[Timestamp] byte[] RowVersion` → `DbUpdateConcurrencyException` → return 409, client retries.
* `AsNoTracking()` for read-only lists (`Genres` list) — less change-tracker garbage, less Gen0 pressure.

## 4. Deadlock checklist

1. `lock` + `await` inside → deadlock/starvation. Use `SemaphoreSlim`.
2. `.Result` on UI/legacy ASP.NET sync context → deadlock. Use `await` all the way.
3. Lock ordering: always take `A` then `B` everywhere. Reverse order in one path = deadlock.
4. Static state + pool threads: assume any thread. Guard with `Concurrent*` or gate above.
5. EF: one context per async flow. Don't pass entity across threads then `SaveChanges` on another.

## 5. NavNet defaults (why no locks there)

* `PasswordHelper` static pure functions — no shared state → no lock.
* `TokenService` singleton stateless (only reads `config`) → no lock.
* `GamesEndpoints` handlers use per-request `db` → no lock.
* Seed in `DataExtensions.MigrateDb` runs once at startup single-threaded → no lock.
* You add a lock only when you add shared mutable memory: in-memory rate limiter, cache, counter, file writer.
