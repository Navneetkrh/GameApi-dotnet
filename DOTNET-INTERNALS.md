# .NET vs ASP.NET Core — How Both Work Internally (via NavNet)

## 1. The split

* **.NET** = runtime + base libs. Runs any code. Like Node itself.
  `Microsoft.NETCore.App 10.0.12` on your machine.
* **ASP.NET Core** = web framework on top. HTTP only. Like Express on Node.
  `Microsoft.AspNetCore.App 10.0.12` on your machine.

`NavNet/NavNet.csproj:1` `Sdk="Microsoft.NET.Sdk.Web"` = pull both.

## 2. .NET internals: what happens to your code

```
GameDto.cs  →  C# compiler  →  CIL + metadata (.dll)  →  JIT (per method, first call)  →  native code
```

1. **CIL:** CPU-independent instructions. Same `.dll` runs on Mac/Windows/Linux.
2. **Metadata:** types, members, `namespace NavNet.Dtos` + `class GameDto`. This is why file paths disappear — identity left is `Assembly + Namespace + Name`.
3. **JIT:** converts CIL → native on first call, caches. `Player.SayHi()` compiles once, runs native after.
4. **GC:** frees `new Game()` when unreachable. Gen 0 (short-lived, e.g. DTO per request) → Gen 1 → Gen 2 (long-lived, e.g. Singleton `TokenService`). You don't `free()`.

TS map: `tsc → .js → V8 JIT → run` ≈ `csc → CIL → RyuJIT → run`. GC in both, no manual free.

Docs:
* CLR overview: https://learn.microsoft.com/en-us/dotnet/standard/clr
* Managed execution (CIL→JIT): https://learn.microsoft.com/en-us/dotnet/standard/managed-execution-process
* GC fundamentals: https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals

## 3. ASP.NET Core internals: what happens to a request

```
Browser → Kestrel (server) → Middleware pipeline → Routing → Endpoint → your code → Response back up
```

`NavNet/Program.cs:9-49`:
```csharp
var builder = WebApplication.CreateBuilder(args); // host + config + DI container
builder.Services.AddSingleton<TokenService>();    // DI: what to inject
builder.Services.AddDbContext<AppDbContext>(...);// DI: tables
var app = builder.Build();                        // build pipeline

app.UseAuthentication();  // middleware: who are you?
app.UseAuthorization();   // middleware: are you allowed?
app.MapControllers();     // endpoints: Controllers
app.MapGamesEndpoints();  // endpoints: Minimal API (GamesEndpoints.cs:11)
app.MigrateDb();          // run once at startup (DataExtensions.cs:8)
app.Run();                // start Kestrel, block
```

Order matters: request goes **down** in file order, response comes **back up** reverse order. Auth before endpoints, or endpoints run unauthenticated.

* **Kestrel:** cross-platform HTTP server. Listens on port, parses HTTP → `HttpContext`.
* **Middleware:** `UseX` = onion layer. Can short-circuit (return early, e.g. `Results.Unauthorized`) or call `next()`.
* **Routing:** `UseRouting` picks endpoint by URL+verb. `MapGet("/games/{id}")` + `[HttpGet("{id}")]` both register endpoints. `RequireAuthorization()` attaches metadata routing-aware middleware reads.
* **Endpoint:** your lambda in `GamesEndpoints.cs:14` or method in `GamesController.cs:15`. Gets `(AppDbContext db)` injected by DI, runs LINQ, returns `Results.Ok(dto)`.

Try: add in `Program.cs` before auth:
```csharp
app.Use(async (ctx, next) => { Console.WriteLine($"IN {ctx.Request.Path}"); await next(); Console.WriteLine("OUT"); });
```
Hit `/games` → see IN → endpoint → OUT.

Docs:
* Fundamentals (DI+config+pipeline): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-10.0
* Middleware pipeline: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0
* Routing: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0
* Kestrel: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-10.0

## 4. Full NavNet example: GET /games/1

1. Kestrel receives `GET /games/1` → `HttpContext`.
2. `UseAuthentication` reads JWT → `ClaimsPrincipal`. `UseAuthorization` checks `[Authorize]` / `RequireAuthorization()`.
3. Routing matches `group.MapGet("/{id}")` in `GamesEndpoints.cs:18`.
4. DI provides `AppDbContext db` (scoped per request).
5. LINQ runs: `db.Games.Where(g => g.Id == id).Select(g => new GameDto(...)).FirstOrDefaultAsync()` — `Where`=filter, `Select`=map.
6. `Results.Ok(dto)` → JSON → back up through middleware → Kestrel → browser.

## Cheat

| Node | .NET | ASP.NET Core |
|---|---|---|
| `node` runtime | `Microsoft.NETCore.App` (CLR+GC+JIT) | — |
| `express()` | — | `WebApplication.CreateBuilder/Build` |
| `app.use()` | — | `app.UseX()` middleware |
| `app.get('/x')` | — | `app.MapGet/MapControllers` |
| `npm package` | assembly `.dll` | NuGet `Microsoft.AspNetCore.*` |
| `tsc → js → V8` | `csc → CIL → JIT` | — |
