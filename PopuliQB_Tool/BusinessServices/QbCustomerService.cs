using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using NLog;
using PopuliQB_Tool.EventArgs;

namespace PopuliQB_Tool.BusinessServices;

public class QbCustomerService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopPersonToQbCustomerBuilder _customerBuilder;

    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }
    public EventHandler<PopToQbCustomerImportArgs>? OnProgressChanged { get; set; }
    private const string AppId = "PopuliToQbSync";
    private const string AppName = "PopuliToQbSync";

    public QbCustomerService(PopPersonToQbCustomerBuilder customerBuilder)
    {
        _customerBuilder = customerBuilder;
    }

    public async Task<bool> AddCustomersAsync(List<PopPerson> persons)
    {
        var sessionManager = new QBSessionManager();

        try
        {
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            //Connect to QuickBooks and begin a session
            sessionManager.OpenConnection(AppId, AppName);
            IsConnected = true;
            OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Connected to QB.", null));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Session Started.", null));

            await Task.Run(() =>
            {
                foreach (var person in persons)
                {
                    _customerBuilder.BuildCustomerAddRequest(requestMsgSet, person);
                    var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                    OnProgressChanged?.Invoke(this,
                        new PopToQbCustomerImportArgs(responseMsgSet.Attributes.MessageSetStatusCode, person));
                }
            });

            OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Completed.", null));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Error: ", ex));
            return false;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Session Ended.", null));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnProgressChanged?.Invoke(this, new PopToQbCustomerImportArgs("Disconnected.", null));
            }
        }
    }
}