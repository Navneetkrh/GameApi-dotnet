# .NET Concurrency + DB — When to Lock, When Not To

## 0. Intro from zero (read this first)

You're not stupid. TS just hid this from you. Node = 1 cook. .NET = team of cooks.

**Words (3 only):**
* **Concurrency** = juggling. Start A, start B while A waits, finish both. 1 cook can do it.
* **Parallelism** = actually same-time. 2 cooks, 2 stoves, 2 cores.
* **Async** = waiting without holding the stove. Order placed → bell rings later → cook does other orders meanwhile.

**Cast:**
* **Thread** = a cook. Has hands (CPU) + notepad (stack).
* **ThreadPool** = the hired team. 1 per process. You don't hire/fire, runtime does.
* **Task** = an order ticket. `Task&lt;string&gt;` = "string coming later".
* **`await`** = "bell me when ready, I'll do other tickets meanwhile". No cook stands frozen.

**Node vs .NET:**
* Node: 1 cook, everything is `await`/callback. Never hire a 2nd cook (except `worker_threads`).
* .NET web: team of cooks (one per core-ish). Request comes in → free cook takes it → hits `await db` → cook drops ticket, takes next request → DB bell rings → any free cook resumes. Thread ID changing after `await` = normal.

**Proof (just ran):** 5x 100ms I/Os:
```
sequential: await one, then next → 507ms (5 * 100, one stove, one by one)
concurrent: start all, await once → 103ms (one overlapping wait)
```
Same cooks, 5x faster, because nobody stood frozen.

```csharp
// sequential — slow, 500ms:
for (int i = 1; i <= 5; i++) await FakeIo(i);

// concurrent — fast, 100ms:
await Task.WhenAll(Enumerable.Range(1, 5).Select(FakeIo));
```

**Blocking vs freeing:**
* `await Task.Delay(100)` = freed. Cook takes other orders.
* `Thread.Sleep(100)` / `.Result` / `.Wait()` = frozen. Cook stares at wall 100ms, paid, useless. Under load this starves the team (all cooks frozen, queue grows, CPU idle — that's "threadpool starvation").

**Rule of the whole doc:** I/O → `await` (free the cook). CPU → `Task.Run`/`Parallel` (give it a cook). Shared notepad (`static`) → guard it (lock/Concurrent). DB rows → let the DB guard them (constraints/transactions), not your memory lock.

## 1. The 3 kinds of work

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

## 3. Hands-on examples (all ran OK)

TS mental model first: `Promise.all([...])` = `Task.WhenAll`, `worker_threads` ≈ `Task.Run/Parallel`.

### Ex1 — I/O overlap, no extra threads (like `Promise.all`)
```csharp
static async Task<string> FakeIo(int id, int ms) { await Task.Delay(ms); return $"r{id}"; }

var tasks = Enumerable.Range(1, 5).Select(i => FakeIo(i, 50));
var all = await Task.WhenAll(tasks); // ~50ms total, not 250ms
// ran: r1,r2,r3,r4,r5
```
No thread is held during `Delay`/DB wait. This is how 5x `GET /games/{id}` overlap. Never `Task.Run` for this.

### Ex2 — CPU work off the request thread (like `worker_threads`)
```csharp
string h = await Task.Run(() => ExpensiveHash(pw)); // pool thread does CPU, request thread freed
```
Ran with `len=1000`. Rule: I/O → plain `await`, CPU → `await Task.Run`.

### Ex3 — data parallelism (`Parallel.ForEach`)
```csharp
var bag = new ConcurrentBag<int>();
Parallel.ForEach(ids, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i => bag.Add(i * i));
// ran: count=8
```
Uses pool threads, caps at 4. For CPU loops only. Never mutate a plain `List<T>` inside — use `ConcurrentBag` or local + merge.

### Ex4 — throttle with `SemaphoreSlim` (max N at once)
```csharp
using var gate = new SemaphoreSlim(2, 2);
async Task<string> One(int i) { await gate.WaitAsync(); try { return await FakeIo(i, 30); } finally { gate.Release(); } }
var res = await Task.WhenAll(Enumerable.Range(1, 4).Select(One));
// ran: r1,r2,r3,r4, max 2 overlapping
```
This is the async `lock`. `lock` can't contain `await` — this can.

### Ex5 — producer/consumer with `Channel` (background worker)
```csharp
var ch = Channel.CreateUnbounded<int>();
_ = Task.Run(async () => { for (int i = 1; i <= 3; i++) await ch.Writer.WriteAsync(i); ch.Writer.Complete(); });
int sum = 0;
await foreach (var x in ch.Reader.ReadAllAsync()) sum += x; // ran: sum=6
```
Request handlers `WriteAsync`, a `BackgroundService` reads. No locks, backpressure via `CreateBounded`.

### Ex6 — shared counter, 3 correct ways
```csharp
int hits = 0;
Parallel.For(0, 1000, _ => Interlocked.Increment(ref hits)); // ran: 1000 — single atomic op, fastest

var d = new ConcurrentDictionary<string,int>();
d.AddOrUpdate("k", 1, (_, v) => v + 1); // ran: 1000 — map, no lock

object gate = new(); int plain = 0;
Parallel.For(0, 1000, _ => { lock (gate) plain++; }); // ran: 1000 — sync-only, never await inside
```

### Anti-examples (don't)
```csharp
var x = db.Games.ToListAsync().Result; // blocks pool thread → starvation under load
lock (_gate) { await db.SaveChangesAsync(); } // illegal-ish: blocks thread across await → use SemaphoreSlim
var sharedDb = db; Task.Run(() => sharedDb.Games.Add(g)); // DbContext not thread-safe → 1 context per flow
```

## 4. DB rules (NavNet)

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

## 5. Deadlock checklist

1. `lock` + `await` inside → deadlock/starvation. Use `SemaphoreSlim`.
2. `.Result` on UI/legacy ASP.NET sync context → deadlock. Use `await` all the way.
3. Lock ordering: always take `A` then `B` everywhere. Reverse order in one path = deadlock.
4. Static state + pool threads: assume any thread. Guard with `Concurrent*` or gate above.
5. EF: one context per async flow. Don't pass entity across threads then `SaveChanges` on another.

## 6. NavNet defaults (why no locks there)

* `PasswordHelper` static pure functions — no shared state → no lock.
* `TokenService` singleton stateless (only reads `config`) → no lock.
* `GamesEndpoints` handlers use per-request `db` → no lock.
* Seed in `DataExtensions.MigrateDb` runs once at startup single-threaded → no lock.
* You add a lock only when you add shared mutable memory: in-memory rate limiter, cache, counter, file writer.
