using Hyperquant.Dto.Dto.UpdateContract;

namespace Hyperquant.Abstraction.Repository;

public interface IUpdateFuturesRepository
{
    public Task UpdateFuturesAsync(UpdateContractResult updateContractResult);
}