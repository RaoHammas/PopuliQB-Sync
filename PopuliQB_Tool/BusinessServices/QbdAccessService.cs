using NLog;
using QBFC16Lib;
using QBXMLRP2Lib;

namespace PopuliQB_Tool.BusinessServices;

public class QbdAccessService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public QBSessionManager SessionManager;
    private readonly RequestProcessor2 _qbXmlProc;

    private const string AppId = "PopuliToQbSync";
    private const string AppName = "PopuliToQbSync";
    public bool IsConnected { get; private set; }

    public QbdAccessService()
    {
        SessionManager = new QBSessionManager();
        _qbXmlProc = new RequestProcessor2();
    }

    public bool OpenConnection()
    {
        try
        {
            if (IsConnected)
            {
                return true;
            }

            SessionManager.OpenConnection(AppId, AppName);
            _qbXmlProc.OpenConnection(AppId, AppName);
            IsConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            SessionManager.EndSession();
            IsConnected = false;
            _logger.Error(ex);
            return false;
        }
    }

    public bool? CloseConnection()
    {
        try
        {
            if (!IsConnected)
            {
                return true;
            }

            SessionManager.CloseConnection();
            _qbXmlProc.CloseConnection();
            return true;
        }
        catch (Exception ex)
        {
            SessionManager.EndSession();
            _logger.Error(ex);
            return false;
        }
        finally
        {
            IsConnected = false;
        }
    }

    public string? GetCompanyName()
    {
        try
        {
            SessionManager.BeginSession("", ENOpenMode.omDontCare);

            var compName = SessionManager.GetCurrentCompanyFileName();
            return compName;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return "";
        }
        finally
        {
            SessionManager.EndSession();
        }
    }

}