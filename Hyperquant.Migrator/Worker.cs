using System.Diagnostics;
using Hyperquant.Data.Contexts;
using Hyperquant.Models.Repository;
using Hyperquant.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Hyperquant.Migrator;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;

    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);


    public Worker(IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PostgresContext>();

            _logger.LogInformation("Start migration for {DbContext}", dbContext.GetType().Name);
            await RunMigrationAsync(dbContext, stoppingToken);
            _logger.LogInformation("Migration completed for {DbContext}", dbContext.GetType().Name);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private async Task RunMigrationAsync(PostgresContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }
}