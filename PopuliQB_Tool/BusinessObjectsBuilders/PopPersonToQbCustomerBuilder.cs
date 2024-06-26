﻿using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;
using PopuliQB_Tool.BusinessServices;
using PopuliQB_Tool.Helpers;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPersonToQbCustomerBuilder
{
    public static string GetFullName(string? firstName, string? lastName)
    {
        var fullName = "";
        
        if (!string.IsNullOrEmpty(lastName))
        {
            fullName += $"{lastName}";
        }

        if (!string.IsNullOrEmpty(firstName))
        {
            fullName += $", {firstName}";
        }

        return fullName;
    }

    public void BuildQbCustomerAddRequest(IMsgSetRequest requestMsgSet, PopPerson person)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCustomerAddRq();
        var maxLength = Convert.ToInt32(request.Salutation.GetMaxLength());
        //request.Salutation.SetValue(person.Prefix?.Length < maxLength ? person.Prefix : "");
        request.Name.SetValue(GetFullName(person.FirstName, person.LastName));

        if (!string.IsNullOrEmpty(person.FirstName))
        {
            maxLength = Convert.ToInt32(request.FirstName.GetMaxLength());
            request.FirstName.SetValue(person.FirstName?.Length < maxLength ? person.FirstName : "");
        }

        if (!string.IsNullOrEmpty(person.LastName))
        {
            maxLength = Convert.ToInt32(request.LastName.GetMaxLength());
            if (person.LastName.Length < maxLength)
            {
                request.LastName.SetValue(person.LastName);
            }
        }

        if (!string.IsNullOrEmpty(person.Suffix))
        {
            maxLength = Convert.ToInt32(request.Suffix.GetMaxLength());
            if (person.Suffix.Length < maxLength)
            {
                request.JobTitle.SetValue(person.Suffix);
            }
        }

        if (person.ReportData?.ContactPrimaryEmail != null)
        {
            request.Email.SetValue(person.ReportData?.ContactPrimaryEmail);
        }

        request.IsActive.SetValue(true);
        
        PopDegree? degree = null;
        if (person.Degrees is { Count: > 0 })
        {
            degree = PopuliAccessService.AllPopuliDegrees.FirstOrDefault(x => x.Id == person.Degrees[0].DegreeId)!;
        }

        if (degree != null)
        {
            request.CompanyName.SetValue(degree.ReportData?.DepartmentName ?? "Divine Mercy University");
        }
        else
        {
            request.CompanyName.SetValue("Divine Mercy University");
        }
        
        if (person.Addresses!= null && person.Addresses.Any())
        {
            var add = person.Addresses[0];
            maxLength = Convert.ToInt32(request.BillAddress.Addr1.GetMaxLength());
            if (add.Street?.Length < maxLength)
            {
                request.BillAddress.Addr1.SetValue(add.Street);
            }
            else
            {
                if (!string.IsNullOrEmpty(add.Street))
                {
                    var adds = add.Street.DivideIntoEqualParts(maxLength);
                    if (adds.Count > 0)
                    {
                        request.BillAddress.Addr1.SetValue(adds[0]);
                    }

                    if (adds.Count > 1)
                    {
                        request.BillAddress.Addr2.SetValue(adds[1]);
                    }

                    if (adds.Count > 2)
                    {
                        request.BillAddress.Addr2.SetValue(adds[2]);
                    }
                }
            }

            maxLength = Convert.ToInt32(request.BillAddress.City.GetMaxLength());
            request.BillAddress.City.SetValue(add.City?.Length < maxLength ? add.City : "");

            maxLength = Convert.ToInt32(request.BillAddress.Country.GetMaxLength());
            request.BillAddress.Country.SetValue(add.Country?.Length < maxLength ? add.Country : "");

            maxLength = Convert.ToInt32(request.BillAddress.PostalCode.GetMaxLength());
            request.BillAddress.PostalCode.SetValue(add.Postal?.Length < maxLength ? add.Postal : "");

            maxLength = Convert.ToInt32(request.BillAddress.State.GetMaxLength());
            request.BillAddress.State.SetValue(add.State?.Length < maxLength ? add.State : "");

            maxLength = Convert.ToInt32(request.BillAddress.Note.GetMaxLength());
            request.BillAddress.Note.SetValue(add.Type?.Length < maxLength ? add.Type : "");
        }

        if (person.PhoneNumbers != null && person.PhoneNumbers.Any())
        {
            var ph1 = person.PhoneNumbers[0].Number;
            if (ph1 != null)
            {
                maxLength = Convert.ToInt32(request.Phone.GetMaxLength());
                if (ph1.Length < maxLength)
                {
                    request.Phone.SetValue(ph1);
                }
                else
                {
                    var phs = ph1.DivideIntoEqualParts(maxLength);
                    if (phs.Count > 0)
                    {
                        request.Phone.SetValue(phs[0]);
                    }

                    if (phs.Count > 1)
                    {
                        request.AltPhone.SetValue(phs[1]);
                    }
                }

                maxLength = Convert.ToInt32(request.Contact.GetMaxLength());
                if (ph1.Length < maxLength)
                {
                    request.Contact.SetValue(ph1);
                }

                maxLength = Convert.ToInt32(request.Mobile.GetMaxLength());
                if (ph1.Length < maxLength)
                {
                    request.Mobile.SetValue(ph1);
                }
            }

            if (person.PhoneNumbers.Count > 1)
            {
                var ph2 = person.PhoneNumbers[1].Number;
                if (ph2 != null)
                {
                    maxLength = Convert.ToInt32(request.AltPhone.GetMaxLength());
                    if (ph2.Length < maxLength)
                    {
                        request.AltPhone.SetValue(ph2);
                    }

                    maxLength = Convert.ToInt32(request.AltContact.GetMaxLength());
                    if (ph2.Length < maxLength)
                    {
                        request.AltContact.SetValue(ph2);
                    }
                }
            }
        }

        if (person.PopStudent?.LoaStartDate != null)
        {
            request.JobStartDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent?.LoaStartDate != null)
        {
            request.JobProjectedEndDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent?.ExitDate != null)
        {
            request.JobEndDate.SetValue(person.PopStudent.ExitDate.Value);
        }

        request.IncludeRetElementList.Add("Name");
        request.IncludeRetElementList.Add("FirstName");
        request.IncludeRetElementList.Add("LastName");
        //request.IncludeRetElementList.Add("Fax");
        request.IncludeRetElementList.Add("ListID");
    }


    public void BuildGetAllQbCustomersRequest(IMsgSetRequest requestMsgSet)
    {
        requestMsgSet.ClearRequests();
        var request = requestMsgSet.AppendCustomerQueryRq();
        request.IncludeRetElementList.Add("Name");
        request.IncludeRetElementList.Add("FirstName");
        request.IncludeRetElementList.Add("LastName");
        //request.IncludeRetElementList.Add("Fax");
        request.IncludeRetElementList.Add("ListID");
        //request.IncludeRetElementList.Add("DataExtRet");
        //request.OwnerIDList.Add("0");
    }
}