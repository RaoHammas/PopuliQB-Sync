using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using QBFC16Lib;
using QBXMLRP2Lib;
using Populi;

namespace PopuliQB1
{
    internal class QBAccessor
    {
        public QBFC16Lib.QBSessionManager sessionManager;
        RequestProcessor2 qbXMLProc;
        private bool booSessionBegun;
        private bool closedConnection;

        public QBAccessor()
        {
            // Create the session manager object using QBFC
            sessionManager = new QBFC16Lib.QBSessionManager();
            qbXMLProc = new RequestProcessor2();
            // Open the connection and begin a session to QuickBooks
            OpenConnection();
            closedConnection = false;
        }

        ~QBAccessor()
        {
            CloseConnection();
            closedConnection = true;
        }

        public bool ClosedConnection()
        {
            return closedConnection;
        }

        public void OpenConnection()
        {
            sessionManager.OpenConnection("", "IDN InvoiceAdd C# sample");
            qbXMLProc.OpenConnection("", "IDN InvoiceAdd C# sample");
            closedConnection = false;
        }

        public void CloseConnection()
        {
            sessionManager.CloseConnection();
            qbXMLProc.CloseConnection();
            closedConnection = true;
        }

        public string GetCompanyName()
        {
            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
            }
            catch (Exception e)
            {
                sessionManager.EndSession();
                var mes = e.Message;
                throw e;
            }

            var compName = sessionManager.GetCurrentCompanyFileName();
            sessionManager.EndSession();
            return compName;
        }

        public Customer GetCustomerByListID(string ListID)
        {
            Customer customer = null;
            string ticket = null;

            try
            {
                ticket = qbXMLProc.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
                booSessionBegun = true;

                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 12, 0);

                //Step 3: Create the query object needed to perform CustomerQueryRq
                ICustomerQuery customerQuery = requestSet.AppendCustomerQueryRq();
                // get all customers - active and inactive
                //customerQuery.ORCustomerListQuery.CustomerListFilter.ActiveStatus.SetValue(ENActiveStatus.asAll);
                customerQuery.ORCustomerListQuery.ListIDList.Add(ListID);
                var xmlReq = requestSet.ToXMLString();

                var strOutXML = qbXMLProc.ProcessRequest(ticket, xmlReq);
                XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.LoadXml(strOutXML);
                // Get elements
                XmlNodeList listIDnd = xmlDoc.GetElementsByTagName("CustomerRet");
                customer = new Customer();
                for (int i = 0; i < listIDnd.Count; i++)
                {
                    string xml = listIDnd[i].InnerXml;
                    XmlNodeList listCont = listIDnd[i].ChildNodes;
                    foreach (XmlNode node in listCont)
                    {
                        switch (node.Name)
                        {
                            case "ListID":
                                customer.ListID = node.InnerText;
                                break;
                            case "EditSequence":
                                customer.EditSeq = node.InnerText;
                                break;
                            case "Name":
                                customer.Name = node.InnerText;
                                break;
                            case "FullName":
                                customer.FullName = node.InnerText;
                                break;
                            case "CompanyName":
                                customer.CompanyName = node.InnerText;
                                break;
                            case "Email":
                                customer.Email = node.InnerText;
                                break;
                            case "Phone":
                                customer.Phone = node.InnerText;
                                break;
                            case "BillAddress":
                                XmlNodeList billAddrNodes = node.ChildNodes;
                                foreach (XmlNode baNode in billAddrNodes)
                                {
                                    switch (baNode.Name)
                                    {
                                        case "Addr1":
                                            customer.BillAddr.Addr1 = baNode.InnerText;
                                            break;
                                        case "Addr2":
                                            customer.BillAddr.Addr2 = baNode.InnerText;
                                            break;
                                        case "City":
                                            customer.BillAddr.City = baNode.InnerText;
                                            break;
                                        case "Country":
                                            customer.BillAddr.Country = baNode.InnerText;
                                            break;
                                        case "State":
                                            customer.BillAddr.State = baNode.InnerText;
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }

                booSessionBegun = false;
                qbXMLProc.EndSession(ticket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                {
                    qbXMLProc.EndSession(ticket);
                }
            }

            return customer;
        }

        public List<Customer> GetCustomers()
        {
            List<Customer> customersLst = null;
            string ticket = null;

            try
            {
                ticket = qbXMLProc.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
                booSessionBegun = true;

                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 12, 0);

                //Step 3: Create the query object needed to perform CustomerQueryRq
                ICustomerQuery customerQuery = requestSet.AppendCustomerQueryRq();
                // get all customers - active and inactive
                customerQuery.ORCustomerListQuery.CustomerListFilter.ActiveStatus.SetValue(ENActiveStatus.asAll);
                //customerQuery.ORCustomerListQuery.FullNameList.Add("Test");
                var xmlReq = requestSet.ToXMLString();

                var strOutXML = qbXMLProc.ProcessRequest(ticket, xmlReq);
                customersLst = new List<Customer>();
                XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.LoadXml(strOutXML);
                // Get elements
                XmlNodeList listIDnd = xmlDoc.GetElementsByTagName("CustomerRet");
                for (int i = 0; i < listIDnd.Count; i++)
                {
                    string xml = listIDnd[i].InnerXml;
                    XmlNodeList listCont = listIDnd[i].ChildNodes;
                    Customer customer = new Customer();
                    foreach (XmlNode node in listCont)
                    {
                        switch (node.Name)
                        {
                            case "ListID":
                                customer.ListID = node.InnerText;
                                break;
                            case "EditSequence":
                                customer.EditSeq = node.InnerText;
                                break;
                            case "Name":
                                customer.Name = node.InnerText;
                                break;
                            case "FullName":
                                customer.FullName = node.InnerText;
                                break;
                            case "CompanyName":
                                customer.CompanyName = node.InnerText;
                                break;
                            case "Email":
                                customer.Email = node.InnerText;
                                break;
                            case "Phone":
                                customer.Phone = node.InnerText;
                                break;
                            case "BillAddress":
                                XmlNodeList billAddrNodes = node.ChildNodes;
                                foreach (XmlNode baNode in billAddrNodes)
                                {
                                    switch (baNode.Name)
                                    {
                                        case "Addr1":
                                            customer.BillAddr.Addr1 = baNode.InnerText;
                                            break;
                                        case "Addr2":
                                            customer.BillAddr.Addr2 = baNode.InnerText;
                                            break;
                                        case "City":
                                            customer.BillAddr.City = baNode.InnerText;
                                            break;
                                        case "Country":
                                            customer.BillAddr.Country = baNode.InnerText;
                                            break;
                                        case "State":
                                            customer.BillAddr.State = baNode.InnerText;
                                            break;
                                    }
                                }
                                break;
                        }
                    }

                    customersLst.Add(customer);
                }

                booSessionBegun = false;
                qbXMLProc.EndSession(ticket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                {
                    qbXMLProc.EndSession(ticket);
                }
            }

            return customersLst;
        }

        public List<QBInvoice> GetInvoicesXML()
        {
            List<QBInvoice> invoicesLst = null;
            string ticket = null;

            try
            {
                ticket = qbXMLProc.BeginSession("", QBFileMode.qbFileOpenDoNotCare);
                booSessionBegun = true;

                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 12, 0);

                //Step 3: Create the query object needed to perform InvoiceQueryRq
                IInvoiceQuery invoiceQuery = requestSet.AppendInvoiceQueryRq();
                var xmlReq = requestSet.ToXMLString();

                var strOutXML = qbXMLProc.ProcessRequest(ticket, xmlReq);
                invoicesLst = new List<QBInvoice>();
                XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
                xmlDoc.LoadXml(strOutXML);
                // Get elements
                XmlNodeList listIDnd = xmlDoc.GetElementsByTagName("InvoiceRet");
                for (int i = 0; i < listIDnd.Count; i++)
                {
                    string xml = listIDnd[i].InnerXml;
                    XmlNodeList listCont = listIDnd[i].ChildNodes;
                    QBInvoice invoice = new QBInvoice();
                    foreach (XmlNode node in listCont)
                    {
                        switch (node.Name)
                        {
                            case "TxnID":
                                invoice.TxnID = node.InnerText;
                                break;
                            //case "EditSequence":
                            //    customer.EditSeq = node.InnerText;
                            //    break;
                            //case "Name":
                            //    customer.Name = node.InnerText;
                            //    break;
                            //case "FullName":
                            //    customer.FullName = node.InnerText;
                            //    break;
                            //case "BillAddress":
                            //    XmlNodeList billAddrNodes = node.ChildNodes;
                            //    foreach (XmlNode baNode in billAddrNodes)
                            //    {
                            //        switch (baNode.Name)
                            //        {
                            //            case "Addr1":
                            //                customer.BillAddr.Addr1 = baNode.InnerText;
                            //                break;
                            //            case "Addr2":
                            //                customer.BillAddr.Addr2 = baNode.InnerText;
                            //                break;
                            //            case "City":
                            //                customer.BillAddr.City = baNode.InnerText;
                            //                break;
                            //            case "Country":
                            //                customer.BillAddr.Country = baNode.InnerText;
                            //                break;
                            //            case "State":
                            //                customer.BillAddr.State = baNode.InnerText;
                            //                break;
                            //        }
                            //    }
                            //    break;
                        }
                    }

                    invoicesLst.Add(invoice);
                }

                booSessionBegun = false;
                qbXMLProc.EndSession(ticket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                {
                    qbXMLProc.EndSession(ticket);
                }
            }

            return invoicesLst;
        }

        // Gets Invoices list by specified customers listID 
        public List<QBInvoice> GetInvoices(List<string> listIDs)
        {
            List<QBInvoice> invoiceLst = new List<QBInvoice>();

            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                booSessionBegun = true;

                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 12, 0);

                //Step 3: Create the query object needed to perform InvoiceQueryRq
                IInvoiceQuery invoiceQuery = requestSet.AppendInvoiceQueryRq();
                invoiceQuery.IncludeLineItems.SetValue(true);
                foreach (var id in listIDs)
                    invoiceQuery.ORInvoiceQuery.InvoiceFilter.EntityFilter.OREntityFilter.ListIDList.Add(id);

                IMsgSetResponse responseSet = sessionManager.DoRequests(requestSet);
                IResponse response = responseSet.ResponseList.GetAt(0);

                IInvoiceRetList invRetList = (IInvoiceRetList)response.Detail;
                if (invRetList != null)
                {
                    for (int i = 0; i < invRetList.Count; i++)
                    {
                        IInvoiceRet invoiceRet = invRetList.GetAt(i);
                        var invoice = new QBInvoice();
                        invoice.TxnID = invoiceRet.TxnID.GetValue();
                        for (int j = 0; j < invoiceRet.ORInvoiceLineRetList.Count; j++)
                        {
                            var item = invoiceRet.ORInvoiceLineRetList.GetAt(j);
                            invoice.Items = new List<QBItem>();
                            var qbItem = new QBItem();
                            qbItem.ID = item.InvoiceLineRet.ItemRef.ListID.GetValue();
                            qbItem.Name = item.InvoiceLineRet.ItemRef.FullName.GetValue();
                            qbItem.Amount = item.InvoiceLineRet.Amount.GetValue();
                            invoice.Items.Add(qbItem);
                        }

                        invoiceLst.Add(invoice);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                {
                    sessionManager.EndSession();
                }
            }

            return invoiceLst;
        }

        // Adds additional contact info like Phones, Email
        private void AddAdditContactRef(XmlDocument inputXMLDoc, XmlElement customerAdd, string name, string value)
        {
            if (value == null || value.Trim() == "")
                return;
            XmlElement addContactRef = inputXMLDoc.CreateElement("AdditionalContactRef");
            customerAdd.AppendChild(addContactRef);
            XmlElement customerHomePhone = inputXMLDoc.CreateElement("ContactName");
            addContactRef.AppendChild(customerHomePhone);
            customerHomePhone.InnerText = name;
            XmlElement customerHomePhoneVal = inputXMLDoc.CreateElement("ContactValue");
            addContactRef.AppendChild(customerHomePhoneVal);
            customerHomePhoneVal.InnerText = value;
        }

        private void AddBillAddr(XmlDocument inputXMLDoc, XmlElement customerAdd, ReportData report_data)
        {
            if (report_data == null)
                return;

            XmlElement customerBillAddr = inputXMLDoc.CreateElement("BillAddress");
            customerAdd.AppendChild(customerBillAddr);
            // Addr1 - Street
            XmlElement customerBillAddr1 = inputXMLDoc.CreateElement("Addr1");
            customerBillAddr.AppendChild(customerBillAddr1);
            var prim_addr = "";
            if (report_data.primary_address_street != null)
            {
                if (report_data.primary_address_street.Length > 36)
                    prim_addr = report_data.primary_address_street.Substring(0, 36);
                else
                    prim_addr = report_data.primary_address_street;
            }

            customerBillAddr1.InnerText = prim_addr;
            // City
            XmlElement customerBillCity = inputXMLDoc.CreateElement("City");
            customerBillAddr.AppendChild(customerBillCity);
            customerBillCity.InnerText = report_data.primary_address_city;
            // State
            XmlElement customerBillState = inputXMLDoc.CreateElement("State");
            customerBillAddr.AppendChild(customerBillState);
            customerBillState.InnerText = report_data.primary_address_state;
            // Country
            XmlElement customerBillCountry = inputXMLDoc.CreateElement("Country");
            customerBillAddr.AppendChild(customerBillCountry);
            customerBillCountry.InnerText = report_data.primary_address_country;
        }

        // Concatenate emails to one string
        private string ConcatEmails(List<EmailAddr> emails)
        {
            var multEmailsStr = "";
            foreach (var item in emails)
                multEmailsStr += item.email + ",";

            // remove last ','
            if (multEmailsStr.Length > 0)
                multEmailsStr = multEmailsStr.Remove(multEmailsStr.Length - 1);

            return multEmailsStr;
        }

        private void AddPhoneNumbers(XmlDocument inputXMLDoc, Person stud, XmlElement customerEl)
        {
            // set Phones
            var primary_phone = "";
            if (stud.report_data != null && stud.report_data.contact_primary_phone != null)
            {
                primary_phone = stud.report_data.contact_primary_phone;
                AddAdditContactRef(inputXMLDoc, customerEl, "Main Phone", primary_phone); // set as main
            }

            if (stud.phone_numbers != null)
            {
                foreach (var item in stud.phone_numbers)
                {
                    // Prim. Phone already set up - skip
                    if (item.number != primary_phone)
                    {
                        switch (item.type)
                        {
                            case "work":
                                AddAdditContactRef(inputXMLDoc, customerEl, "Work Phone", item.number); // set as main
                                break;
                            case "home":
                                AddAdditContactRef(inputXMLDoc, customerEl, "Home Phone", item.number); // set as main
                                break;
                            case "mobile":
                                AddAdditContactRef(inputXMLDoc, customerEl, "Mobile", item.number); // set as main
                                break;
                            case "other":
                                AddAdditContactRef(inputXMLDoc, customerEl, "Alt. Phone", item.number); // set as main
                                break;
                        }
                    }
                }
            }
        }

        // Only modification variant, without Name tag
        private void AddCustomerFields(XmlDocument inputXMLDoc, XmlElement customerEl, Person stud)
        {
            // set FirstName
            XmlElement customerFirstName = inputXMLDoc.CreateElement("FirstName");
            customerFirstName.InnerXml = stud.first_name;
            customerEl.AppendChild(customerFirstName);
            // set LastName
            XmlElement customerLastName = inputXMLDoc.CreateElement("LastName");
            customerLastName.InnerXml = stud.last_name;
            customerEl.AppendChild(customerLastName);

            // Set Bill Addr
            AddBillAddr(inputXMLDoc, customerEl, stud.report_data);

            // set Emails in one string line
            if (stud.email_addresses != null)
            {
                var conctEmailsStr = ConcatEmails(stud.email_addresses);
                AddAdditContactRef(inputXMLDoc, customerEl, "Main Email", conctEmailsStr);
            }

            // Set Phones
            AddPhoneNumbers(inputXMLDoc, stud, customerEl);
        }

        // Creation variant - with name tag
        private void AddCustomerFields(XmlDocument inputXMLDoc, XmlElement customerEl, Person stud, int countDup)
        {
            XmlElement customerName = inputXMLDoc.CreateElement("Name");
            var dupSufix = "";
            if (countDup > 0)
                dupSufix = " (DUP " + countDup + ")";

            var fullName = stud.last_name + ", " + stud.first_name + dupSufix;
            // set Name
            customerName.InnerText = fullName;
            customerEl.AppendChild(customerName);

            AddCustomerFields(inputXMLDoc, customerEl, stud);
        }

        // Create Customer through xml
        public bool CreateCustomerXML(Person stud, ref CustomerIDs custIDs, int countDup)
        {
            string strRequestXML = "";
            XmlDocument inputXMLDoc = null;

            // CustomerQuery
            inputXMLDoc = new XmlDocument();
            //inputXMLDoc.AppendChild(inputXMLDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            inputXMLDoc.AppendChild(inputXMLDoc.CreateXmlDeclaration("1.0", "ISO-8859-1", null));
            inputXMLDoc.AppendChild(inputXMLDoc.CreateProcessingInstruction("qbxml", "version=\"16.0\""));

            XmlElement qbXML = inputXMLDoc.CreateElement("QBXML");
            inputXMLDoc.AppendChild(qbXML);
            XmlElement qbXMLMsgsRq = inputXMLDoc.CreateElement("QBXMLMsgsRq");
            qbXML.AppendChild(qbXMLMsgsRq);
            qbXMLMsgsRq.SetAttribute("onError", "continueOnError");
            XmlElement customerAddRq = inputXMLDoc.CreateElement("CustomerAddRq");
            qbXMLMsgsRq.AppendChild(customerAddRq);
            XmlElement customerAdd = inputXMLDoc.CreateElement("CustomerAdd");
            customerAddRq.AppendChild(customerAdd);
            // Add customer fields: Name, FirstName, Emails etc
            AddCustomerFields(inputXMLDoc, customerAdd, stud, countDup);

            strRequestXML = inputXMLDoc.OuterXml;

            booSessionBegun = false;
            bool operSuccess = true;

            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                booSessionBegun = true;
                IMsgSetResponse responseSet = sessionManager.DoRequestsFromXMLString(strRequestXML);
                var respXML = responseSet.ToXMLString();
                IResponse response = responseSet.ResponseList.GetAt(0);
                int statusCode = response.StatusCode;
                string statusMessage = response.StatusMessage;
                string statusSeverity = response.StatusSeverity;
                string resMes = "Status: Code = " + statusCode + "Message = " + statusMessage + "Severity = " + statusSeverity;
                Console.WriteLine(resMes);

                //step4: parse the XML response and show a message
                XmlDocument outputXMLDoc = new XmlDocument();
                outputXMLDoc.LoadXml(respXML);
                XmlNodeList qbXMLMsgsRsNodeList = outputXMLDoc.GetElementsByTagName("CustomerAddRs");

                if (qbXMLMsgsRsNodeList.Count == 1) //it's always true, since we added a single Customer
                {
                    System.Text.StringBuilder popupMessage = new System.Text.StringBuilder();

                    XmlAttributeCollection rsAttributes = qbXMLMsgsRsNodeList.Item(0).Attributes;

                    //get the CustomerRet node for detailed info

                    //a CustomerAddRs contains max one childNode for "CustomerRet"
                    XmlNodeList custAddRsNodeList = qbXMLMsgsRsNodeList.Item(0).ChildNodes;
                    if (custAddRsNodeList.Count == 1 && custAddRsNodeList.Item(0).Name.Equals("CustomerRet"))
                    {
                        XmlNodeList custRetNodeList = custAddRsNodeList.Item(0).ChildNodes;

                        foreach (XmlNode custRetNode in custRetNodeList)
                        {
                            if (custRetNode.Name.Equals("ListID"))
                            {
                                var ListID = custRetNode.InnerText;
                                custIDs.ListID = ListID;
                                popupMessage.AppendFormat("\r\nCustomer ListID = {0}", custRetNode.InnerText);
                            } 
                            else if (custRetNode.Name.Equals("EditSequence"))
                            {
                                var editSeq = custRetNode.InnerText;
                                custIDs.EditSeq = editSeq;
                            }
                        }
                    } // End of customerRet
                }

                if (statusCode == 0)
                {
                    operSuccess = true;
                }
                else
                    operSuccess = false;

                // Close the session and connection with QuickBooks
                sessionManager.EndSession();
                booSessionBegun = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                    sessionManager.EndSession();
            }

            return operSuccess;
        }

        public bool UpdateCustomerXML(string ListID, Person stud)
        {
            var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string strRequestXML = "";
            XmlDocument inputXMLDoc = null;
            // Get Customer by ListID
            var customer = GetCustomerByListID(ListID);
            if (customer == null)
                Console.WriteLine("Error {0}(): customer with ListID={1} not found.", methodName, ListID);

            // CustomerQuery
            inputXMLDoc = new XmlDocument();
            //inputXMLDoc.AppendChild(inputXMLDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            inputXMLDoc.AppendChild(inputXMLDoc.CreateXmlDeclaration("1.0", "ISO-8859-1", null));
            inputXMLDoc.AppendChild(inputXMLDoc.CreateProcessingInstruction("qbxml", "version=\"16.0\""));

            XmlElement qbXML = inputXMLDoc.CreateElement("QBXML");
            inputXMLDoc.AppendChild(qbXML);
            XmlElement qbXMLMsgsRq = inputXMLDoc.CreateElement("QBXMLMsgsRq");
            qbXML.AppendChild(qbXMLMsgsRq);
            qbXMLMsgsRq.SetAttribute("onError", "continueOnError");
            XmlElement customerModRq = inputXMLDoc.CreateElement("CustomerModRq");
            qbXMLMsgsRq.AppendChild(customerModRq);
            XmlElement customerMod = inputXMLDoc.CreateElement("CustomerMod");
            customerModRq.AppendChild(customerMod);
            XmlElement custListID = inputXMLDoc.CreateElement("ListID");
            customerMod.AppendChild(custListID);
            custListID.InnerText = customer.ListID;
            XmlElement custEditSeq = inputXMLDoc.CreateElement("EditSequence");
            customerMod.AppendChild(custEditSeq);
            custEditSeq.InnerText = customer.EditSeq;

            // Add customer fields: Name, FirstName, Emails etc
            AddCustomerFields(inputXMLDoc, customerMod, stud);

            strRequestXML = inputXMLDoc.OuterXml;

            booSessionBegun = false;
            bool operSuccess = true;

            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                booSessionBegun = true;
                IMsgSetResponse responseSet = sessionManager.DoRequestsFromXMLString(strRequestXML);

                IResponse response = responseSet.ResponseList.GetAt(0);
                int statusCode = response.StatusCode;
                string statusMessage = response.StatusMessage;
                string statusSeverity = response.StatusSeverity;
                string resMes = "Status: Code = " + statusCode + "Message = " + statusMessage + "Severity = " + statusSeverity;
                Console.WriteLine(resMes);

                if (statusCode == 0)
                {
                    operSuccess = true;
                }
                else
                    operSuccess = false;

                // Close the session and connection with QuickBooks
                sessionManager.EndSession();
                booSessionBegun = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                    sessionManager.EndSession();
            }

            return operSuccess;
        }

        public bool CreateCustomer(Person stud)
        {
            // We want to know if we begun a session so we can end it if an
            // error happens
            booSessionBegun = false;
            bool importSuccess = true;

            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                booSessionBegun = true;

                // Get the RequestMsgSet based on the correct QB Version
                // Create the message set request object
                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 13, 0);

                // Initialize the message set request object
                requestSet.Attributes.OnError = ENRqOnError.roeStop;

                // Add the request to the message set request object
                var Custq = (ICustomerAdd)requestSet.AppendCustomerAddRq();
                var fullName = stud.last_name + ", " + stud.first_name;
                Custq.Name.SetValue(fullName);
                Custq.FirstName.SetValue(stud.first_name);
                Custq.LastName.SetValue(stud.last_name);
                // set Emails
                var multEmailsStr = "";
                foreach (var item in stud.email_addresses)
                    multEmailsStr += item.email + ",";

                // remove last ','
                multEmailsStr = multEmailsStr.Remove(multEmailsStr.Length - 1);
                Custq.Email.SetValue(multEmailsStr);

                // set Phones
                foreach (var item in stud.phone_numbers)
                {
                    bool wasWork = false;
                    switch (item.type)
                    {
                        case "work":
                            if (!wasWork)
                                Custq.Phone.SetValue(item.number); // set as main
                            else
                                Custq.AltPhone.SetValue(item.number); // set as alt
                            wasWork = true;
                            break;
                        case "home":
                            Custq.AltPhone.SetValue(item.number);
                            break;
                        case "mobile":
                            Custq.Mobile.SetValue(item.number);
                            break;
                    }
                }

                // Bill Address
                Custq.BillAddress.Country.SetValue(stud.report_data.primary_address_country);
                Custq.BillAddress.State.SetValue(stud.report_data.primary_address_state);
                Custq.BillAddress.City.SetValue(stud.report_data.primary_address_city);
                var prim_addr = "";
                if (stud.report_data.primary_address_street != null)
                {
                    if (stud.report_data.primary_address_street.Length > 36)
                        prim_addr = stud.report_data.primary_address_street.Substring(0, 36);
                    else
                        prim_addr = stud.report_data.primary_address_street;
                }

                Custq.BillAddress.Addr1.SetValue(prim_addr);

                // Do the request and get the response message set object
                var xml = requestSet.ToXMLString();
                IMsgSetResponse responseSet = sessionManager.DoRequests(requestSet);
                IResponse response = responseSet.ResponseList.GetAt(0);
                int statusCode = response.StatusCode;
                string statusMessage = response.StatusMessage;
                string statusSeverity = response.StatusSeverity;
                string resMes = "Status: Code = " + statusCode + "Message = " + statusMessage + "Severity = " + statusSeverity;
                Console.WriteLine(resMes);

                if (statusCode == 0)
                {
                    importSuccess = true;
                }
                else
                    importSuccess = false;

                // Close the session and connection with QuickBooks
                sessionManager.EndSession();
                booSessionBegun = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                    sessionManager.EndSession();
            }

            return importSuccess;
        }

        public bool UpdateCustomer(string ListID, string EditSeq, Person stud)
        {
            // We want to know if we begun a session so we can end it if an
            // error happens
            booSessionBegun = false;
            bool operSuccess = true;

            try
            {
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                booSessionBegun = true;

                // Get the RequestMsgSet based on the correct QB Version
                // Create the message set request object
                IMsgSetRequest requestSet = sessionManager.CreateMsgSetRequest("US", 13, 0);

                // Initialize the message set request object
                requestSet.Attributes.OnError = ENRqOnError.roeStop;

                // Add the request to the message set request object
                var CustModq = requestSet.AppendCustomerModRq();
                CustModq.ListID.SetValue(ListID);
                CustModq.EditSequence.SetValue(EditSeq);
                // set Emails
                var multEmailsStr = "";
                foreach (var item in stud.email_addresses)
                    multEmailsStr += item.email + ",";

                // remove last ','
                multEmailsStr = multEmailsStr.Remove(multEmailsStr.Length - 1);
                CustModq.Email.SetValue(multEmailsStr);

                // set Phones
                foreach (var item in stud.phone_numbers)
                {
                    bool wasWork = false;
                    switch (item.type)
                    {
                        case "work":
                            if (!wasWork)
                                CustModq.Phone.SetValue(item.number); // set as main
                            else
                                CustModq.AltPhone.SetValue(item.number); // set as alt
                            wasWork = true;
                            break;
                        case "home":
                            CustModq.AltPhone.SetValue(item.number);
                            break;
                        case "mobile":
                            CustModq.Mobile.SetValue(item.number);
                            break;
                    }
                }

                // Bill Address
                CustModq.BillAddress.Country.SetValue(stud.report_data.primary_address_country);
                CustModq.BillAddress.State.SetValue(stud.report_data.primary_address_state);
                CustModq.BillAddress.City.SetValue(stud.report_data.primary_address_city);
                var prim_addr = "";
                if (stud.report_data.primary_address_street != null)
                {
                    if (stud.report_data.primary_address_street.Length > 36)
                        prim_addr = stud.report_data.primary_address_street.Substring(0, 36);
                    else
                        prim_addr = stud.report_data.primary_address_street;

                    CustModq.BillAddress.Addr1.SetValue(prim_addr);
                }

                // Do the request and get the response message set object
                IMsgSetResponse responseSet = sessionManager.DoRequests(requestSet);
                IResponse response = responseSet.ResponseList.GetAt(0);
                int statusCode = response.StatusCode;
                string statusMessage = response.StatusMessage;
                string statusSeverity = response.StatusSeverity;
                string resMes = "Status: Code = " + statusCode + "Message = " + statusMessage + "Severity = " + statusSeverity;
                Console.WriteLine(resMes);

                if (statusCode == 0)
                {
                    operSuccess = true;
                }
                else
                    operSuccess = false;

                // Close the session and connection with QuickBooks
                sessionManager.EndSession();
                booSessionBegun = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString() + "\nStack Trace: \n" + ex.StackTrace + "\nExiting the application");

                if (booSessionBegun)
                    sessionManager.EndSession();
            }

            return operSuccess;
        }
    }

    class CustomerIDs
    {
        public string ListID;
        public string EditSeq;
    }

    class Customer
    {
        public string ListID;
        public string EditSeq;
        public string Name;
        public string FullName;
        public string CompanyName;
        public string Email;
        public string Phone;
        public Address BillAddr = new Address();
    }

    class Address
    { 
        public string Addr1; 
        public string Addr2; 
        public string City;
        public string State;
        public string Country;
    }

    class QBInvoice
    {
        public string TxnID;
        public List<QBItem> Items = new List<QBItem>();
    }

    class QBItem
    {
        public string ID;
        public string Name;
        public double Amount;
    }
}
