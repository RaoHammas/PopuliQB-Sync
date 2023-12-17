using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessObjectsBuilders;
using NLog;
using PopuliQB_Tool.EventArgs;
using PopuliQB_Tool.Models;

namespace PopuliQB_Tool.BusinessServices;

public class QbCustomerService
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly PopPersonToQbCustomerBuilder _customerBuilder;

    public bool IsConnected { get; set; }
    public bool IsSessionOpen { get; set; }
    public EventHandler<StatusMessageArgs>? OnSyncStatusChanged { get; set; }
    public EventHandler<ProgressArgs>? OnSyncProgressChanged { get; set; }
    public List<PopPerson> AllExistingCustomersList { get; set; } = new();
    private const string AppId = "PopuliToQbSync";
    private const string AppName = "PopuliToQbSync";

    public QbCustomerService(PopPersonToQbCustomerBuilder customerBuilder)
    {
        _customerBuilder = customerBuilder;
    }

    #region ADD NEW CUSTOMER

    public async Task<bool> AddCustomersAsync(List<PopPerson> persons)
    {
        var sessionManager = new QBSessionManager();

        try
        {
            sessionManager.OpenConnection(AppId, AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                for (var index = 0; index < persons.Count; index++)
                {
                    var person = persons[index];
                    var personFullName = person.DisplayName;

                    if (AllExistingCustomersList.FirstOrDefault(x => x.Id == person.Id) != null)
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Warn, $"{personFullName} | Id = {person.Id} already exists."));
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(index));
                        continue;
                    }

                    _customerBuilder.BuildPersonAddRequest(requestMsgSet, person);
                    var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                    if (ReadAddedCustomer(responseMsgSet))
                    {
                        AllExistingCustomersList.Add(new PopPerson
                        {
                            Id = person.Id,
                            DisplayName = personFullName,
                        });

                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Success, $"{personFullName} | Id = {person.Id}"));
                        OnSyncProgressChanged?.Invoke(this, new ProgressArgs(index));
                    }
                    else
                    {
                        OnSyncStatusChanged?.Invoke(this,
                            new StatusMessageArgs(StatusMessageType.Error, $"{personFullName} | Id = {person.Id}"));
                    }
                }
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            throw;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private bool ReadAddedCustomer(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null)
        {
            return false;
        }
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null)
        {
            return false;
        }

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);
            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCustomerAddRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var customerRet = (ICustomerRet)response.Detail;
                        if (customerRet != null)
                        {
                            return true;
                        }

                        return false;

                        //WalkCustomerRet(CustomerRet);
                    }
                }
            }
        }

        return false;
    }

    #endregion

    #region GET ALL CUSTOMERS

    public async Task<List<PopPerson>> GetAllExistingCustomersAsync()
    {
        var sessionManager = new QBSessionManager();

        try
        {
            AllExistingCustomersList.Clear();

            sessionManager.OpenConnection(AppId, AppName);
            IsConnected = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Connected to QB."));

            sessionManager.BeginSession("", ENOpenMode.omDontCare);
            IsSessionOpen = true;
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Session Started."));

            await Task.Run(() =>
            {
                var requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                _customerBuilder.BuildGetAllPersonsRequest(requestMsgSet);
                var responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                ReadFetchedCustomers(responseMsgSet);
            });

            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Success, "Completed."));

            return AllExistingCustomersList;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Error, ex.Message));
            return AllExistingCustomersList;
        }
        finally
        {
            if (IsSessionOpen)
            {
                sessionManager.EndSession();
                IsSessionOpen = false;
                OnSyncStatusChanged?.Invoke(this,
                    new StatusMessageArgs(StatusMessageType.Info, "Session Ended."));
            }

            if (IsConnected)
            {
                sessionManager.CloseConnection();
                IsConnected = false;
                OnSyncStatusChanged?.Invoke(this, new StatusMessageArgs(StatusMessageType.Info, "Disconnected."));
            }
        }
    }

    private void ReadFetchedCustomers(IMsgSetResponse? responseMsgSet)
    {
        if (responseMsgSet == null) return;
        var responseList = responseMsgSet.ResponseList;
        if (responseList == null) return;

        //if we sent only one request, there is only one response, we'll walk the list for this sample
        for (var i = 0; i < responseList.Count; i++)
        {
            var response = responseList.GetAt(i);

            //check the status code of the response, 0=ok, >0 is warning
            if (response.StatusCode >= 0)
            {
                //the request-specific response is in the details, make sure we have some
                if (response.Detail != null)
                {
                    //make sure the response is the type we're expecting
                    var responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtCustomerQueryRs)
                    {
                        //upcast to more specific type here, this is safe because we checked with response.Type check above
                        var customerRet = (ICustomerRetList)response.Detail;

                        for (var x = 0; x < customerRet.Count; x++)
                        {
                            var person = ReadCustomerProperties(customerRet.GetAt(x));
                            if (person != null)
                            {
                                AllExistingCustomersList.Add(person);
                            }
                        }
                    }
                }
            }
        }
    }

    private PopPerson? ReadCustomerProperties(ICustomerRet? customerRet)
    {
        try
        {
            if (customerRet == null) return null;

            var name = customerRet.Name.GetValue();
            var uniqueId = customerRet.Fax.GetValue();
            return new PopPerson
            {
                Id = Convert.ToInt32(uniqueId),
                DisplayName = name,
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return null;

        /*
        //Go through all the elements of ICustomerRetList

        //Get value of ListID
        string ListID7497 = (string)customerRet.ListID.GetValue();
        //Get value of TimeCreated
        DateTime TimeCreated7498 = (DateTime)customerRet.TimeCreated.GetValue();
        //Get value of TimeModified
        DateTime TimeModified7499 = (DateTime)customerRet.TimeModified.GetValue();
        //Get value of EditSequence
        string EditSequence7500 = (string)customerRet.EditSequence.GetValue();
        //Get value of Name
        string Name7501 = (string)customerRet.Name.GetValue();
        //Get value of FullName
        string FullName7502 = (string)customerRet.FullName.GetValue();
        //Get value of IsActive
        if (customerRet.IsActive != null)
        {
            bool IsActive7503 = (bool)customerRet.IsActive.GetValue();
        }

        if (customerRet.ClassRef != null)
        {
            //Get value of ListID
            if (customerRet.ClassRef.ListID != null)
            {
                string ListID7504 = (string)customerRet.ClassRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.ClassRef.FullName != null)
            {
                string FullName7505 = (string)customerRet.ClassRef.FullName.GetValue();
            }
        }

        if (customerRet.ParentRef != null)
        {
            //Get value of ListID
            if (customerRet.ParentRef.ListID != null)
            {
                string ListID7506 = (string)customerRet.ParentRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.ParentRef.FullName != null)
            {
                string FullName7507 = (string)customerRet.ParentRef.FullName.GetValue();
            }
        }

        //Get value of Sublevel
        int Sublevel7508 = (int)customerRet.Sublevel.GetValue();
        //Get value of CompanyName
        if (customerRet.CompanyName != null)
        {
            string CompanyName7509 = (string)customerRet.CompanyName.GetValue();
        }

        //Get value of Salutation
        if (customerRet.Salutation != null)
        {
            string Salutation7510 = (string)customerRet.Salutation.GetValue();
        }

        //Get value of FirstName
        if (customerRet.FirstName != null)
        {
            string FirstName7511 = (string)customerRet.FirstName.GetValue();
        }

        //Get value of MiddleName
        if (customerRet.MiddleName != null)
        {
            string MiddleName7512 = (string)customerRet.MiddleName.GetValue();
        }

        //Get value of LastName
        if (customerRet.LastName != null)
        {
            string LastName7513 = (string)customerRet.LastName.GetValue();
        }

        //Get value of JobTitle
        if (customerRet.JobTitle != null)
        {
            string JobTitle7514 = (string)customerRet.JobTitle.GetValue();
        }

        if (customerRet.BillAddress != null)
        {
            //Get value of Addr1
            if (customerRet.BillAddress.Addr1 != null)
            {
                string Addr17515 = (string)customerRet.BillAddress.Addr1.GetValue();
            }

            //Get value of Addr2
            if (customerRet.BillAddress.Addr2 != null)
            {
                string Addr27516 = (string)customerRet.BillAddress.Addr2.GetValue();
            }

            //Get value of Addr3
            if (customerRet.BillAddress.Addr3 != null)
            {
                string Addr37517 = (string)customerRet.BillAddress.Addr3.GetValue();
            }

            //Get value of Addr4
            if (customerRet.BillAddress.Addr4 != null)
            {
                string Addr47518 = (string)customerRet.BillAddress.Addr4.GetValue();
            }

            //Get value of Addr5
            if (customerRet.BillAddress.Addr5 != null)
            {
                string Addr57519 = (string)customerRet.BillAddress.Addr5.GetValue();
            }

            //Get value of City
            if (customerRet.BillAddress.City != null)
            {
                string City7520 = (string)customerRet.BillAddress.City.GetValue();
            }

            //Get value of State
            if (customerRet.BillAddress.State != null)
            {
                string State7521 = (string)customerRet.BillAddress.State.GetValue();
            }

            //Get value of PostalCode
            if (customerRet.BillAddress.PostalCode != null)
            {
                string PostalCode7522 = (string)customerRet.BillAddress.PostalCode.GetValue();
            }

            //Get value of Country
            if (customerRet.BillAddress.Country != null)
            {
                string Country7523 = (string)customerRet.BillAddress.Country.GetValue();
            }

            //Get value of Note
            if (customerRet.BillAddress.Note != null)
            {
                string Note7524 = (string)customerRet.BillAddress.Note.GetValue();
            }
        }

        if (customerRet.BillAddressBlock != null)
        {
            //Get value of Addr1
            if (customerRet.BillAddressBlock.Addr1 != null)
            {
                string Addr17525 = (string)customerRet.BillAddressBlock.Addr1.GetValue();
            }

            //Get value of Addr2
            if (customerRet.BillAddressBlock.Addr2 != null)
            {
                string Addr27526 = (string)customerRet.BillAddressBlock.Addr2.GetValue();
            }

            //Get value of Addr3
            if (customerRet.BillAddressBlock.Addr3 != null)
            {
                string Addr37527 = (string)customerRet.BillAddressBlock.Addr3.GetValue();
            }

            //Get value of Addr4
            if (customerRet.BillAddressBlock.Addr4 != null)
            {
                string Addr47528 = (string)customerRet.BillAddressBlock.Addr4.GetValue();
            }

            //Get value of Addr5
            if (customerRet.BillAddressBlock.Addr5 != null)
            {
                string Addr57529 = (string)customerRet.BillAddressBlock.Addr5.GetValue();
            }
        }

        if (customerRet.ShipAddress != null)
        {
            //Get value of Addr1
            if (customerRet.ShipAddress.Addr1 != null)
            {
                string Addr17530 = (string)customerRet.ShipAddress.Addr1.GetValue();
            }

            //Get value of Addr2
            if (customerRet.ShipAddress.Addr2 != null)
            {
                string Addr27531 = (string)customerRet.ShipAddress.Addr2.GetValue();
            }

            //Get value of Addr3
            if (customerRet.ShipAddress.Addr3 != null)
            {
                string Addr37532 = (string)customerRet.ShipAddress.Addr3.GetValue();
            }

            //Get value of Addr4
            if (customerRet.ShipAddress.Addr4 != null)
            {
                string Addr47533 = (string)customerRet.ShipAddress.Addr4.GetValue();
            }

            //Get value of Addr5
            if (customerRet.ShipAddress.Addr5 != null)
            {
                string Addr57534 = (string)customerRet.ShipAddress.Addr5.GetValue();
            }

            //Get value of City
            if (customerRet.ShipAddress.City != null)
            {
                string City7535 = (string)customerRet.ShipAddress.City.GetValue();
            }

            //Get value of State
            if (customerRet.ShipAddress.State != null)
            {
                string State7536 = (string)customerRet.ShipAddress.State.GetValue();
            }

            //Get value of PostalCode
            if (customerRet.ShipAddress.PostalCode != null)
            {
                string PostalCode7537 = (string)customerRet.ShipAddress.PostalCode.GetValue();
            }

            //Get value of Country
            if (customerRet.ShipAddress.Country != null)
            {
                string Country7538 = (string)customerRet.ShipAddress.Country.GetValue();
            }

            //Get value of Note
            if (customerRet.ShipAddress.Note != null)
            {
                string Note7539 = (string)customerRet.ShipAddress.Note.GetValue();
            }
        }

        if (customerRet.ShipAddressBlock != null)
        {
            //Get value of Addr1
            if (customerRet.ShipAddressBlock.Addr1 != null)
            {
                string Addr17540 = (string)customerRet.ShipAddressBlock.Addr1.GetValue();
            }

            //Get value of Addr2
            if (customerRet.ShipAddressBlock.Addr2 != null)
            {
                string Addr27541 = (string)customerRet.ShipAddressBlock.Addr2.GetValue();
            }

            //Get value of Addr3
            if (customerRet.ShipAddressBlock.Addr3 != null)
            {
                string Addr37542 = (string)customerRet.ShipAddressBlock.Addr3.GetValue();
            }

            //Get value of Addr4
            if (customerRet.ShipAddressBlock.Addr4 != null)
            {
                string Addr47543 = (string)customerRet.ShipAddressBlock.Addr4.GetValue();
            }

            //Get value of Addr5
            if (customerRet.ShipAddressBlock.Addr5 != null)
            {
                string Addr57544 = (string)customerRet.ShipAddressBlock.Addr5.GetValue();
            }
        }

        if (customerRet.ShipToAddressList != null)
        {
            for (int i7545 = 0; i7545 < customerRet.ShipToAddressList.Count; i7545++)
            {
                IShipToAddress ShipToAddress = customerRet.ShipToAddressList.GetAt(i7545);
                //Get value of Name
                string Name7546 = (string)ShipToAddress.Name.GetValue();
                //Get value of Addr1
                if (ShipToAddress.Addr1 != null)
                {
                    string Addr17547 = (string)ShipToAddress.Addr1.GetValue();
                }

                //Get value of Addr2
                if (ShipToAddress.Addr2 != null)
                {
                    string Addr27548 = (string)ShipToAddress.Addr2.GetValue();
                }

                //Get value of Addr3
                if (ShipToAddress.Addr3 != null)
                {
                    string Addr37549 = (string)ShipToAddress.Addr3.GetValue();
                }

                //Get value of Addr4
                if (ShipToAddress.Addr4 != null)
                {
                    string Addr47550 = (string)ShipToAddress.Addr4.GetValue();
                }

                //Get value of Addr5
                if (ShipToAddress.Addr5 != null)
                {
                    string Addr57551 = (string)ShipToAddress.Addr5.GetValue();
                }

                //Get value of City
                if (ShipToAddress.City != null)
                {
                    string City7552 = (string)ShipToAddress.City.GetValue();
                }

                //Get value of State
                if (ShipToAddress.State != null)
                {
                    string State7553 = (string)ShipToAddress.State.GetValue();
                }

                //Get value of PostalCode
                if (ShipToAddress.PostalCode != null)
                {
                    string PostalCode7554 = (string)ShipToAddress.PostalCode.GetValue();
                }

                //Get value of Country
                if (ShipToAddress.Country != null)
                {
                    string Country7555 = (string)ShipToAddress.Country.GetValue();
                }

                //Get value of Note
                if (ShipToAddress.Note != null)
                {
                    string Note7556 = (string)ShipToAddress.Note.GetValue();
                }

                //Get value of DefaultShipTo
                if (ShipToAddress.DefaultShipTo != null)
                {
                    bool DefaultShipTo7557 = (bool)ShipToAddress.DefaultShipTo.GetValue();
                }
            }
        }

        //Get value of Phone
        if (customerRet.Phone != null)
        {
            string Phone7558 = (string)customerRet.Phone.GetValue();
        }

        //Get value of AltPhone
        if (customerRet.AltPhone != null)
        {
            string AltPhone7559 = (string)customerRet.AltPhone.GetValue();
        }

        //Get value of Fax
        if (customerRet.Fax != null)
        {
            string Fax7560 = (string)customerRet.Fax.GetValue();
        }

        //Get value of Email
        if (customerRet.Email != null)
        {
            string Email7561 = (string)customerRet.Email.GetValue();
        }

        //Get value of Cc
        if (customerRet.Cc != null)
        {
            string Cc7562 = (string)customerRet.Cc.GetValue();
        }

        //Get value of Contact
        if (customerRet.Contact != null)
        {
            string Contact7563 = (string)customerRet.Contact.GetValue();
        }

        //Get value of AltContact
        if (customerRet.AltContact != null)
        {
            string AltContact7564 = (string)customerRet.AltContact.GetValue();
        }

        if (customerRet.ContactsRetList != null)
        {
            for (int i7568 = 0; i7568 < customerRet.ContactsRetList.Count; i7568++)
            {
                IContactsRet ContactsRet = customerRet.ContactsRetList.GetAt(i7568);
                //Get value of ListID
                string ListID7569 = (string)ContactsRet.ListID.GetValue();
                //Get value of TimeCreated
                DateTime TimeCreated7570 = (DateTime)ContactsRet.TimeCreated.GetValue();
                //Get value of TimeModified
                DateTime TimeModified7571 = (DateTime)ContactsRet.TimeModified.GetValue();
                //Get value of EditSequence
                string EditSequence7572 = (string)ContactsRet.EditSequence.GetValue();
                //Get value of Contact
                if (ContactsRet.Contact != null)
                {
                    string Contact7573 = (string)ContactsRet.Contact.GetValue();
                }

                //Get value of Salutation
                if (ContactsRet.Salutation != null)
                {
                    string Salutation7574 = (string)ContactsRet.Salutation.GetValue();
                }

                //Get value of FirstName
                string FirstName7575 = (string)ContactsRet.FirstName.GetValue();
                //Get value of MiddleName
                if (ContactsRet.MiddleName != null)
                {
                    string MiddleName7576 = (string)ContactsRet.MiddleName.GetValue();
                }

                //Get value of LastName
                if (ContactsRet.LastName != null)
                {
                    string LastName7577 = (string)ContactsRet.LastName.GetValue();
                }

                //Get value of JobTitle
                if (ContactsRet.JobTitle != null)
                {
                    string JobTitle7578 = (string)ContactsRet.JobTitle.GetValue();
                }

            }
        }

        if (customerRet.CustomerTypeRef != null)
        {
            //Get value of ListID
            if (customerRet.CustomerTypeRef.ListID != null)
            {
                string ListID7582 = (string)customerRet.CustomerTypeRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.CustomerTypeRef.FullName != null)
            {
                string FullName7583 = (string)customerRet.CustomerTypeRef.FullName.GetValue();
            }
        }

        if (customerRet.TermsRef != null)
        {
            //Get value of ListID
            if (customerRet.TermsRef.ListID != null)
            {
                string ListID7584 = (string)customerRet.TermsRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.TermsRef.FullName != null)
            {
                string FullName7585 = (string)customerRet.TermsRef.FullName.GetValue();
            }
        }

        if (customerRet.SalesRepRef != null)
        {
            //Get value of ListID
            if (customerRet.SalesRepRef.ListID != null)
            {
                string ListID7586 = (string)customerRet.SalesRepRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.SalesRepRef.FullName != null)
            {
                string FullName7587 = (string)customerRet.SalesRepRef.FullName.GetValue();
            }
        }

        //Get value of Balance
        if (customerRet.Balance != null)
        {
            double Balance7588 = (double)customerRet.Balance.GetValue();
        }

        //Get value of TotalBalance
        if (customerRet.TotalBalance != null)
        {
            double TotalBalance7589 = (double)customerRet.TotalBalance.GetValue();
        }

        if (customerRet.SalesTaxCodeRef != null)
        {
            //Get value of ListID
            if (customerRet.SalesTaxCodeRef.ListID != null)
            {
                string ListID7590 = (string)customerRet.SalesTaxCodeRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.SalesTaxCodeRef.FullName != null)
            {
                string FullName7591 = (string)customerRet.SalesTaxCodeRef.FullName.GetValue();
            }
        }

        if (customerRet.ItemSalesTaxRef != null)
        {
            //Get value of ListID
            if (customerRet.ItemSalesTaxRef.ListID != null)
            {
                string ListID7592 = (string)customerRet.ItemSalesTaxRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.ItemSalesTaxRef.FullName != null)
            {
                string FullName7593 = (string)customerRet.ItemSalesTaxRef.FullName.GetValue();
            }
        }

        //Get value of SalesTaxCountry
        if (customerRet.SalesTaxCountry != null)
        {
            ENSalesTaxCountry SalesTaxCountry7594 = (ENSalesTaxCountry)customerRet.SalesTaxCountry.GetValue();
        }

        //Get value of ResaleNumber
        if (customerRet.ResaleNumber != null)
        {
            string ResaleNumber7595 = (string)customerRet.ResaleNumber.GetValue();
        }

        //Get value of AccountNumber
        if (customerRet.AccountNumber != null)
        {
            string AccountNumber7596 = (string)customerRet.AccountNumber.GetValue();
        }

        //Get value of CreditLimit
        if (customerRet.CreditLimit != null)
        {
            double CreditLimit7597 = (double)customerRet.CreditLimit.GetValue();
        }

        if (customerRet.PreferredPaymentMethodRef != null)
        {
            //Get value of ListID
            if (customerRet.PreferredPaymentMethodRef.ListID != null)
            {
                string ListID7598 = (string)customerRet.PreferredPaymentMethodRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.PreferredPaymentMethodRef.FullName != null)
            {
                string FullName7599 = (string)customerRet.PreferredPaymentMethodRef.FullName.GetValue();
            }
        }

        if (customerRet.CreditCardInfo != null)
        {
            //Get value of CreditCardNumber
            if (customerRet.CreditCardInfo.CreditCardNumber != null)
            {
                string CreditCardNumber7600 = (string)customerRet.CreditCardInfo.CreditCardNumber.GetValue();
            }

            //Get value of ExpirationMonth
            if (customerRet.CreditCardInfo.ExpirationMonth != null)
            {
                int ExpirationMonth7601 = (int)customerRet.CreditCardInfo.ExpirationMonth.GetValue();
            }

            //Get value of ExpirationYear
            if (customerRet.CreditCardInfo.ExpirationYear != null)
            {
                int ExpirationYear7602 = (int)customerRet.CreditCardInfo.ExpirationYear.GetValue();
            }

            //Get value of NameOnCard
            if (customerRet.CreditCardInfo.NameOnCard != null)
            {
                string NameOnCard7603 = (string)customerRet.CreditCardInfo.NameOnCard.GetValue();
            }

            //Get value of CreditCardAddress
            if (customerRet.CreditCardInfo.CreditCardAddress != null)
            {
                string CreditCardAddress7604 = (string)customerRet.CreditCardInfo.CreditCardAddress.GetValue();
            }

            //Get value of CreditCardPostalCode
            if (customerRet.CreditCardInfo.CreditCardPostalCode != null)
            {
                string CreditCardPostalCode7605 = (string)customerRet.CreditCardInfo.CreditCardPostalCode.GetValue();
            }
        }

        //Get value of JobStatus
        if (customerRet.JobStatus != null)
        {
            ENJobStatus JobStatus7606 = (ENJobStatus)customerRet.JobStatus.GetValue();
        }

        //Get value of JobStartDate
        if (customerRet.JobStartDate != null)
        {
            DateTime JobStartDate7607 = (DateTime)customerRet.JobStartDate.GetValue();
        }

        //Get value of JobProjectedEndDate
        if (customerRet.JobProjectedEndDate != null)
        {
            DateTime JobProjectedEndDate7608 = (DateTime)customerRet.JobProjectedEndDate.GetValue();
        }

        //Get value of JobEndDate
        if (customerRet.JobEndDate != null)
        {
            DateTime JobEndDate7609 = (DateTime)customerRet.JobEndDate.GetValue();
        }

        //Get value of JobDesc
        if (customerRet.JobDesc != null)
        {
            string JobDesc7610 = (string)customerRet.JobDesc.GetValue();
        }

        if (customerRet.JobTypeRef != null)
        {
            //Get value of ListID
            if (customerRet.JobTypeRef.ListID != null)
            {
                string ListID7611 = (string)customerRet.JobTypeRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.JobTypeRef.FullName != null)
            {
                string FullName7612 = (string)customerRet.JobTypeRef.FullName.GetValue();
            }
        }

        //Get value of Notes
        if (customerRet.Notes != null)
        {
            string Notes7613 = (string)customerRet.Notes.GetValue();
        }

        if (customerRet.AdditionalNotesRetList != null)
        {
            for (int i7614 = 0; i7614 < customerRet.AdditionalNotesRetList.Count; i7614++)
            {
                IAdditionalNotesRet AdditionalNotesRet = customerRet.AdditionalNotesRetList.GetAt(i7614);
                //Get value of NoteID
                int NoteID7615 = (int)AdditionalNotesRet.NoteID.GetValue();
                //Get value of Date
                DateTime Date7616 = (DateTime)AdditionalNotesRet.Date.GetValue();
                //Get value of Note
                string Note7617 = (string)AdditionalNotesRet.Note.GetValue();
            }
        }

        //Get value of PreferredDeliveryMethod
        if (customerRet.PreferredDeliveryMethod != null)
        {
            ENPreferredDeliveryMethod PreferredDeliveryMethod7618 =
                (ENPreferredDeliveryMethod)customerRet.PreferredDeliveryMethod.GetValue();
        }

        if (customerRet.PriceLevelRef != null)
        {
            //Get value of ListID
            if (customerRet.PriceLevelRef.ListID != null)
            {
                string ListID7619 = (string)customerRet.PriceLevelRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.PriceLevelRef.FullName != null)
            {
                string FullName7620 = (string)customerRet.PriceLevelRef.FullName.GetValue();
            }
        }

        //Get value of ExternalGUID
        if (customerRet.ExternalGUID != null)
        {
            string ExternalGUID7621 = (string)customerRet.ExternalGUID.GetValue();
        }

        //Get value of TaxRegistrationNumber
        if (customerRet.TaxRegistrationNumber != null)
        {
            string TaxRegistrationNumber7622 = (string)customerRet.TaxRegistrationNumber.GetValue();
        }

        if (customerRet.CurrencyRef != null)
        {
            //Get value of ListID
            if (customerRet.CurrencyRef.ListID != null)
            {
                string ListID7623 = (string)customerRet.CurrencyRef.ListID.GetValue();
            }

            //Get value of FullName
            if (customerRet.CurrencyRef.FullName != null)
            {
                string FullName7624 = (string)customerRet.CurrencyRef.FullName.GetValue();
            }
        }

        if (customerRet.DataExtRetList != null)
        {
            for (int i7625 = 0; i7625 < customerRet.DataExtRetList.Count; i7625++)
            {
                IDataExtRet DataExtRet = customerRet.DataExtRetList.GetAt(i7625);
                //Get value of OwnerID
                if (DataExtRet.OwnerID != null)
                {
                    string OwnerID7626 = (string)DataExtRet.OwnerID.GetValue();
                }

                //Get value of DataExtName
                string DataExtName7627 = (string)DataExtRet.DataExtName.GetValue();
                //Get value of DataExtType
                ENDataExtType DataExtType7628 = (ENDataExtType)DataExtRet.DataExtType.GetValue();
                //Get value of DataExtValue
                string DataExtValue7629 = (string)DataExtRet.DataExtValue.GetValue();
            }
        }*/
    }

    #endregion
}