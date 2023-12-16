namespace PopuliQB_Tool.EventArgs;

public class PopToQbCustomerImportArgs : System.EventArgs
{
    public PopToQbCustomerImportArgs(string status, object? data)
    {
        Status = status;
        Data = data;
    }

    public string Status { get; set; }
    public object? Data { get; set; }
}