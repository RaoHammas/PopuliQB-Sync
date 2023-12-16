using PopuliQB_Tool.Models;

namespace PopuliQB_Tool.EventArgs;

public class PopToQbCustomerImportArgs : System.EventArgs
{
    public PopToQbCustomerImportArgs(StatusMessageType status, object? data)
    {
        Status = status;
        Data = data;
    }

    public StatusMessageType Status { get; set; }
    public object? Data { get; set; }
}