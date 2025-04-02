using System.Net;
using Hyperquant.Dto.Dto.UpdateContract;
using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Hyperquant.Abstraction.Exchanges;
using Hyperquant.Bybit.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Hyperquant.Bybit.Tests.Services;

[TestFixture]
public class BybitFuturesUpdateTests
{
    private IExchangeFuturesUpdate _service;

    
    [SetUp]
    public void Setup()
    {
        var serviceBuilder = new ServiceCollection(); 
        serviceBuilder.AddLogging();
        
        serviceBuilder.AddSingleton<IExchangeFuturesUpdate, BybitFuturesUpdate>();
        serviceBuilder.AddHostedService<BybitFuturesUpdate>(x =>
            (BybitFuturesUpdate)x.GetRequiredService<IExchangeFuturesUpdate>());

        serviceBuilder.AddBybit((options) =>
        {
            options.ApiCredentials = new ApiCredentials("test", "test");
        });

        var provider = serviceBuilder.BuildServiceProvider();
        
        _service = provider.GetRequiredService<IExchangeFuturesUpdate>();
    }

    [Test]
    public async Task UpdateContract_WithValidData_ReturnsCorrectDifferences()
    {
        // Arrange
        var updateDto = new InitializeUpdateDto
        {
            FuturesFirst = "BTCUSDT",
            FuturesSecond = "ETHUSDT",
            From = DateTime.UtcNow.AddHours(-2),
            To = DateTime.UtcNow,
            Interval = "OneHour"
        };
        
        // Act
        var result = await _service.UpdateContract(updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Difference, Is.Not.Null);
        Assert.That(result.Difference, Has.Count.GreaterThan(0));
        Assert.That(result.FuturesFirst, Is.EqualTo(updateDto.FuturesFirst));
        Assert.That(result.FuturesSecond, Is.EqualTo(updateDto.FuturesSecond));
        Assert.That(result.From, Is.EqualTo(updateDto.From));
        Assert.That(result.To, Is.EqualTo(updateDto.To));
    }

    [Test]
    public Task UpdateContract_WithEmptyData_ThrowsException()
    {
        // Arrange
        var updateDto = new InitializeUpdateDto
        {
            FuturesFirst = "",
            FuturesSecond = "",
            From = DateTime.UtcNow.AddHours(-2),
            To = DateTime.UtcNow,
            Interval = "1h"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await _service.UpdateContract(updateDto));
        
        Assert.That(ex.Message, Does.Contain("Failed to get klines"));
        
        return Task.CompletedTask;
    }

    [Test]
    public async Task UpdateContract_WithInvalidInterval_DefaultsToOneHour()
    {
        // Arrange
        var updateDto = new InitializeUpdateDto
        {
            FuturesFirst = "BTCUSDT",
            FuturesSecond = "ETHUSDT",
            From = DateTime.UtcNow.AddHours(-2),
            To = DateTime.UtcNow,
            Interval = "invalid"
        };

        // Act
        var result = await _service.UpdateContract(updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Difference, Is.Not.Null.And.Not.Empty);

        for (int i = 1; i < result.Difference.Count; i++)
        {
            var prev = result.Difference[i - 1];
            var curr = result.Difference[i];

            Assert.That((curr.DateTime - prev.DateTime).TotalHours, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task UpdateContract_WithValidData_CalculatesDifferencesCorrectly()
    {
        // Arrange
        var updateDto = new InitializeUpdateDto
        {
            FuturesFirst = "BTCUSDT",
            FuturesSecond = "ETHUSDT",
            From = DateTime.UtcNow.AddHours(-2),
            To = DateTime.UtcNow,
            Interval = "OneHour"
        };
        
        var bybitClient = new Mock<IBybitRestClient>();
        
        var firstFutures = new List<BybitKline>
        {
            new() { StartTime = DateTime.UtcNow.AddHours(-2), ClosePrice = 50000 },
            new() { StartTime = DateTime.UtcNow.AddHours(-1), ClosePrice = 51000 },
            new() { StartTime = DateTime.UtcNow, ClosePrice = 52000 }
        };

        var secondFutures = new List<BybitKline>
        {
            new() { StartTime = DateTime.UtcNow.AddHours(-2), ClosePrice = 50100 },
            new() { StartTime = DateTime.UtcNow.AddHours(-1), ClosePrice = 51100 },
            new() { StartTime = DateTime.UtcNow, ClosePrice = 52100 }
        };

        var mockResponse1 = new WebCallResult<BybitResponse<BybitKline>>(HttpStatusCode.OK, null, TimeSpan.Zero, 0,
            null, 0,
            null, null, null, null, ResultDataSource.Server, new BybitResponse<BybitKline> { List = firstFutures },
            null);

        var mockResponse2 = new WebCallResult<BybitResponse<BybitKline>>(HttpStatusCode.OK, null, TimeSpan.Zero, 0,
            null, 0,
            null, null, null, null, ResultDataSource.Server, new BybitResponse<BybitKline> { List = secondFutures },
            null);

        bybitClient.SetupSequence(x => x.V5Api.ExchangeData.GetKlinesAsync(
                It.IsAny<Category>(),
                It.IsAny<string>(),
                It.IsAny<KlineInterval>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse1)
            .ReturnsAsync(mockResponse2);
        
        
        var builder = new ServiceCollection();
        
        builder.AddLogging();
        builder.AddSingleton<IBybitRestClient>(bybitClient.Object);
        builder.AddSingleton<IExchangeFuturesUpdate, BybitFuturesUpdate>(x =>
            new BybitFuturesUpdate(x.GetRequiredService<ILogger<BybitFuturesUpdate>>(),
                x.GetRequiredService<IBybitRestClient>()));
        
        var provider = builder.BuildServiceProvider();
        var service = provider.GetRequiredService<IExchangeFuturesUpdate>();
        
        // Act
        var result = await service.UpdateContract(updateDto);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Difference, Has.Count.EqualTo(3));
        Assert.That(result.FuturesFirst, Is.EqualTo(updateDto.FuturesFirst));
        Assert.That(result.FuturesSecond, Is.EqualTo(updateDto.FuturesSecond));
        Assert.That(result.From, Is.EqualTo(updateDto.From));
        Assert.That(result.To, Is.EqualTo(updateDto.To));

        var differences = result.Difference.OrderBy(x => x.DateTime).ToList();
        
        Assert.That(differences[0].DifferencePrice, Is.EqualTo(-100m));
        Assert.That(differences[1].DifferencePrice, Is.EqualTo(-100m));
        Assert.That(differences[2].DifferencePrice, Is.EqualTo(-100m));
    }

    [Test]
    public async Task UpdateContract_WithMissingData_ThrowsException()
    {
        // Arrange
        var updateDto = new InitializeUpdateDto
        {
            FuturesFirst = "BTCUSDT",
            FuturesSecond = "ETHUSDT",
            From = DateTime.UtcNow.AddHours(-2),
            To = DateTime.UtcNow,
            Interval = "OneHour"
        };

        var emptyResult = new WebCallResult<BybitResponse<BybitKline>>(HttpStatusCode.OK, null, TimeSpan.Zero,
            0,
            "",
            0,
            "",
            "",
            HttpMethod.Get,
            null,
            ResultDataSource.Server,
            new BybitResponse<BybitKline> { List = new List<BybitKline>() }, null);
        
        var bybitClient = new Mock<IBybitRestClient>();
        bybitClient.Setup(x => x.V5Api.ExchangeData.GetKlinesAsync(
                It.IsAny<Category>(),
                It.IsAny<string>(),
                It.IsAny<KlineInterval>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResult);
        
        var builder = new ServiceCollection();
        
        builder.AddLogging();
        builder.AddSingleton<IBybitRestClient>(bybitClient.Object);
        builder.AddSingleton<IExchangeFuturesUpdate, BybitFuturesUpdate>(x =>
            new BybitFuturesUpdate(x.GetRequiredService<ILogger<BybitFuturesUpdate>>(),
                x.GetRequiredService<IBybitRestClient>()));
        
        var provider = builder.BuildServiceProvider();
        var service = provider.GetRequiredService<IExchangeFuturesUpdate>();
        
        // Act & Assert
        var ex = Assert.ThrowsAsync<Exception>(async () => await service.UpdateContract(updateDto));
        
        Assert.That(ex.Message, Is.EqualTo("No data received for futures"));
    }
} 