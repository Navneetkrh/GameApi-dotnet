# C# EF Core Models — Cheat Sheet (for `NavNet/Models`)

Models describe **what the DB stores**. Input rules (`[Required]`, `[Range]`…) belong on DTOs, not here.
Convention first: `Id`/`GameId` = PK + autoincrement, `DbSet<Game> Games` = table `Games`. Annotate only to override.

## 1. Keys

```csharp
[Key]                                             // PK.
                                                  // free if named Id / GameId — skip it
[DatabaseGenerated(DatabaseGeneratedOption.Identity)] // auto-increment.
[DatabaseGenerated(DatabaseGeneratedOption.Computed)] // DB-computed on insert+update
[DatabaseGenerated(DatabaseGeneratedOption.None)]     // you always supply the value
```

Composite keys — attributes can't do this, Fluent only:
```csharp
modelBuilder.Entity<Game>().HasKey(g => new { g.Id, g.Region });
```

> SQLite allows exactly ONE autoincrement column and it must be the integer PK.

## 2. Columns

```csharp
[Table("Games")]                      // table name override
[Column("game_name")]                  // column rename
[Column(TypeName = "decimal(10,2)")]   // exact DB type (money!)
[MaxLength(256)]                       // VARCHAR(256), not unlimited TEXT
[NotMapped]                            // property ignored by EF
```

Example — your game (current FK design):
```csharp
public class Game
{
    public int Id { get; set; }                    // PK + autoincrement, by convention

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public int GenreId { get; set; }               // FK column → Genres.Id
    public Genre Genre { get; set; } = null!;      // navigation (object view of same FK)

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public DateOnly ReleaseDate { get; set; }
}
```

## 3. Relationships

```csharp
// one-to-many: one Publisher has many Games.
public class Game
{
    public int Id { get; set; }
    public int PublisherId { get; set; }              // FK column (convention: <Nav>Id)

    [ForeignKey(nameof(PublisherId))]
    public Publisher Publisher { get; set; } = null!; // navigation
}
public class Publisher
{
    public int Id { get; set; }
    public List<Game> Games { get; set; } = [];       // inverse navigation
}
```

## 4. `OnModelCreating` — what attributes CAN'T do

```csharp
protected override void OnModelCreating(ModelBuilder m)
{
    m.Entity<Game>().HasIndex(g => g.Name);                 // index
    m.Entity<Game>().HasIndex(g => g.Sku).IsUnique();       // unique index
    m.Entity<Game>().Property(g => g.Price).HasDefaultValue(0); // DB default
    m.Entity<Game>().Property(g => g.Name).IsRequired()     // NOT NULL (no [Required] needed)
        .HasMaxLength(256);
}
```

> Prefer Fluent for schema rules (indexes, defaults, relationships) — keeps `Game.cs` clean.

## 5. Wiring + Migrations

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Games")));
```

```zsh
dotnet ef migrations add InitialCreate --output-dir Data/Migrations  # scaffold
dotnet ef database update                                             # apply → games.db
```

Scaffolded `Id` line for SQLite (all inferred, zero config):
```csharp
Id = table.Column<int>(type: "INTEGER", nullable: false)
    .Annotation("Sqlite:Autoincrement", true),
// ...
table.PrimaryKey("PK_Games", x => x.Id);
```

## 6. Model vs DTO — where things go

| Concern | Model (`Game`) | DTO (`CreateGameDto`) |
|---|---|---|
| PK, autoincrement | `[Key]`, `Identity` | never |
| Column size/type | `[MaxLength]`, `[Column]` | never |
| Input required? | never | `[Required]` |
| Length/range rules | never | `[StringLength]`, `[Range]` |
| Relationships | nav props, `[ForeignKey]` | flatten to `PublisherId` / nested DTO |
