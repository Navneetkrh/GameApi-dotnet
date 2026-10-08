# EF Core `DbContext` / `DbSet` — Cheat Sheet (for `NavNet/Data`)

## 1. `DbSet<T>` — your typed table handle

```csharp
public DbSet<Game> Games => Set<Game>();
```

A `DbSet` is both a **query** (LINQ over rows) and a **staging area** (tracks adds/edits/removes until saved). Nothing hits the DB until `SaveChanges()` (except direct `Find`/`Sql` calls).

## 2. Reading

```csharp
db.Games.ToList();                          // all rows
db.Games.Find(5);                           // by PK (checks memory first, fast)
db.Games.First(g => g.Id == 5);             // throws if missing
db.Games.FirstOrDefault(g => g.Id == 5);    // null if missing
db.Games.Where(g => g.Price < 20);          // filter (returns IQueryable — lazy!)
db.Games.OrderBy(g => g.Name);              // sort (.OrderByDescending)
db.Games.Skip(10).Take(10);                 // paging: page 2, size 10
db.Games.Count();                           // SELECT COUNT(*)
db.Games.Any(g => g.Price < 0);             // EXISTS check
db.Games.Include(g => g.Genre);             // eager-load navigation
db.Games.Include(g => g.Genre).ThenInclude(...); // nested include
db.Games.Select(g => g.Name);               // single column only
db.Games.AsNoTracking().ToList();           // read-only: faster, no change tracking
```

> `Where`/`OrderBy` build the query; `ToList`/`First`/`Count` execute it. Chain freely — one SQL round trip.

## 3. Writing (staged until `SaveChanges`)

```csharp
db.Games.Add(game);                         // INSERT on save
db.Games.AddRange(games);                   // bulk insert
db.Games.Remove(game);                      // DELETE on save
db.Games.Update(game);                      // UPDATE all columns on save
db.Entry(game).State = EntityState.Modified; // same, explicit

db.SaveChanges();                           // flush everything in ONE transaction
```

## 4. Async variants (use in endpoints/controllers)

```csharp
await db.Games.ToListAsync();
await db.Games.FindAsync(5);
await db.Games.FirstOrDefaultAsync(g => g.Id == 5);
await db.SaveChangesAsync();
```

> ASP.NET is async-first: prefer `*Async` in handlers so threads aren't blocked on DB I/O.

## 5. `DbContext` members (similar things to `DbSet`)

```csharp
db.SaveChanges() / SaveChangesAsync()  // persist staged changes
db.Set<Game>()                         // DbSet for any entity (no property needed)
db.Entry(game)                         // tracking info: .State, .OriginalValues
db.Database.EnsureCreated()            // create schema, NO migrations (prototypes only)
db.Database.Migrate()                  // apply migrations at runtime (alt. to dotnet ef update)
db.Database.BeginTransaction()         // manual transaction
db.Games.FromSql($"SELECT ...")        // raw SQL query (mapped to entity)
db.Database.ExecuteSql($"DELETE ...")  // raw SQL command
```

## 6. Full flow — your game

```csharp
// CREATE: DTO (validated) → model → stage → save (Id auto-assigned)
var game = new Game { Name = dto.Name, GenreId = dto.GenreId, Price = dto.Price, ReleaseDate = dto.ReleaseDate };
db.Games.Add(game);
await db.SaveChangesAsync();   // game.Id now set

// READ: model → DTO (never return the entity directly)
var dto = await db.Games.Include(g => g.Genre)
    .Select(g => new GameDto(g.Id, g.Name, g.Genre.Name, g.Price, g.ReleaseDate))
    .FirstOrDefaultAsync(g => g.Id == id);

// UPDATE: fetch → mutate → save (tracking detects the diff)
var game = await db.Games.FindAsync(id);
game.Price = dto.Price;
await db.SaveChangesAsync();

// DELETE
db.Games.Remove(game);
await db.SaveChangesAsync();
```

## 7. Rules of thumb

- Query with LINQ, never string-concat SQL (injection-safe by default).
- `Find` for PK lookup, `FirstOrDefault` + null-check otherwise.
- `Include` navigations you read; `AsNoTracking` for pure reads.
- One `SaveChanges` per request = one transaction.
- Return DTOs, not entities (avoids lazy-load surprises + over-posting).
