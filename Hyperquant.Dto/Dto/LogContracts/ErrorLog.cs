namespace Hyperquant.Dto.Dto.LogContracts;

public class ErrorLog
{
    public string Message { get; set; }
    public DateTime TimeStamp { get; set; }
    public string StackTrace { get; set; }
}