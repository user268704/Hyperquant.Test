using System.Diagnostics;
using Hyperquant.Abstraction;
using Hyperquant.Abstraction.Rabbit;
using Hyperquant.Dto.Dto.UpdateContract;
using Quartz;
using RabbitMQ.Client;

namespace Hyperquant.Jobs.Jobs;

public class FuturesUpdateJob : IJob
{
    private readonly IRabbitClient _rabbitClient;
    ActivitySource _activitySource = new(ActivitySourceName);

    public const string ActivitySourceName = "Hyperquant.Jobs";
    const string ActivityName = "Start job update";

    
    
    public FuturesUpdateJob(IRabbitClient rabbitClient)
    {
        _rabbitClient = rabbitClient;
    }

    public Task Execute(IJobExecutionContext context)
    {
        using var internalActivity = _activitySource.StartActivity(ActivityName, ActivityKind.Client);
        
        var isDataExist = context.JobDetail.JobDataMap.TryGetValue("initializeData", out var initializeData);
        
        if (!isDataExist)
        {
            internalActivity.SetStatus(ActivityStatusCode.Error, "Data for job not set");
            
            return Task.CompletedTask;
        }

        internalActivity.SetStatus(ActivityStatusCode.Ok);
        /*
        var message = new InitializeUpdateDto
        {
            From = initial  izeData,
            To = to,
            FuturesFirst = "BTCUSDT-18APR25",
            FuturesSecond = "BTCUSDT-30MAY25",
            Interval = "OneHour"
        };
        */

        return _rabbitClient.PublishAsync(initializeData, "stock_exchange_upload", "update");
    }
}