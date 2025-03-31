using System.Diagnostics;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Aspire.ServiceDefaults;
using Hyperquant.Dto.Dto.UpdateContract;
using Hyperquant.Jobs.Jobs;
using Hyperquant.Jobs.Services;
using Quartz;
using RabbitMQ.Client;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.AddRabbitMQClient("bus");
builder.AddServiceDefaults();

ILogger loggerConfiguration = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.WithProperty("app", "Hyperquant.Jobs")
    .CreateLogger();

string? jobFrom = builder.Configuration["JobData:From"];

if (string.IsNullOrEmpty(jobFrom) || !DateTime.TryParse(jobFrom, out DateTime jobFromDate))
{
    jobFromDate = DateTime.Now.AddDays(-7);
}

string? jobTo = builder.Configuration["JobData:To"];

if (string.IsNullOrEmpty(jobTo) || !DateTime.TryParse(jobFrom, out DateTime jobToDate))
{
    jobToDate = DateTime.Now;
}

string? jobFuturesFirst = builder.Configuration["JobData:FuturesFirst"];
string? jobFuturesSecond = builder.Configuration["JobData:FuturesSecond"];

if (string.IsNullOrEmpty(jobFuturesFirst) || string.IsNullOrEmpty(jobFuturesSecond))
{
    string errorText = "Job data is not set. Please set valid JobData:FuturesFirst and JobData:FuturesSecond in the configuration.";
    
    loggerConfiguration.Fatal(errorText);
    
    return;
}

string? jobInterval = builder.Configuration["JobData:Interval"];

int jobStartIntervalInMinutes =
    int.TryParse(builder.Configuration["JobStartConfig:StartIntervalInMinutes"], out jobStartIntervalInMinutes)
        ? jobStartIntervalInMinutes
        : 60;

builder.Services.AddQuartz(config =>
{
    JobKey jobKey = new JobKey("FuturesUpdateJob", "Updates");

    config.AddJob<FuturesUpdateJob>(options =>
    {
        options.WithIdentity(jobKey)
            .SetJobData(new JobDataMap
            {
                {
                    "initializeData", new InitializeUpdateDto
                    {
                        From = jobFromDate,
                        To = jobToDate,
                        FuturesFirst = jobFuturesFirst,
                        FuturesSecond = jobFuturesSecond,
                        Interval = jobInterval ?? "OneHour"
                    }
                },
            });
    });

    config.AddTrigger(trigger =>
    {
        trigger.WithIdentity("FuturesUpdateJobTrigger", "Updates")
            .ForJob(jobKey)
            .WithSimpleSchedule(scheduleBuilder =>
            {
                scheduleBuilder
                    .WithIntervalInMinutes(jobStartIntervalInMinutes)
                    .RepeatForever();
            })
            .StartAt(DateTimeOffset.Now.AddMinutes(jobStartIntervalInMinutes));
    });

});

builder.Services.AddSingleton<RabbitClientHostedService>();
builder.Services.AddHostedService<RabbitClientHostedService>(x => x.GetRequiredService<RabbitClientHostedService>());
builder.Services.AddSingleton<IChannel>(x =>
{
    var clientHostedService = x.GetRequiredService<RabbitClientHostedService>();

    return clientHostedService.Channel;
});

builder.Services.AddSingleton<IRabbitClient, RabbitClient>();

builder.Services.AddQuartzHostedService();


var app = builder.Build();

app.Run();