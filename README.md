# NavNet — Game Store API (ASP.NET Core tutorial)

Two implementations of the same CRUD API, kept side by side for learning:

| Style | Route | File |
|---|---|---|
| MVC controllers + `[ApiController]` | `/api/Games` | `NavNet/Controllers/GamesController.cs` |
| Minimal APIs + `AddValidation()` | `/games` | `NavNet/Endpoints/GamesEndpoints.cs` |

SQLite + EF Core (`games.db`, gitignored). DTOs validate input; `Game`/`Genre` models map the tables.

## Run

```zsh
dotnet run --project NavNet/NavNet.csproj        # → http://localhost:5204
dotnet ef database update --project NavNet/NavNet.csproj  # schema (or auto via MigrateDb)
```

Test with `game.http` (VS Code REST Client) or curl.

## Docs

| Doc | What |
|---|---|
| `NavNet/APPFLOW.md` | Full app flow: startup → request → shutdown, with diagrams |
| `NavNet/AUTH.md` | JWT auth: users, hashing, tokens, protecting writes |
| `NavNet/RUNTIME.md` | Threads, memory/GC, disposal, SQLite concurrency, scaling |
| `NavNet/RUNTIME.md` | How the webapp works: threads, memory/GC, DI scopes, SQLite concurrency |
| `NavNet/CONFIG.md` | Configuration: layers, options, secrets, terminal overrides |
| `NavNet/Dtos/VALIDATORS.md` | Validation attributes cheat sheet |
| `NavNet/Models/MODELS.md` | EF Core modeling cheat sheet |
| `NavNet/Data/DBCONTEXT.md` | `DbContext`/`DbSet` ORM cheat sheet + recipes |

## Commits

History is the tutorial: DTO validation → minimal endpoints → `AddValidation` → EF setup →
genres → builder seeding → DB-backed handlers → config. `git log --oneline` to walk it.
