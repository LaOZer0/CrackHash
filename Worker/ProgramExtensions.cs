using Worker.Services;

namespace Worker;

public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IHashCracker, HashCracker>();
        builder.Services.AddScoped<IManagerClient, ManagerClient>();
        return builder;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapControllers();
        return app;
    }
}