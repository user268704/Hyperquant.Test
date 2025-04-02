using Hyperquant.Abstraction.Exchanges;
using Hyperquant.Dto.Dto.UpdateContract;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using Bybit.Net.Objects.Models.V5;
using Hyperquant.Bybit.Extensions;

namespace Hyperquant.Bybit.Services;

public class BybitFuturesUpdate : IExchangeFuturesUpdate, IHostedService
{
    private readonly ILogger<BybitFuturesUpdate> _logger;
    private readonly IBybitRestClient _restClient;

    public BybitFuturesUpdate(ILogger<BybitFuturesUpdate> logger, IBybitRestClient restClient)
    {
        _logger = logger;
        _restClient = restClient;
    }

    public async Task<UpdateContractResult> UpdateContract(InitializeUpdateDto updateDto)
    {
        try
        {
            if (!Enum.TryParse<KlineInterval>(updateDto.Interval, out var interval))
                interval = KlineInterval.OneHour;

            _logger.LogInformation("Start updating contract for {FuturesFirst} and {FuturesSecond} from {From} to {To}",
                updateDto.FuturesFirst, updateDto.FuturesSecond, updateDto.From, updateDto.To);
            
            var firstFutures = await GetKlinesAsync(updateDto.FuturesFirst, interval, updateDto.From, updateDto.To);
            var secondFutures = await GetKlinesAsync(updateDto.FuturesSecond, interval, updateDto.From, updateDto.To);

            if (!firstFutures.Any() || !secondFutures.Any())
            {
                _logger.LogWarning("No data received for futures. First: {CountFirst}, Second: {CountSecond}", 
                    firstFutures.Count, secondFutures.Count);
                throw new Exception("No data received for futures");
            }

            _logger.LogInformation(
                "Got {CountFirst} klines for {FuturesFirst} and {CountSecond} klines for {FuturesSecond}",
                firstFutures.Count, updateDto.FuturesFirst, secondFutures.Count, updateDto.FuturesSecond);

            _logger.LogInformation("Calculating difference between {FuturesFirst} and {FuturesSecond}",
                updateDto.FuturesFirst, updateDto.FuturesSecond);
            
            var differences = CalculateDifference(firstFutures, secondFutures);

            _logger.LogInformation("Calculated {Count} differences",
                differences.Count);

            return new UpdateContractResult
            {
                Difference = differences,
                To = updateDto.To,
                From = updateDto.From,
                FuturesFirst = updateDto.FuturesFirst,
                FuturesSecond = updateDto.FuturesSecond
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract: {Message}", ex.Message);
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private List<Difference> CalculateDifference(List<BybitKline> firstFutures, List<BybitKline> secondFutures)
    {
        var result = new List<Difference>();

        firstFutures.ForEach(x => x.StartTime = x.StartTime.Truncate());
        firstFutures =  firstFutures.OrderBy(f => f.StartTime).ToList();

        secondFutures.ForEach(x => x.StartTime = x.StartTime.Truncate());
        secondFutures = secondFutures.OrderBy(s => s.StartTime.Truncate()).ToList();

        var allTimePoints = firstFutures.Select(f => f.StartTime.Truncate())
            .Union(secondFutures.Select(s => s.StartTime.Truncate()))
            .OrderBy(t => t)
            .ToList();
    
        decimal? lastFirstPrice = null;
        decimal? lastSecondPrice = null;
    
        foreach (var timePoint in allTimePoints)
        {
            var firstKline = firstFutures.FirstOrDefault(f => f.StartTime.Truncate() == timePoint);
            var secondKline = secondFutures.FirstOrDefault(s => s.StartTime.Truncate() == timePoint);
        
            if (firstKline != null)
            {
                lastFirstPrice = firstKline.ClosePrice;
            }
        
            if (secondKline != null)
            {
                lastSecondPrice = secondKline.ClosePrice;
            }
        
            if (lastFirstPrice.HasValue && lastSecondPrice.HasValue)
            {
                var difference = new Difference
                {
                    DateTime = timePoint,
                    DifferencePrice = lastFirstPrice.Value - lastSecondPrice.Value
                };
            
                result.Add(difference);
            }
        }
    
        return result;
    }

    private async Task<List<BybitKline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime from, DateTime to)
    {
        try
        {
            var data = await _restClient.V5Api.ExchangeData
                .GetKlinesAsync(category: Category.Linear,
                    symbol: symbol,
                    interval: interval,
                    startTime: from,
                    endTime: to);

            if (!data.Success)
            {
                _logger.LogError("Error getting klines: {Error}", data.Error);
                throw new Exception($"Failed to get klines for {symbol}: {data.Error}");
            }

            return data.Data.List.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting klines for {Symbol}: {Message}", symbol, ex.Message);
            throw;
        }
    }
}