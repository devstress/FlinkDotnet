using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FlinkDotNet.Aspire.IntegrationTests.Controllers;
using FlinkDotNet.Aspire.IntegrationTests.StepDefinitions;

namespace FlinkDotNet.Aspire.IntegrationTests;

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<IBatchProcessingService, BatchProcessingService>();
        services.AddLogging();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}