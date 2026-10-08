using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NavNet.Data;
using NavNet.Endpoints;
using NavNet.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddValidation();
builder.Services.AddDbContext<NavNet.Data.AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Games")));

builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Missing Jwt:Key. Set it via User Secrets or env.");
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGamesEndpoints();
app.MapGenreEndpoints();
app.MapAuthEndpoints();

app.MapGet("/", () => "Hello World!");
app.MigrateDb();

app.Run();