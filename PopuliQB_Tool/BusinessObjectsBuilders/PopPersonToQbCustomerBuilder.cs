using QBFC16Lib;
using PopuliQB_Tool.BusinessObjects;

namespace PopuliQB_Tool.BusinessObjectsBuilders;

public class PopPersonToQbCustomerBuilder
{
    public void BuildCustomerAddRequest(IMsgSetRequest requestMsgSet, PopPerson person)
    {
        var customerAddRq = requestMsgSet.AppendCustomerAddRq();

        customerAddRq.ClassRef.ListID.SetValue(person.Id.ToString());
        customerAddRq.Name.SetValue($"{person.FirstName} {person.LastName}");
        customerAddRq.Salutation.SetValue(person.Prefix?.ToString());
        customerAddRq.FirstName.SetValue(person.FirstName);
        customerAddRq.MiddleName.SetValue(person.MiddleName);
        customerAddRq.LastName.SetValue(person.LastName);
        customerAddRq.JobTitle.SetValue("");
        customerAddRq.IsActive.SetValue(true);
        customerAddRq.CompanyName.SetValue("");
        customerAddRq.JobTitle.SetValue("");
        // customerAddRq.Email.SetValue(person.NotificationEmailId.ToString());

        if (person.Addresses.Any())
        {
            var add = person.Addresses[0];
            customerAddRq.BillAddress.Addr1.SetValue(add.Street);
            customerAddRq.BillAddress.City.SetValue(add.City);
            customerAddRq.BillAddress.Country.SetValue(add.Country);
            customerAddRq.BillAddress.PostalCode.SetValue(add.Postal);
            customerAddRq.BillAddress.State.SetValue(add.State);
            customerAddRq.BillAddress.Note.SetValue(add.Type);

            customerAddRq.ShipAddress.Addr1.SetValue(add.Street);
            customerAddRq.ShipAddress.City.SetValue(add.City);
            customerAddRq.ShipAddress.Country.SetValue(add.Country);
            customerAddRq.ShipAddress.PostalCode.SetValue(add.Postal);
            customerAddRq.ShipAddress.State.SetValue(add.State);
            customerAddRq.ShipAddress.Note.SetValue(add.Type);

            var shipToAddress = customerAddRq.ShipToAddressList.Append();
            shipToAddress.Addr1.SetValue(add.Street);
            shipToAddress.City.SetValue(add.City);
            shipToAddress.Country.SetValue(add.Country);
            shipToAddress.PostalCode.SetValue(add.Postal);
            shipToAddress.State.SetValue(add.State);
            shipToAddress.Note.SetValue(add.Type);
            shipToAddress.DefaultShipTo.SetValue(true);
        }

        if (person.PhoneNumbers.Any())
        {
            customerAddRq.Phone.SetValue(person.PhoneNumbers[0].Number);
            customerAddRq.Contact.SetValue(person.PhoneNumbers[0].Number);

            if (person.PhoneNumbers.Count > 1)
            {
                customerAddRq.AltPhone.SetValue(person.PhoneNumbers[1].Number);
                customerAddRq.AltContact.SetValue(person.PhoneNumbers[1].Number);
            }
        }

        if (person.PopStudent.LoaStartDate != null)
        {
            customerAddRq.JobStartDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent.LoaStartDate != null)
        {
            customerAddRq.JobProjectedEndDate.SetValue(person.PopStudent.LoaStartDate.Value);
        }

        if (person.PopStudent.ExitDate != null)
        {
            customerAddRq.JobEndDate.SetValue(person.PopStudent.ExitDate.Value);
        }
    }
}