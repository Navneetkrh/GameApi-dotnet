# ASP.NET Configuration — Cheat Sheet (for `NavNet`)

Sources load in order, later wins: `appsettings.json` → `appsettings.{Env}.json` → user-secrets (Dev) → env vars (`__` = `:`) → CLI args (`--Section:Key value`).

## 1. Connection strings

```json
// appsettings.json
{ "ConnectionStrings": { "Games": "Data Source=games.db" } }
```

```csharp
builder.Configuration.GetConnectionString("Games"); // reads ConnectionStrings:Games
```

Env override: `ConnectionStrings__Games="Data Source=/tmp/x.db"`

## 2. Simple values

```json
{ "PageSize": 20, "EnableCache": true }
```

```csharp
int pageSize = builder.Configuration.GetValue<int>("PageSize", 20); // 20 = fallback
bool cache = builder.Configuration.GetValue<bool>("EnableCache");
```

## 3. Nested sections → options class (multiple settings, typed)

```json
{
  "GameStore": {
    "PageSize": 20,
    "MaxResults": 100,
    "Currency": "USD"
  }
}
```

```csharp
public class GameStoreOptions
{
    public int PageSize { get; set; } = 20;
    public int MaxResults { get; set; } = 100;
    public string Currency { get; set; } = "USD";
}

// Program.cs — register once
builder.Services.Configure<GameStoreOptions>(
    builder.Configuration.GetSection("GameStore"));

// inject anywhere (controller ctor, endpoint param, seeder)
public GamesController(IOptions<GameStoreOptions> opts) { ... }
opts.Value.PageSize
```

Env override: `GameStore__PageSize=50`. CLI: `dotnet run --GameStore:PageSize 50`.

## 4. Arrays / lists

```json
{ "AllowedGenres": ["Action", "FPS", "RPG"] }
```

```csharp
string[] genres = builder.Configuration.GetSection("AllowedGenres").Get<string[]>() ?? [];
// env form: AllowedGenres__0=Action  AllowedGenres__1=FPS
```

## 5. Secrets (dev) — never commit passwords/keys

```zsh
dotnet user-secrets init --project NavNet/NavNet.csproj   # once: adds UserSecretsId
dotnet user-secrets set "ConnectionStrings:Games" "Data Source=prod-copy.db" --project NavNet/NavNet.csproj
dotnet user-secrets list --project NavNet/NavNet.csproj
```

Stored in `~/.microsoft/usersecrets/<id>/secrets.json`, auto-loaded in Development only. Same `GetConnectionString` call reads them.

## 6. Per-request access (minimal endpoints)

```csharp
group.MapGet("/page-size", (IConfiguration c) =>
    Results.Ok(c.GetValue<int>("GameStore:PageSize")));
```

## 7. Rules of thumb

- Connection strings → `GetConnectionString` (never hardcode, never commit passwords).
- 1–2 loose values → `GetValue<T>` with fallback.
- 3+ related values → options class + `IOptions<T>`.
- Dev secrets → user-secrets; prod secrets → env vars / KeyVault.
- Debug what's winning: expose `IConfiguration` temporarily and print the key.

## 8. Terminal overrides (`:` → `__`, `\` continues the line)

```zsh
# one command only
PricingApi__Key="pk-abc" Jwt__Key="s3cret" dotnet run --project NavNet/NavNet.csproj

# whole shell session
export PricingApi__Key="pk-abc"
export Jwt__Key="s3cret"

# beats env vars — CLI args (use : here, not __)
dotnet run --project NavNet/NavNet.csproj --PricingApi:Key "pk-abc" --Jwt:Key "s3cret"

# swap whole sections via environment, not terminal
DOTNET_ENVIRONMENT=Staging dotnet run --project NavNet/NavNet.csproj  # loads appsettings.Staging.json
```

Sections override leaf-by-leaf only — env vars can't carry JSON blobs.

## 9. Secret stores ladder

1. user-secrets — local dev, outside repo, Development only
2. env vars — VPS / containers (`docker run -e PricingApi__Key=...`)
3. mounted files — K8s/Docker secrets via `AddKeyPerFile("/run/secrets")`
4. cloud vaults — Key Vault / Secrets Manager, audited + rotated (prod answer)
5. self-hosted — HashiCorp Vault

Ladder: secrets on laptop → env on small host → vault with a team or real user data.
