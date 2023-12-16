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
    public ProgressArgs(int progressValue)
    {
        ProgressValue = progressValue;
    }

    public int ProgressValue { get; set; }
}