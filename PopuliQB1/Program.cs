//using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Populi;

namespace PopuliQB1
{
    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);

            Person student = new Person();
            student.first_name = "Colmenares";
            student.last_name = "Andrés";
            //EmailAddr email_addr1 = new EmailAddr();
            //email_addr1.email = "first1q@gmail.com";
            //student.email_addresses = new List<EmailAddr>();
            //student.email_addresses.Add(email_addr1);
            //EmailAddr email_addr2 = new EmailAddr();
            //email_addr2.email = "second@gmail.com";
            //student.phone_numbers = new List<PhoneNumber>();  
            //var phoneNum = new PhoneNumber();
            //phoneNum.number = "3244";
            //phoneNum.type = "mobile";
            //student.phone_numbers.Add(phoneNum);
            //Customer cust = qBAccess.GetCustomerByListID("80002F62-1700352166");

            //var custIDs = new CustomerIDs();
            //qBAccess.CreateCustomerXML(student, ref custIDs, 0);
            //qBAccess.UpdateCustomerXML("", "", student);
            //student.email_addresses.Add(email_addr2);
            //qBAccess.CreateCustomerXML(student);
            //qBAccess.GetCustomers();
            //qBAccess.CreateCustomer(student);
            //qBAccessor.GetCustomers();
            //PopuliAccessor popAccessor = new PopuliAccessor();
            //var invCont = popAccessor.GetInvoices();
            //popAccessor.GetPersons();
            //var sync = new Sync();
            //sync.PopuliQBSync();
            //// DB
            using (var ctx = new PopuliQBSyncEntities1())
            {
                var studDBList = ctx.Students.ToList();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
