using NLog;
using QBFC16Lib;
using QBXMLRP2Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QBCompanyService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private const string AppId = "PopuliToQbSync";
    private const string AppName = "PopuliToQbSync";

    public QBCompanyService()
    {
    }

    public string GetCompanyName()
    {
        var sessionManager = new QBSessionManager();
        try
        {
            sessionManager.OpenConnection(AppId, AppName);

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            var compName = sessionManager.GetCurrentCompanyFileName();
            try
            {
                compName = compName.Split("\\").Last().Split('.')[0];
            }
            catch (Exception)
            {
                //ignore
            }

            return compName;
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