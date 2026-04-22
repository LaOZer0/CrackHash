using Worker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IHashCracker, HashCracker>();
builder.Services.AddScoped<IManagerClient, ManagerClient>();

var app = builder.Build();

app.MapControllers();

Console.WriteLine("Worker started. Press Ctrl+C to stop.");
app.Run();