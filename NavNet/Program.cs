using Microsoft.EntityFrameworkCore;
using NavNet.Data;
using NavNet.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddValidation();
builder.Services.AddDbContext<NavNet.Data.AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Games")));

var app = builder.Build();

app.MapControllers();

app.MapGamesEndpoints();

app.MapGet("/", () => "Hello World!");
app.MigrateDb();

app.Run();