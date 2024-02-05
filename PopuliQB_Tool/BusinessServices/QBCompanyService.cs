using NLog;
using QBFC16Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QBCompanyService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public static string AppId => "PopuliToQbSync";
    public static string AppName => "PopuliToQbSync";
    public static string CompanyName { get; private set; } = "";
    public static string CompanyFileName { get; private set; } = "";

    public QBCompanyService()
    {
    }

    public string GetCompanyName()
    {
        var sessionManager = new QBSessionManager();
        try
        {
            sessionManager.OpenConnection2(AppId, AppName, ENConnectionType.ctLocalQBD);

            sessionManager.BeginSession(QBCompanyService.CompanyFileName, ENOpenMode.omDontCare);
            CompanyName = sessionManager.GetCurrentCompanyFileName();
            CompanyFileName = sessionManager.GetCurrentCompanyFileName();

            try
            {
                CompanyName = CompanyName.Split("\\").Last().Split('.')[0];
            }
            catch (Exception)
            {
                //ignore
            }

            return CompanyName;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            throw;
        }
        finally
        {
            sessionManager.EndSession();
            sessionManager.CloseConnection();
        }
    }
}