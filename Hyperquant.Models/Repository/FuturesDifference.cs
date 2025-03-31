namespace Hyperquant.Models.Repository;

public class FuturesDifference
{
    public Guid Id { get; set; }
    public string FuturesFirst { get; set; }
    public string FuturesSecond { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Difference { get; set; }
}