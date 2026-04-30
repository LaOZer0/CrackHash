namespace Worker;

public static class Program
{
    public static async Task Main(string[] args) =>
        await WebApplication.CreateBuilder(args)
            .ConfigureServices()
            .Build()
            .ConfigurePipeline()
            .RunAsync();
}