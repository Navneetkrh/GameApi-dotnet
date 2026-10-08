# Auth — JWT bearer for `NavNet` (cheat sheet)

Same flow in both styles: minimal (`/auth/*`) and MVC (`/api/Auth/*`).
Reads stay open; writes need a token.

## 1. Pieces

| Piece | File | Job |
|---|---|---|
| `User` | `Models/User.cs` | `Id`, `Username` (unique), `PasswordHash` — never the password |
| `RegisterDto` / `LoginDto` | `Dtos/AuthDtos.cs` | validated input (username 3–64, password 8–100) |
| `PasswordHelper` | `Services/PasswordHelper.cs` | PBKDF2-SHA256 hash + fixed-time verify |
| `TokenService` | `Services/TokenService.cs` | mints JWT (id + name claims, 8h expiry) |
| Validation | `Program.cs` | `AddJwtBearer` checks signature/issuer/audience per request |

## 2. Request flow

```
register/login (OPEN) → check hash → TokenService.CreateToken → {"token": "eyJ..."}
                                                          ↓
writes (LOCKED) → Authorization: Bearer eyJ... → JwtBearer validates → handler runs
                → no/invalid token → 401, valid token → 200/201/204
```

## 3. Protecting routes

```csharp
// minimal: per-route
group.MapPost("/", handler).RequireAuthorization();

// MVC: per-action
[HttpPost, Authorize] public async Task<ActionResult<GameDto>> CreateGame(...)
```

Reads have neither — open by design in this tutorial.

## 4. Config — never commit a real key

```zsh
cd NavNet
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Jwt:Issuer" "NavNet"
dotnet user-secrets set "Jwt:Audience" "NavNet"
```

`appsettings.json` holds a dev-only placeholder so the app boots;
secrets/env override it (see `CONFIG.md` layering). Key must be ≥32 chars.

## 5. Test order (`game.http`)

1. `POST /auth/register` → copy `token`
2. `POST /games` without header → expect 401
3. Same with `Authorization: Bearer <token>` → expect 201
4. `POST /auth/login` → new token, same shape

## 6. Rules of thumb

- Store hashes, never passwords; verify with fixed-time compare.
- 401 = who are you (bad/missing token); 403 = you may not (policy/role).
- Short expiries + HTTPS in prod; this tutorial uses 8h + HTTP for learning.
- Changing username/password rules = DTO attributes only, no migration.
- Changing `User` shape = new migration (`dotnet ef migrations add ...`).
