# C# DataAnnotations Validators — Cheat Sheet (for `NavNet/Dtos`)

Mapped from NestJS `class-validator` → ASP.NET `System.ComponentModel.DataAnnotations`.
Works automatically with `[ApiController]` (auto-400). In manual `IResult` controllers you must call `Validator.TryValidateObject` yourself.

## 1. Strings

```csharp
[Required]                          // not null, not empty. = @IsNotEmpty()
[StringLength(50)]                  // max 50. = @MaxLength(50)
[StringLength(50, MinimumLength = 3)] // 3..50. = @Length(3, 50)
[MinLength(1)] [MaxLength(5)]       // for arrays/lists too
[RegularExpression(@"^[A-Z][a-z ]+$")] // regex. = @Matches()
[EmailAddress]                      // = @IsEmail()
[Phone]                             // = @IsPhoneNumber()
[Url]                               // = @IsUrl()
[AllowedValues("Action","RPG","FPS")]   // .NET 7+. = @IsIn([...])
[DeniedValues("", "unknown")]       // forbid specific values
```

Example — your game:
```csharp
public record CreateGameDto(
    [Required, StringLength(50, MinimumLength = 3)] string Name,
    [Required, Range(1, int.MaxValue)] int GenreId,
    ...
);
```

## 2. Numbers — `int`, `decimal`, `double`

`decimal` is for money (exact base-10). `double` is for science (binary, imprecise).

```csharp
[Required]                          // int/decimal can't be null unless int?/decimal?
[Range(0, 100)]                     // 0..100 inclusive. = @Min(0) @Max(100)
[Range(0.01, 999.99)]               // works for decimal too
[Range(typeof(decimal), "0", "100")] // explicit decimal range (avoids double conversion)
[DeniedValues(0)]                    // price can't be 0, but can be negative (rare)
[AllowedValues(0, 9.99, 19.99, 59.99)] // only these tiers
[DataType(DataType.Currency)]       // hint for Swagger/UI, NOT validation
```

Example — your game:
```csharp
public record CreateGameDto(
    ...
    [Range(0, 100, ErrorMessage = "Price must be 0-100")] decimal Price,
    [Range(1, 10)] int MaxPlayers,
    ...
);
```

> Tip: `[Range]` on `decimal` uses `double` under the hood. For strict money rules, combine with custom check or keep `decimal` + `[Range(typeof(decimal), "0", "100")]`.

## 3. Dates — `DateOnly`, `DateTime`

```csharp
[Required]                          // = @IsDate()
[DataType(DataType.Date)]           // display hint only
[Range(typeof(DateOnly), "1962-10-19", "2026-12-31")] // min..max date
```

Custom past-only example:
```csharp
public class PastDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx) =>
        value is DateOnly d && d > DateOnly.FromDateTime(DateTime.Today)
            ? new ValidationResult("ReleaseDate cannot be in the future")
            : ValidationResult.Success;
}

// usage:
[Required, PastDate] DateOnly ReleaseDate
```

## 4. Collections / Nested DTOs

```csharp
[MinLength(1)] List<string> Tags    // at least 1 tag
[MaxLength(5)] List<string> Tags    // at most 5
[Required] PublisherDto Publisher   // nested object — MVC validates recursively automatically
```

```csharp
public record PublisherDto(
    [Required, StringLength(50, MinimumLength = 2)] string Name,
    [Required, StringLength(2, MinimumLength = 2)] string CountryCode
);
```

> Manual `IResult` controllers do NOT recurse — you must `Validator.TryValidateObject` the nested object yourself.

## 5. Compare / Confirm + Credit Card

```csharp
[Compare("Password")] string ConfirmPassword // = @Equals field
[CreditCard] string CardNumber
```

## 6. Full Game Example

```csharp
using System.ComponentModel.DataAnnotations;

public record CreateGameDto(
    [Required, StringLength(50, MinimumLength = 3)] string Name,
    [Required, Range(1, int.MaxValue)] int GenreId,
    [Range(typeof(decimal), "0", "100")] decimal Price,
    [Required] DateOnly ReleaseDate,
    [MinLength(1), MaxLength(5)] List<string> Tags,
    [Required] PublisherDto Publisher
);
```

Bad POST returns (MVC only, automatic):
```json
// 400 Bad Request
{
  "title": "One or more validation errors occurred.",
  "errors": {
    "Name": ["The field Name must be a string with a minimum length of 3."],
    "Price": ["The field Price must be between 0 and 100."],
    "Publisher.Name": ["The Name field is required."]
  }
}
```
