using QBFC16Lib;
using System.Windows;

namespace PopuliQB_Tool.BusinessServices;

public class QbCustomerService
{
    public QbCustomerService()
    {
    }

    /*public void AddCustomer()
    {
        var sessionBegun = false;
        var connectionOpen = false;
        QBSessionManager sessionManager = null;

        try
        {
            //Create the session Manager object?
            sessionManager = new QBSessionManager();

            //Create the message set request object? to hold our request
            var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            BuildCustomerAddRq(requestMsgSet);

            //Connect to QuickBooks and begin a session
            sessionManager.OpenConnection("", "Sample Code from OSR");
            connectionOpen = true;
            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            sessionBegun = true;

            //Send the request and get the response from QuickBooks
            var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

            //End the session and close the connection to QuickBooks
            sessionManager.EndSession();
            sessionBegun = false;
            sessionManager.CloseConnection();
            connectionOpen = false;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error");
            if (sessionBegun)
            {
                sessionManager.EndSession();
            }

            if (connectionOpen)
            {
                sessionManager.CloseConnection();
            }
        }
    }*/



}