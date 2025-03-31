#region

using Hyperquant.Abstraction.Repository;
using Hyperquant.Data.Contexts;
using Hyperquant.Dto.Dto.UpdateContract;
using Hyperquant.Models.Repository;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Hyperquant.Repository.Services;

public class UpdateFuturesRepository : IUpdateFuturesRepository
{
    private readonly PostgresContext _context;
    private readonly ILogger<UpdateFuturesRepository> _logger;

    public UpdateFuturesRepository(PostgresContext context, ILogger<UpdateFuturesRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task UpdateFuturesAsync(UpdateContractResult updateContractResult)
    {
        if (updateContractResult == null)
        {
            throw new ArgumentNullException(nameof(updateContractResult));
        }

        if (string.IsNullOrEmpty(updateContractResult.FuturesFirst) || string.IsNullOrEmpty(updateContractResult.FuturesSecond))
        {
            throw new ArgumentException("Futures names cannot be empty");
        }

        if (!updateContractResult.Difference.Any())
        {
            _logger.LogWarning("No differences to update for futures {FuturesFirst} and {FuturesSecond}", 
                updateContractResult.FuturesFirst, updateContractResult.FuturesSecond);
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var futuresUpdates = updateContractResult.Difference.Select(x => new FuturesDifference
                {
                    Difference = x.DifferencePrice,
                    DateTime = x.DateTime,
                    FuturesFirst = updateContractResult.FuturesFirst,
                    FuturesSecond = updateContractResult.FuturesSecond,
                });
            
                await _context.FuturesUpdates.AddRangeAsync(futuresUpdates);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully updated {Count} futures differences", futuresUpdates.Count());
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating futures differences: {Message}", ex.Message);
                throw;
            }
        });
    }
}