using System.Threading.Channels;
using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.Data.Contexts;
using Hyperquant.Migrator;
using Hyperquant.Data.Migrations;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

builder.AddNpgsqlDbContext<PostgresContext>(connectionName: "hyperquant", 
    configureDbContextOptions: options => 
    {
        var migrationsAssembly = typeof(Initial).Assembly.GetName().Name;
        options.UseNpgsql(o => o.MigrationsAssembly(migrationsAssembly));
    });

var host = builder.Build();
host.Run();