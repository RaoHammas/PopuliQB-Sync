using Populi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopuliQB1
{
    internal class DBAccessor
    {
        static public void SaveStudent(Populi.Person student, CustomerIDs custIDs, int dupNo)
        {
            using (var ctx = new PopuliQBSyncEntities1())
            {
                try
                {
                    var stud = new Student();
                    stud.PopId = student.id;
                    var fullName = student.last_name + ", " + student.first_name;
                    stud.StudName = fullName;
                    stud.QBId = custIDs.ListID;
                    stud.QBEditSeq = custIDs.EditSeq;
                    stud.DupNo = dupNo;
                    ctx.Students.Add(stud);
                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        static public void SaveInvoice(QBInvoice invoice, Populi.Invoice popInvoice)
        {
            using (var ctx = new PopuliQBSyncEntities1())
            {
                try
                {
                    var invSave = new Invoice();
                    invSave.QBTxnId = invoice.TxnID;
                    invSave.InvNo = popInvoice.number;
                    invSave.FirstItemName = popInvoice.items[0].name;
                    invSave.PopId = popInvoice.id;
                    invSave.Amount = (decimal)popInvoice.amount;
                    invSave.StudPopId = popInvoice.report_data.studentid;
                    ctx.Invoices.Add(invSave);
                    ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
