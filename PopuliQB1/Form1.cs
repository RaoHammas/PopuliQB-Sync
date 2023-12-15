using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Populi;
using System.Configuration;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace PopuliQB1
{
    public partial class MainForm : Form
    {
        private QBDAccessor qBAccessor;
        static public StatusBox statusBox = null;

        public MainForm()
        {
            qBAccessor = new QBDAccessor();
            InitializeComponent();
            this.Text = "Populi To QuickBooks Sync v. " + Utils.GetVersion();
            statusBox = new StatusBox(this.rtbStatus);
            btPopQBSync.Enabled = false;
        }

        private void btPopQBSync_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("Populi to QBD Sync Utility. Version {0}", Utils.GetVersion());
            var popQBSync = new Sync();
            List<string?> listID = new List<string?>();
            //listID.Add("8000000A-1543833601");
            listID.Add("800030B9-1700435003"); // cust: "Test, Test"
            //var invLst = qBAccessor.GetInvoices(listID);
            Populi.Invoice inv = new Populi.Invoice();
            var item1 = new Item();
            item1.name = "CLINIC";
            item1.amount = 100.0;
            inv.items.Add(item1);
            var item2 = new Item();
            item2.name = "App Fee";
            item2.amount = 180.5; 
            inv.items.Add(item2);
            var pattr = "@[^0-9a-zA]";
            //var inpStr = "Hell   \n  wor\nl";
            var custList = qBAccessor.GetCustomers();
            var popAccessor = new PopuliAccessor();
            //var studList = popAccessor.GetPersons();
            //var studUniq = studList.data.Select(x => x.id).Distinct().ToList();
            var studtList = popAccessor.GetPersons1();
            var fsNames = studtList.data.Select(/*x => x.last_name + " " +  x.first_name + " " +*/ x => x.id).Distinct().ToList();
            var selVal = dtStartTxn.Value;
            string? dateStr = "11/10/2023";
            DateTime startDate = new DateTime();
            startDate = DateTime.Parse(dateStr);
            //var start = DateTime.Now;
            //popAccessor.GetPersons1();
            //var end = DateTime.Now;
            //var diff = end - start;
            //var studSelList = PopuliAccessor.SelectPersonsByDate(studList.data, selVal);

            //var item = qBAccessor.GetItem("Spring 2019 Student Loan Refund");
            //var type = item.ortype;
            //if (type == ENORItemRet.orirItemNonInventoryRet)
            //{
            //    IItemNonInventoryRet itemNonInv = item.ItemNonInventoryRet;
            //    var name = itemNonInv.FullName.GetValue();
            //    var price = itemNonInv.ORSalesPurchase.SalesOrPurchase.ORPrice.Price.GetValue();
            //    for (var i = 0; i < itemNonInv.DataExtRetList.Count; i++)
            //    {
            //        var extData = itemNonInv.DataExtRetList.GetAt(i);
            //        var extName = extData.DataExtName.GetValue();
            //        var extVal = extData.DataExtValue.GetValue();
            //    }
            //}

            //var itemNew = qBAccessor.CreateItem("Test Item");

            //var name = item.ItemPaymentRet.Name.GetValue();

            //qBAccessor.UpdateInvoice(invLst[0], inv);
            popQBSync.PopuliQBSync(dtStartTxn.Value);
            //var popAccessor = new PopuliAccessor();
            //var invList = popAccessor.GetInvoices();
            //popQBSync.CreateAndSaveInvoice(invList.data[0]);

            statusBox.AddStatusMsg("Synching invoices...", StatusBox.MsgType.Info);
            //popQBSync.SyncInvoices();
            statusBox.AddStatusMsg("Synching completed.", StatusBox.MsgType.Info);
        }

        private void btConnectQB_Click(object? sender, EventArgs e)
        {
            try
            {
                var popAccess = new PopuliAccessor();

                if (qBAccessor.ClosedConnection())
                    qBAccessor.OpenConnection();

                tbQBCompanyName.Text = qBAccessor.GetCompanyName();
                this.btPopQBSync.Enabled = true;
                statusBox.AddStatusMsg("Connected to QuickBooks.", StatusBox.MsgType.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                this.btPopQBSync.Enabled = false;
                statusBox.AddStatusMsg("QuickBooks isn't running.", StatusBox.MsgType.Err);
                qBAccessor.CloseConnection();
                statusBox.AddStatusMsg("QuickBooks isn't running.", StatusBox.MsgType.Info);
            }

        }
    }
}
