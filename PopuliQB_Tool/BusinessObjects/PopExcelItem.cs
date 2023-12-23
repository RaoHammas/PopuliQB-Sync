namespace PopuliQB_Tool.BusinessObjects;

public class PopExcelItem
{
    public string Name { get; set; }
    public string Account { get; set; }
    public string QbAccListId { get; set; }

    public string AccNumberOnly => Account.Trim().Split("·")[0].Trim();
    public string AccTitleOnly
    {
        get
        {
            var sp = Account.Trim().Split("·");
            if (sp[1].Trim().StartsWith(":"))
            {
                var sp2 = sp[1].Split(":");
                return sp2[1].Trim();
            }
            else
            {
                return sp[1].Trim();
            }
        }
    }
}