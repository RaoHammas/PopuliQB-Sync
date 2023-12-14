using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Populi;
using QBFC16Lib;
using static System.Net.Mime.MediaTypeNames;

namespace PopuliQB1
{
    internal class Sync
    {
        private PopuliAccessor popAccessor = new PopuliAccessor();
        private QBDAccessor QBDAccess = new QBDAccessor();
        private List<Person> studentsList;
        private string dateStr = "11/10/2023";
        //private DateTime startDate;

        public void PopuliQBSync(DateTime startDate)
        {
            MainForm.statusBox.AddStatusMsg("Synching students...", StatusBox.MsgType.Info);
            SyncStudents(startDate);
            MainForm.statusBox.AddStatusMsg("Synching students completed.", StatusBox.MsgType.Info);
            MainForm.statusBox.AddStatusMsg("Synching invoices...", StatusBox.MsgType.Info);
            SyncInvoices(startDate);
            MainForm.statusBox.AddStatusMsg("Synching invoices completed.", StatusBox.MsgType.Info);
        }

        public void SyncStudents(DateTime startDate)
        {
            var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var studentsCnt = popAccessor.GetPersons1();
            studentsList = popAccessor.GetStudents(studentsCnt.data);
            var customersQB = QBDAccess.GetCustomers();
            
            //DateTime startDates = new DateTime();
            //startDates = DateTime.Parse(dateStr);
            studentsList = PopuliAccessor.SelectPersonsByDate(studentsList, startDate);
            Console.WriteLine("Students Count = {0}", studentsList.Count());

            int countProcessedCust = 0;
            foreach (var student in studentsList)
            {
                var fullName1 = student.last_name + ", " + student.first_name;
                Console.WriteLine("Student name: " + fullName1);

                // search in DB
                using (var ctx = new PopuliQBSyncEntities1())
                {
                    var studDBList = ctx.Students.ToList();
                    var foundStud = studDBList.Where(p => p.PopId == student.id).FirstOrDefault();
                    if (foundStud != null)
                    {
                        // found - will update if need
                        var rsUpdate = QBDAccess.UpdateCustomerXML(foundStud.QBId, student);
                        if (!rsUpdate)
                            Console.WriteLine(DateTime.Now + " " + methodName + "(): Error Customer wasn't updated.");
                    }
                    else
                    {
                        // doesn't exist - will search in QBD
                        var fullName = student.last_name + ", " + student.first_name;
                        var foundCustomer = AddEntityOperat.SearchStudent(student, customersQB);

                        // found
                        if (foundCustomer != null)
                        { 
                            // it is a duplicate information
                            // count how much duplicates
                            var duplicates = studDBList.Where(p => p.StudName.Contains(fullName)).ToList();
                            var maxDupNo = duplicates.Max(p => p.DupNo);
                            int curDupNo = 0;
                            var custIDs = new CustomerIDs();
                            if (maxDupNo != null)
                            {
                                curDupNo = (int)maxDupNo + 1;
                                var res = QBDAccess.CreateCustomerXML(student, ref custIDs, curDupNo);
                                // save to DB
                                if (res)
                                    DBAccessor.SaveStudent(student, custIDs, curDupNo);
                            }
                            else
                            {
                                // to do link between new created in Populi and existing in QBD
                                // there is no dups still
                                custIDs.CopyFromCustomer(foundCustomer);
                                DBAccessor.SaveStudent(student, custIDs, 0);
                            }
                        }
                        else
                        {
                            // not found
                            var custIDs = new CustomerIDs();
                            // otherwise - create a new one
                            var res = QBDAccess.CreateCustomerXML(student, ref custIDs, 0);
                            // save to DB
                            if (res)
                                DBAccessor.SaveStudent(student, custIDs, 0);
                        }
                    }
                }

                countProcessedCust++;
                Console.WriteLine("{0} Students of {1} processed.", countProcessedCust, studentsList.Count());
            }
        }

        public void SyncInvoices(DateTime startDate)
        {
            QBDAccessor qbAccessor = new QBDAccessor();
            Populi.PopuliAccessor popAccessor = new PopuliAccessor();
            var popInvoices = popAccessor.GetInvoices();
            DateTime startDates = new DateTime();
            startDates = DateTime.Parse(dateStr);
            var invoicesSel = PopuliAccessor.SelectInvoicesByDate(popInvoices.data, startDate);
            Console.WriteLine("Invoices Count = {0}", invoicesSel.Count);

            // search in DB
            using (var ctx = new PopuliQBSyncEntities1())
            {
                var studDBList = ctx.Students.ToList();
                var invDBList = ctx.Invoices.ToList();
                // get all invoices from QBD for all studens in DB by its ListID
                var studDBListIDs = studDBList.Select(x => x.QBId).ToList();
                var invLst = qbAccessor.GetInvoices(studDBListIDs);
                Console.WriteLine("Students Count = {0}", studDBList.Count());

                // go throughout each student in DB, that was saved in Sync students step
                int studCount = 0;
                foreach (var stud in studDBList)
                {
                    // get all invoices for this student from Populi 
                    var popInvoiceList = invoicesSel.Where(x => x.report_data.studentid == stud.PopId).ToList();
                    Console.WriteLine("Processing invoices for Student name: " + stud.StudName);
                    // get Invoices from DB by Student Id
                    var invOfStudLst = invDBList.Where(p => p.StudPopId == stud.PopId).ToList();
                    // if found - get them from QB to update if need
                    if (invOfStudLst.Count() > 0)
                    {
                        foreach (var invoice in invOfStudLst)
                        {
                            // get Invoice from QBD by its TxnID
                            var qBInvoice = invLst.Where(x => x.TxnID == invoice.QBTxnId).FirstOrDefault();
                            // get Invoice from Populi by its id
                            var popInvoice = invoicesSel.Where(x => x.id == invoice.PopId).FirstOrDefault();
                            // TODO - add compare invoices for update
                            // update invoice in QBD
                            if (qBInvoice == null)
                            {
                                Console.WriteLine("Can't update invoice PopId = {0}" +
                                    " InvNo = {1}. TxnID = {2} not found in QuickBooks.", invoice.PopId, invoice.InvNo, invoice.QBTxnId
                                    );
                            }
                            else
                            {
                                var rsUpdate = qbAccessor.UpdateInvoice(qBInvoice, popInvoice);
                                // update db
                            }
                        }

                        // create all Populi invoices that don't exist in DB still
                        CreateInvoicesNonFoundInDB(popInvoiceList, invOfStudLst);
                    }
                    else
                    {
                        // not found in DB - will search in QB all invoices for this Student by StudId
                        // there is no in Invoice table yet
                        // get all invoices for this student from QB
                        var studInvList = invLst.Where(x => x.CustomerListID == stud.QBId).ToList();
                        // go throughout all Populi invoices, and search matching in QBD
                        foreach (var popInv in popInvoiceList)
                        {
                            var foundQBInvList = SearchInvoiceInQB(popInv, studInvList);
                            if (foundQBInvList.Count() > 0)
                            {
                                // write to DB first found invoice
                                DBAccessor.SaveInvoice(foundQBInvList.FirstOrDefault(), popInv);
                            } 
                            else 
                                CreateAndSaveInvoice(popInv);   // create a new one then
                        }
                    }
                    studCount++;
                    Console.WriteLine("Processed invoices for {0} Students of {1}.", studCount, studDBList.Count());
                }
            }
        }

        // Creates invoices in QBD that weren't found in DB, but exist in Populi
        private void CreateInvoicesNonFoundInDB(List<Populi.Invoice> popInvLst, List<Invoice> dbInvLst)
        {
            var popIdLst = popInvLst.Select(x => x.id).ToList();
            var dbIdLst = dbInvLst.Select(x => x.PopId.Value).ToList();
            var nonExistDbLst = popIdLst.Except(dbIdLst);
            foreach (var popId in nonExistDbLst)
            {
                // get Populi invoice by its Id
                var popInvoice = popInvLst.Where(x => x.id == popId).FirstOrDefault();
                CreateAndSaveInvoice(popInvoice);
            }
        }

        // Creates Populi Invoice in QBD and saves in SQL DB
        public void CreateAndSaveInvoice(Populi.Invoice popInvoice)
        {
            // create it invoice in QBD
            var qbAccessor = new QBDAccessor();
            var newInvoice = qbAccessor.CreateInvoice(popInvoice);
            // save to DB
            var qbInvoice = new QBInvoice();
            qbInvoice.CreateFromQBRet(newInvoice);
            DBAccessor.SaveInvoice(qbInvoice, popInvoice);
        }

        // Searches the same invoices by Populi invoice in QBD invoices list (by selected StudentID) 
        private List<QBInvoice> SearchInvoiceInQB(Populi.Invoice findInvoice, List<QBInvoice> qbInvoices)
        {
            var foundInv = new List<QBInvoice>();
            // find first by amount
            // go through QBD invoices
            foreach (var invoice in qbInvoices)
            {
                // go through items
                if (invoice.Items.Count() == findInvoice.items.Count())
                {
                    int i = 0;
                    foreach (var item in findInvoice.items)
                    {
                        var foundItem = invoice.Items.Where(x => x.Name == item.name && x.Amount == item.amount).FirstOrDefault();
                        // if at least one item don't match then invoices don't match
                        if (foundItem == null) 
                            break;
                        i++;
                    }

                    // all items are matching - invoice found
                    if (i == findInvoice.items.Count())
                        foundInv.Add(invoice);
                }
            }

            return foundInv;
        }
    }
}
