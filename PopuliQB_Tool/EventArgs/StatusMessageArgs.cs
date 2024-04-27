using PopuliQB_Tool.Models;

namespace PopuliQB_Tool.EventArgs;

public class StatusMessageArgs : System.EventArgs
{
    public StatusMessageArgs(StatusMessageType statusType, string message)
    {
        StatusType = statusType;
        Message = message;
    }

    public StatusMessageType StatusType { get; set; }
    public string Message { get; set; }
}

public class ProgressArgs : System.EventArgs
{
    public ProgressArgs(int progressValue, int? total = null)
    {
        ProgressValue = progressValue;
        Total = total;
    }

    public int ProgressValue { get; set; }
    public int? Total { get; set; }
}

public class ErrorMessage
{
    public Exception Ex { get; }
    public string Message { get; }

    public ErrorMessage(Exception ex, string message)
    {
        Ex = ex;
        Message = message;
    }
}