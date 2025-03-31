namespace Hyperquant.Dto.Dto.UpdateContract;

public class InitializeUpdateDto
{
    public required DateTime From { get; set; }
    public required DateTime To { get; set; }
    public required string FuturesFirst { get; set; }
    public required string FuturesSecond { get; set; }

    /// <summary>
    /// OneHour by default
    /// </summary>
    public string Interval { get; set; }
}