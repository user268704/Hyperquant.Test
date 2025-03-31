using Hyperquant.Dto.Dto.UpdateContract;

namespace Hyperquant.Abstraction.Exchanges;

public interface IExchangeFuturesUpdate
{
    public Task<UpdateContractResult> UpdateContract(InitializeUpdateDto updateDto);
}