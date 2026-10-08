# C# for TS Devs — Full Picture (via NavNet)

You know TypeScript/Node. This maps 1:1 to C# using this repo.

## 0. Project file

TS: `package.json` lists deps + scripts.
C#: `NavNet/NavNet.csproj:1-18` lists deps + settings.

```xml
<TargetFramework>net10.0</TargetFramework> <!-- like "engines": node version -->
<Nullable>enable</Nullable>                <!-- like strictNullChecks -->
<ImplicitUsings>enable</ImplicitUsings>    <!-- auto-import common namespaces -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.12" />
```

Run: `dotnet run --project NavNet` = `npm run dev`.

## 1. Startup: `Program.cs`

TS/Express:
```ts
const app = express()
app.use(auth)
app.get('/games', handler)
app.listen(3000)
```

C#: `NavNet/Program.cs:9-49`
```csharp
var builder = WebApplication.CreateBuilder(args); // express()
builder.Services.AddControllers();                 // app.use() setup
builder.Services.AddSingleton<TokenService>();     // DI registration
var app = builder.Build();                         // build app
app.UseAuthentication();                           // middleware
app.MapControllers();                              // routes
app.MapGamesEndpoints();                           // routes from file
app.Run();                                         // listen
```

Pattern: `builder.Services` = what to inject, `app.Use/Map` = how requests flow.

## 2. `using` vs `import`

TS: import by **file path**.
```ts
import { GameDto } from './Dtos/GameDto.js'
```

C#: import by **namespace**, not path.
```csharp
// NavNet/Endpoints/GamesEndpoints.cs:1-4
using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Dtos;
```

`ImplicitUsings` auto-adds `System`, `System.Collections.Generic`, etc. so you don't write them.

## 3. Namespace: last-name, not file

File: `NavNet/Dtos/GameDto.cs:4`
```csharp
namespace NavNet.Dtos;
public record class GameDto(...);
```

File: `NavNet/Models/Game.cs:1`
```csharp
namespace NavNet.Models;
public class Game { ... }
```

Full name = `Namespace.Class`:
- `NavNet.Dtos.GameDto`
- `NavNet.Models.Game`
- `NavNet.Services.PasswordHelper` (`NavNet/Services/PasswordHelper.cs:3,6`)
- `System.Security.Cryptography.RandomNumberGenerator`

Two ways to use:
```csharp
using NavNet.Dtos;
var x = new GameDto(...); // short, via using

var y = new NavNet.Dtos.GameDto(...); // long, no using needed
```

Rule: namespace can be **anything**. Folder = convention only (`NavNet` = project name + folder `Dtos`). This compiles and runs, even though all three differ:

```csharp
// File: Demo/WrongFolder/FileA.cs
namespace Demo.Dtos; // folder says WrongFolder, file says FileA, ns says Dtos
public class Bar { ... }
```
```csharp
using Demo.Dtos; // you import ns, file location ignored
Console.WriteLine(Bar.Hi());
```

Bad org still works because compiler only looks at namespace.

Try: change `namespace NavNet.Dtos` to `namespace Foo` in `GameDto.cs`, then fix call sites to `using Foo;`. It builds. Change it back.

## 4. Files: OOP container outside, functional inside

TS: file can export anything.
C#: file must live inside `namespace` + `class` (container), but code inside methods is normal imperative/functional.

```csharp
namespace NavNet.Endpoints;          // container
public static class GamesEndpoints  // container
{
  public static void MapGamesEndpoints(this WebApplication app) // method
  {
    // inside here: just functions, lambdas, ifs — like TS
    group.MapGet("/", async (AppDbContext db) => Results.Ok(...));
  }
}
```

## 5. `static` vs instance

Toolbox (no memory) vs employee (has memory).

Static — `NavNet/Services/PasswordHelper.cs:6,12`:
```csharp
public static class PasswordHelper // sealed, no `new`
{
  public static string Hash(string password) { ... }
}
// use: PasswordHelper.Hash("pw") — like Math.random()
```

Static class rules: all members must be `static`, no `new`, required for extension methods (`NavNet/Data/DataExtensions.cs:6,8`).

Instance — `NavNet/Services/TokenService.cs:9,11`:
```csharp
public class TokenService(IConfiguration config) // needs config to live
{
  public string CreateToken(User user) { ... var key = config["Jwt:Key"] ... }
}
// use: builder.Services.AddSingleton<TokenService>(); then inject TokenService svc
// use: svc.CreateToken(user)
```

If it needs config/db/state → instance + DI. If pure function → static.

Try: call `PasswordHelper.Hash("hello")` from `Program.cs`. Then try `new PasswordHelper()` — compile error.

## 6. Two API styles (same job)

Controller (OOP style) — `NavNet/Controllers/GamesController.cs:10,13`:
```csharp
[ApiController]
[Route("api/[controller]")] // -> /api/games
public class GamesController(AppDbContext db) : ControllerBase
{
  [HttpGet] public async Task<ActionResult<List<GameDto>>> GetGames() { ... }
}
```

Minimal API (functional style) — `NavNet/Endpoints/GamesEndpoints.cs:8,11`:
```csharp
public static class GamesEndpoints
{
  public static void MapGamesEndpoints(this WebApplication app)
  {
    var group = app.MapGroup("/games");
    group.MapGet("/", async (AppDbContext db) => ... );
  }
}
```
Registered in `NavNet/Program.cs:42-44` via `app.MapGamesEndpoints()`.

Same `async`, same LINQ, same DTOs inside. Only routing style differs.

## 7. DTO vs Model

Model = DB shape, mutable: `NavNet/Models/Game.cs:3-11`
```csharp
public class Game
{
  public int Id { get; set; } // get+set = let, mutable
  public string Name { get; set; } = string.Empty;
}
```

DTO = API shape, immutable: `NavNet/Dtos/GameDto.cs:6-12`
```csharp
public record class GameDto(int Id, string Name, ...); // record = value equality, concise
```

TS mental model:
```ts
type Game = { id: number; name: string } // DB row
type GameDto = Pick<Game, 'id'|'name'>   // what you send to client
```

## 8. Validation: attributes = decorators

TS (zod/class-validator):
```ts
@IsString() @MaxLength(256) name: string
```

C#: `NavNet/Dtos/CreateGameDto.cs:5-10`
```csharp
public record class CreateGameDto(
  [Required, StringLength(256, MinimumLength = 1)] string Name,
  [Required, Range(1, int.MaxValue)] int GenreId,
  [Required, Range(0, 999999)] decimal Price,
  [Required] DateOnly ReleaseDate
);
```
`[X]` above a prop = decorator. Runs automatically with `[ApiController]` or `AddValidation()`.

## 9. LINQ = array methods for DB

TS:
```ts
games.filter(g => g.id === id).map(g => ({ id: g.id, name: g.name }))
```

C#: `NavNet/Controllers/GamesController.cs:17-18`
```csharp
await db.Games.Where(g => g.Id == id)
  .Select(g => new GameDto(g.Id, g.Name, g.GenreId, g.Price, g.ReleaseDate))
  .FirstOrDefaultAsync();
```
`Where` = filter, `Select` = map, `FirstOrDefaultAsync` = find-or-undefined, `ToListAsync` = to-array. `g => ...` is arrow function, same as TS.

## 10. `async Task` = `async Promise`

TS: `async function f(): Promise<GameDto>`
C#: `NavNet/Controllers/GamesController.cs:15`
```csharp
public async Task<ActionResult<List<GameDto>>> GetGames()
{
  return await db.Games...ToListAsync();
}
```
`Task<T>` = `Promise<T>`, `await` identical.

## 11. DI vs manual import

TS: `import db from './db'; new TokenService(config)`
C#: declare need in signature, framework provides it.

```csharp
// NavNet/Program.cs:16
builder.Services.AddSingleton<TokenService>();

// then anywhere:
group.MapPost("/", async (CreateGameDto dto, AppDbContext db, TokenService svc) => ...);
//                                         ^ injected, no import path
```
`AppDbContext` itself registered in `NavNet/Program.cs:13-14`, defined in `NavNet/Data/AppDbContext.cs:6-12` with `DbSet<Game> Games` (= table).

## Cheat sheet

| TS | C# |
|---|---|
| `import from './file'` | `using Namespace;` |
| `package.json` | `NavNet.csproj` |
| `express(), app.use, app.get` | `CreateBuilder, Services, Use/Map, Run` |
| `type/interface` out | `record DTO` |
| `class Entity` DB | `class Model` with `{get;set;}` |
| `@Decorator` | `[Attribute]` |
| `filter/map/find` | `Where/Select/FirstOrDefaultAsync` |
| `Promise<T>` | `Task<T>` |
| `new Svc(config)` | `AddSingleton<Svc>()` + inject |
| `Math.xxx()` | `static class xxx` |
