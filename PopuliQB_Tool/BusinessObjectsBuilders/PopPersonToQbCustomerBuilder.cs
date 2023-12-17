using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.Helpers;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPersonToQbCustomerBuilder
{
    /*public static string GetFullName(PopPerson person)
    {
        var fullName = "";
        if (!string.IsNullOrEmpty(person.FirstName))
        {
            fullName += $"{person.FirstName}";
        }

        if (!string.IsNullOrEmpty(person.LastName))
        {
            fullName += $" {person.LastName}";
        }

        return fullName;
    }*/

    public void BuildPersonAddRequest(IMsgSetRequest request, PopPerson person)
    {
        var customerAddRq = request.AppendCustomerAddRq();

        customerAddRq.Fax.SetValue(person.Id.ToString());

        var maxLength = Convert.ToInt32(customerAddRq.Salutation.GetMaxLength());
        customerAddRq.Salutation.SetValue(person.Prefix?.Length <= maxLength ? person.Prefix : "");

        maxLength = Convert.ToInt32(customerAddRq.Name.GetMaxLength());
        if (!string.IsNullOrEmpty(person.DisplayName) && person.DisplayName.Length <= maxLength)
        {
            customerAddRq.Name.SetValue(person.DisplayName);
        }
        else
        {
            customerAddRq.Name.SetValue(person.DisplayName?.DivideIntoEqualParts(maxLength)[0]);
        }

        if (!string.IsNullOrEmpty(person.FirstName))
        {
            maxLength = Convert.ToInt32(customerAddRq.FirstName.GetMaxLength());
            customerAddRq.FirstName.SetValue(person.FirstName?.Length <= maxLength ? person.FirstName : "");
        }

        if (!string.IsNullOrEmpty(person.LastName))
        {
            maxLength = Convert.ToInt32(customerAddRq.LastName.GetMaxLength());
            if (person.LastName.Length <= maxLength)
            {
                customerAddRq.LastName.SetValue(person.LastName);
            }
        }

        if (!string.IsNullOrEmpty(person.Suffix))
        {
            maxLength = Convert.ToInt32(customerAddRq.Suffix.GetMaxLength());
            if (person.Suffix.Length <= maxLength)
            {
                customerAddRq.JobTitle.SetValue(person.Suffix);
            }
        }

        customerAddRq.Email.SetValue(person.ReportData?.ContactPrimaryEmail ?? "");
        customerAddRq.IsActive.SetValue(true);
        customerAddRq.CompanyName.SetValue(QBCompanyService.CompanyName);

        if (person.Addresses!= null && person.Addresses.Any())
        {
            var add = person.Addresses[0];
            maxLength = Convert.ToInt32(customerAddRq.BillAddress.Addr1.GetMaxLength());
            if (add.Street?.Length <= maxLength)
            {
                customerAddRq.BillAddress.Addr1.SetValue(add.Street);
            }
            else
            {
                if (!string.IsNullOrEmpty(add.Street))
                {
                    var adds = add.Street.DivideIntoEqualParts(maxLength);
                    if (adds.Count > 0)
                    {
                        customerAddRq.BillAddress.Addr1.SetValue(adds[0]);
                    }

                    if (adds.Count > 1)
                    {
                        customerAddRq.BillAddress.Addr2.SetValue(adds[1]);
                    }

                    if (adds.Count > 2)
                    {
                        customerAddRq.BillAddress.Addr2.SetValue(adds[2]);
                    }
                }
            }

            maxLength = Convert.ToInt32(customerAddRq.BillAddress.City.GetMaxLength());
            customerAddRq.BillAddress.City.SetValue(add.City?.Length <= maxLength ? add.City : "");

            maxLength = Convert.ToInt32(customerAddRq.BillAddress.Country.GetMaxLength());
            customerAddRq.BillAddress.Country.SetValue(add.Country?.Length <= maxLength ? add.Country : "");

            maxLength = Convert.ToInt32(customerAddRq.BillAddress.PostalCode.GetMaxLength());
            customerAddRq.BillAddress.PostalCode.SetValue(add.Postal?.Length <= maxLength ? add.Postal : "");

            maxLength = Convert.ToInt32(customerAddRq.BillAddress.State.GetMaxLength());
            customerAddRq.BillAddress.State.SetValue(add.State?.Length <= maxLength ? add.State : "");

            maxLength = Convert.ToInt32(customerAddRq.BillAddress.Note.GetMaxLength());
            customerAddRq.BillAddress.Note.SetValue(add.Type?.Length <= maxLength ? add.Type : "");
        }

        if (person.PhoneNumbers != null && person.PhoneNumbers.Any())
        {
            var ph1 = person.PhoneNumbers[0].Number;
            if (ph1 != null)
            {
                maxLength = Convert.ToInt32(customerAddRq.Phone.GetMaxLength());
                if (ph1.Length <= maxLength)
                {
                    customerAddRq.Phone.SetValue(ph1);
                }
                else
                {
                    var phs = ph1.DivideIntoEqualParts(maxLength);
                    if (phs.Count > 0)
                    {
                        customerAddRq.Phone.SetValue(phs[0]);
                    }

                    if (phs.Count > 1)
                    {
                        customerAddRq.AltPhone.SetValue(phs[1]);
                    }
                }

                maxLength = Convert.ToInt32(customerAddRq.Contact.GetMaxLength());
                if (ph1.Length <= maxLength)
                {
                    customerAddRq.Contact.SetValue(ph1);
                }

                maxLength = Convert.ToInt32(customerAddRq.Mobile.GetMaxLength());
                if (ph1.Length <= maxLength)
                {
                    customerAddRq.Mobile.SetValue(ph1);
                }
            }

            if (person.PhoneNumbers.Count > 1)
            {
                var ph2 = person.PhoneNumbers[1].Number;
                if (ph2 != null)
                {
                    maxLength = Convert.ToInt32(customerAddRq.AltPhone.GetMaxLength());
                    if (ph2.Length <= maxLength)
                    {
                        customerAddRq.AltPhone.SetValue(ph2);
                    }

                    maxLength = Convert.ToInt32(customerAddRq.AltContact.GetMaxLength());
                    if (ph2.Length <= maxLength)
                    {
                        customerAddRq.AltContact.SetValue(ph2);
                    }
                }
            }
        }

        if (person.PopStudent?.LoaStartDate != null)
        {
            customerAddRq.JobStartDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent?.LoaStartDate != null)
        {
            customerAddRq.JobProjectedEndDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent?.ExitDate != null)
        {
            customerAddRq.JobEndDate.SetValue(person.PopStudent.ExitDate.Value);
        }
    }


    public void BuildGetAllPersonsRequest(IMsgSetRequest request)
    {
        ICustomerQuery customerQuery = request.AppendCustomerQueryRq();
        //customerQuery.ORCustomerListQuery.CustomerListFilter.ActiveStatus.SetValue(ENActiveStatus.asAll);
        customerQuery.IncludeRetElementList.Add("Name");
        customerQuery.IncludeRetElementList.Add("Fax");
    }
}