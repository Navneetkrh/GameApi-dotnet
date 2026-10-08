# .NET VM Internals — No Hand-Waving (CLR, JIT, GC, Threads)

> No Express comparisons here. Only what happens inside the process.

## 0. What the CLR is

A user-mode VM made of:

* **EE (Execution Engine):** loads assemblies, lays out objects, dispatches calls, tracks GC refs.
* **JIT (RyuJIT):** CIL → native per CPU. `src/coreclr/jit` in dotnet/runtime.
* **GC:** tracing, generational, compacting heap.
* **Type loader + metadata:** `Assembly + Namespace + Type` identity. File paths gone after compile.
* **ThreadPool + IOCP:** 1 pool per process. Worker threads + I/O completion threads.

Flow:
```
.cs → csc → PE (.dll): CIL + metadata → CLR loads → JIT per method (Tier0 fast → Tier1 optimized) → run → GC reclaims
```

Docs:
* CLR overview: https://learn.microsoft.com/en-us/dotnet/standard/clr
* Managed execution (CIL→JIT→run): https://learn.microsoft.com/en-us/dotnet/standard/managed-execution-process
* RyuJIT overview: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/ryujit-overview.md
* Tiered compilation: https://github.com/dotnet/runtime/blob/main/docs/design/features/tiered-compilation.md

## 1. CIL: the fake CPU

Stack machine. `02 03 58 2A` = ldarg.0, ldarg.1, add, ret. No registers, no x64/ARM opcodes.
Same bytes on all CPUs (you dumped this earlier: `IL bytes: 02 03 58 2A` on Arm64).

## 2. JIT: Tier0 → Tier1 → OSR → PGO

* **Tier0:** minimal opts, fast to compile. Runs immediately at startup. Call counter via Prestub (~30 calls = hot).
* **Timer:** ~100ms without new Tier0 JIT = startup over → allow promotion. Avoids slowing startup with background Tier1 compiles.
* **Tier1:** full opts (inlining, CSE, range-check elim, devirtualization), compiled on ThreadPool background thread, then hot-swapped.
* **R2R (ReadyToRun):** precompiled Tier0 shipped in dll. Replaced by JIT Tier1 if hot (JIT sees real CPU + loaded deps).
* **Dynamic PGO:** Tier0Instrumented collects block counts → Tier1 uses them (better inlining, hot/cold split).
* **OSR (On-Stack Replacement):** long loop in `Main` called once would trap you in Tier0 forever. OSR patchpoints at loop back-edges jump live frame Tier0 → optimized version mid-loop.

Mental model: first run = quick draft, hot path = rewritten optimized in background, long loop = upgraded mid-flight.

## 3. GC: generational tracing

* **Roots:** statics, stack locals/params, registers, GC handles, finalize queue. Graph walk from roots = live. Rest = garbage.
* **Gen0:** new objects. Collected most. Survive → promote Gen1.
* **Gen1:** buffer. Survive → Gen2.
* **Gen2 + LOH:** long-lived. Full collection = Gen2+1+0. LOH (≥85KB) normally not compacted (copy cost).
* **Phases:** mark live → relocate refs → compact (memmove survivors, fix pointers, reset heap pointer).
* **Managed vs unmanaged:** GC knows managed heap only. File/socket handles = unmanaged → need `Dispose`/`SafeHandle`, else leak until finalizer.

NavNet mapping: per-request `GameDto` dies in Gen0. Singleton `TokenService`, `AppDbContext` pool internals live Gen2.

Doc: https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals

## 4. Threads: 1 pool per process

* **Worker threads:** run `Task`, `Task.Run`, `QueueUserWorkItem`, timer callbacks, Tier1 background JIT.
* **IOCP threads:** blocked in `GetQueuedCompletionStatus` on Windows (epoll/kqueue on Linux). No thread is consumed while I/O flies. On completion, one wakes, marks `Task` complete, queues continuation.
* **Dispatch:** your `await db.ToListAsync()` sends I/O on calling thread, returns thread to pool. Zero threads held during DB wait.
* **Hill-climbing:** pool starts at `GetMinThreads` (= CPU count), adds ~1-2/sec if queue grows and CPU <100%. Starvation signal: thread count climbs to 2-3x cores, queue length grows, CPU low, latency high. Cause is almost always blocking: `.Result/.Wait()`, `lock` around `await`, sync I/O.
* **Background:** pool threads are background (`IsBackground=true`). Don't keep process alive. Never store `ThreadStatic` expectations across awaits — thread changes after await.

Docs:
* Managed pool: https://learn.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool
* Starvation diagnosis: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-threadpool-starvation

## 5. async: state machine, not threads

`async` doesn't create a thread. Compiler rewrites method into struct state machine (`<Method>d__0`) + `AsyncTaskMethodBuilder`:

```csharp
// you write:
async Task<int> F() { var x = await Db(); return x.Length; }
// compiler emits roughly:
struct d__0 { int state; TaskAwaiter awaiter; void MoveNext() { switch(state){...} } }
```

* Runs sync until first incomplete `await`. Captures `ExecutionContext` (AsyncLocal flow). If `Task` awaiter and `SynchronizationContext.Current != null`, posts continuation there, else ThreadPool.
* ASP.NET Core: no `SynchronizationContext` (unlike old ASP.NET / WPF). Continuation = any pool thread. That's why no UI-thread deadlock by default, but still never block.
* `ExecutionContext` flows (ambient state). `SynchronizationContext` doesn't flow — captured/posted only.

Docs:
* TAP model: https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model
* How async really works: https://devblogs.microsoft.com/dotnet/how-async-await-really-works/
* ExecutionContext vs SynchronizationContext: https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/executioncontext-synchronizationcontext

## 6. Memory model in one paragraph

Reads/writes can reorder unless synchronized. `lock`/`Monitor`, `Interlocked`, `volatile`, `SemaphoreSlim`, `Concurrent*` insert barriers. `await` boundaries are safe handoff points (continuation sees prior writes via ExecutionContext flow + task completion fence). Don't roll your own flag polling without `Volatile`/`Interlocked`.
