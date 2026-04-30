namespace Manager;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = WebApplication.CreateBuilder(args)
            .ConfigureServices()
            .Build();
        
        await app.ConfigurePipeline().RunAsync();
    }
}