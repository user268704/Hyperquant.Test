namespace Hyperquant.Dto.Dto.UpdateContract;

public class UpdateContractResult
{
    public string FuturesFirst { get; set; }
    public string FuturesSecond { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<Difference> Difference { get; set; }
}
 