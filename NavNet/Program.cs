using NavNet.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<NavNet.Data.AppDbContext>(opt =>
    opt.UseSqlite("Data Source=games.db"));

var app = builder.Build();

app.MapControllers();

app.MapGamesEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();