using Populi;
using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PopuliQB1
{
    internal class AddEntityOperat
    {
        class Addr
        {
            public string addr;
        };

        // Search student in QBD by basic parameters: FullName, Email, Phone Number, Billing Address
        // if at least one parameter will match then we consider students as the same
        static public Customer SearchStudent(Person student, List<Customer> customerList)
        {
            var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            var fullName = student.last_name + ", " + student.first_name;
            // check for student existance in QBD, searching by name
            var customer = customerList.Where(e => e.FullName == fullName).FirstOrDefault();
            if (customer != null)
            {
                return customer;
            }

            var emailsLine = ConcatEmails(student.email_addresses);
            // find by email
            customer = customerList.Where(e => e.Email == emailsLine).FirstOrDefault();
            if (customer != null)
                return customer;

            // phone numbers - if at least one number will match we consider as the same
            foreach (var phoneNumber in student.phone_numbers)
            {
                var pattr = "[^0-9.]";
                var popNum = Regex.Replace(phoneNumber.number, pattr, "");
                try
                {
                    switch (phoneNumber.type)
                    {
                        case "work":
                            {
                                // search by Work Phone
                                customer = customerList.Where(e => Regex.Replace(e.WorkPhone ?? "", pattr, "") == popNum).FirstOrDefault();
                                if (customer != null)
                                    return customer;

                                // search by Main Phone
                                customer = customerList.Where(e => Regex.Replace(e.MainPhone ?? "", pattr, "") == popNum).FirstOrDefault();
                                if (customer != null)
                                    return customer;
                                break;
                            }
                        case "home":
                            // search by Home Phone
                            customer = customerList.Where(e => Regex.Replace(e.HomePhone ?? "", pattr, "") == popNum).FirstOrDefault();
                            if (customer != null)
                                return customer;
                            break;
                        case "mobile":
                            // search by Mobile Phone
                            customer = customerList.Where(e => Regex.Replace(e.Mobile ?? "", pattr, "") == popNum).FirstOrDefault();
                            if (customer != null)
                                return customer;
                            break;
                        case "other":
                            // other can be as a main
                            if (phoneNumber.primary)
                            {
                                customer = customerList.Where(e => Regex.Replace(e.MainPhone ?? "", pattr, "") == popNum).FirstOrDefault();
                                if (customer != null)
                                    return customer;
                            }

                            // search by Alt. Phone
                            customer = customerList.Where(e => Regex.Replace(e.AltPhone ?? "", pattr, "") == popNum).FirstOrDefault();
                            if (customer != null)
                                return customer;

                            // search by Alt. Mobile
                            customer = customerList.Where(e => Regex.Replace(e.AltMobile ?? "", pattr, "") == popNum).FirstOrDefault();
                            if (customer != null)
                                return customer;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0} {1}", methodName, ex.Message);
                }
            }

            // Billing Address
            foreach (var cust in customerList)
            {
                try
                {
                    var addr = cust.BillAddr.Addr1 + (cust.BillAddr.Addr2 ?? "") + (cust.BillAddr.Addr3 ?? "");
                    // replace spaces and \n symbols
                    var pattr = "{ 1, }\n { 1,}|\n { 1,}| { 1,}\n | { 2,}|\n";
                    var addrConv = Regex.Replace(addr, pattr, " ");
                    var studAddrConv = Regex.Replace(student.report_data.primary_address_street ?? "", pattr, " ");
                    if (addrConv.Contains(studAddrConv) && cust.BillAddr.City == student.report_data.primary_address_city
                        && cust.BillAddr.State == student.report_data.primary_address_state)
                        return cust;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0} {1}", methodName, ex.Message);
                }
            }

            return null;
        }

        // Concatenate emails to one string
        static public string ConcatEmails(List<EmailAddr> emails)
        {
            var multEmailsStr = "";
            foreach (var item in emails)
                multEmailsStr += item.email + ",";

            // remove last ','
            if (multEmailsStr.Length > 0)
                multEmailsStr = multEmailsStr.Remove(multEmailsStr.Length - 1);

            return multEmailsStr;
        }
    }
}
