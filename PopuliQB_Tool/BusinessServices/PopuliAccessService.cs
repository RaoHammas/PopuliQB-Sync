using System.Configuration;
using System.IO;
using System.Net;
using System.Reflection;
using PopuliQB_Tool.BusinessObjects;

namespace PopuliQB_Tool.BusinessServices;

public class PopuliAccessService
{
    /*private readonly string dev_url = "https://divinemercyedu-validation.populi.co/api2/";
    private readonly string prod_url = "https://divinemercyedu.populiweb.com/api2/";

    private readonly string auth_token =
        "sk_y75vZUhN4fP14zXrGcnl8ThHDiLf0xAdGVLWekgcbvgKN8KJHcEw7y0JOp8YlrZ4ZtNSRahQGnkK8dhHFsHNyG";

    private static readonly string? BProdVer = ConfigurationManager.AppSettings["prod_version"];

    private static readonly object LockObject = new();
    
    public List<Person> GetStudents(List<Person> allPersons)
    {
        var studList = allPersons.Where(p => p.student != null).ToList();
        return studList;
    }

    public ParsedPersons GetPersons()
    {
        ParsedPersons contPersons = null;
        bool more_pages = true;
        int page_num = 1;
        var tmpLs = new List<Person>();
        MainForm.statusBox.AddStatusMsg("Getting Persons from Populi...", StatusBox.MsgType.Info);
        var url = dev_url;
        if (BProdVer == "true")
            url = prod_url;

        url += "people";

        while (more_pages)
        {
            var request = WebRequest.Create(url);
            request.Headers.Add("Authorization", "Bearer " + auth_token);

            request.ContentType = "application/json";
            request.Method = "GET";

            var type = request.GetType();
            var currentMethod = type.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(request);

            var methodType = currentMethod.GetType();
            methodType.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(currentMethod, false);

            var json = @"{
                ""page"":" + page_num + "," +
                       @"""expand"": [""phone_numbers"", ""email_addresses"", ""student"", ""aid_authorizations"", ""deposits""]
                }";

            //request.Timeout = 1900;
            //request.Proxy = null;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 20;

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(json);
            }

            try
            {
#if !TEST_POP_FILE
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
#else
                    // Testing
                    string path = @"C:\Work\Populi_Prj\Tests\BillAddr.json";
                    string readText = File.ReadAllText(path);
                    Console.WriteLine(readText);
                    var responseString = readText;
#endif
                contPersons = JsonConvert.DeserializeObject<ParsedPersons>(responseString);
#if TEST_POP_FILE
                    tmpLs = tmpLs.Concat(contPersons.data).ToList();
                    break;
#endif
            }
            catch (Exception e)
            {
                var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine(DateTime.Now + " " + methodName + "(): Exception = " + e.Message);
            }

            if (contPersons == null)
                continue;
            more_pages = contPersons.has_more;
            page_num++;
            tmpLs = tmpLs.Concat(contPersons.data).ToList();

            MainForm.statusBox.ShowCountProcessedEntities(tmpLs.Count, contPersons.results);
        }

        contPersons.data = tmpLs;
        return contPersons;
    }

    static void GetPersonsByPage(int page, ref List<Person> retPersonList)
    {
        //var personList = new List<Person>();
        var url = dev_url;
        if (BProdVer == "true")
            url = prod_url;


        url += "people";
        var request = WebRequest.Create(url);

        request.Headers.Add("Authorization", "Bearer " + auth_token);

        request.ContentType = "application/json";
        request.Method = "GET";

        var type = request.GetType();
        var currentMethod = type.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(request);

        var methodType = currentMethod.GetType();
        methodType.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(currentMethod, false);

        var json = @"{
                ""page"":" + page + "," +
                   @"""expand"": [""phone_numbers"", ""email_addresses"", ""student"", ""aid_authorizations"", ""deposits""]
                }";

        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(json);
            streamWriter.Close();
        }

        request.GetResponse();

        ParsedPersons contPersons = new ParsedPersons();
        try
        {
            var response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine(response.StatusCode.ToString());
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            contPersons = JsonConvert.DeserializeObject<ParsedPersons>(responseString);
        }
        catch (Exception e)
        {
            var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(DateTime.Now + " " + methodName + "(): Exception = " + e.Message);
        }

        //MainForm.statusBox.ShowCountProcessedEntities(tmpLs.Count, contPersons.results);
        //retPersonList = contPersons.data;
        lock (LockObject)
        {
            Console.WriteLine("Count = " + contPersons.data.Count());
            //listPer.Add(contPersons.data);
            //contPersons.data.CopyTo(retPersonList.ToArray());
            foreach (var person in contPersons.data)
                retPersonList.Add(person);
        }
    }

    // gets Persons in multithreading
    public ParsedPersons GetPersons1()
    {
        ParsedPersons contPersons = null;
        bool more_pages = true;
        int page_num = 1;
        var tmpLs = new List<Person>();
        MainForm.statusBox.AddStatusMsg("Getting Persons from Populi...", StatusBox.MsgType.Info);
        var url = dev_url;
        if (BProdVer == "true")
            url = prod_url;

        url += "people";

        var request = WebRequest.Create(url);
        request.Headers.Add("Authorization", "Bearer " + auth_token);

        request.ContentType = "application/json";
        request.Method = "GET";

        var type = request.GetType();
        var currentMethod = type.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(request);

        var methodType = currentMethod.GetType();
        methodType.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(currentMethod, false);

        var json = @"{
                ""page"":" + page_num + "," +
                   @"""expand"": [""phone_numbers"", ""email_addresses"", ""student"", ""aid_authorizations"", ""deposits""]
                }";

        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(json);
            //streamWriter.Close();
        }

        try
        {
#if !TEST_POP_FILE
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
#else
                    // Testing
                    string path = @"C:\Work\Populi_Prj\Tests\BillAddr.json";
                    string readText = File.ReadAllText(path);
                    Console.WriteLine(readText);
                    var responseString = readText;
#endif
            contPersons = JsonConvert.DeserializeObject<ParsedPersons>(responseString);

            request.GetResponse();

#if TEST_POP_FILE
                    tmpLs = tmpLs.Concat(contPersons.data).ToList();
                    break;
#endif
        }
        catch (Exception e)
        {
            var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(DateTime.Now + " " + methodName + "(): Exception = " + e.Message);
        }


        //var request1 = WebRequest.Create(url);
        //request1.Headers.Add("Authorization", "Bearer " + auth_token);

        //request1.ContentType = "application/json";
        //request1.Method = "GET";

        //var type1 = request1.GetType();
        //var currentMethod1 = type1.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(request);

        //var methodType1 = currentMethod1.GetType();
        //methodType1.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(currentMethod, false);

        //listPer.Clear();
        //List<List<Person>> listPer = new List<List<Person>>();
        List<Thread> thrdArr = new();

        List<List<Person>> list = new();
        List<Person> retPersArr;
        for (int j = 0; j < contPersons.pages - 1; j++)
        {
            lock (LockObject1)
            {
                retPersArr = new List<Person>();
                Thread t = new Thread(() =>
                {
                    GetPersonsByPage(j + 2, ref retPersArr
                    );
                });
                thrdArr.Add(t);
                t.Start();
                list.Add(retPersArr);
            }

            Thread.Sleep(20);
            Console.WriteLine("Started threads = {0}", j + 1);
        }

        // launch each thread
        //int i = 0;
        //foreach (var thread in thrdArr)
        //{
        //    thread.Start();
        //    Thread.Sleep(100);
        //    Console.WriteLine("Started threads = {0}", ++i);
        //}

        // wait for all threads to complete
        int i = 0;
        foreach (var thread in thrdArr)
        {
            thread.Join();
            Console.WriteLine("Ended threads = {0}", ++i);
            MainForm.statusBox.ShowCountProcessedEntities((i + 1) * 200, contPersons.results);
        }

        //// get persons for undownloaded again
        //var cpyList = listPer.GetRange(0, listPer.Count);
        //for (int ii = 0; ii < cpyList.Count; ii++)
        //{
        //    var ls = new List<Person>();
        //    if (cpyList[ii].Count < 200)
        //        GetPersonsByPage(ii + 2, ref ls);
        //}

        //// delete undownloaded from list
        //int deleted = 0;
        //for (int ii = 0; ii < cpyList.Count; ii++)
        //{
        //    if (cpyList[ii].Count < 200 || cpyList[ii].Count == 0)
        //    {
        //        listPer.RemoveAt(ii - deleted);
        //        deleted++;
        //    }
        //}

        // concat all pages into one object
        //foreach (var onePage in listPer)
        //    contPersons.data = contPersons.data.Concat(onePage).ToList();

        foreach (var page in list)
            contPersons.data = contPersons.data.Concat(page).ToList();

        //listPer.Clear();

        //tmpLs = tmpLs.Concat(contPersons.data).ToList();
        MainForm.statusBox.ShowCountProcessedEntities(tmpLs.Count, contPersons.results);

        return contPersons;
    }

    public static List<Person> SelectPersonsByDate(List<Person> personList, DateTime date)
    {
        var selPersonList = new List<Person>();
        //foreach (var person in personList)
        //{
        //    if (person.added_at >= date)
        //    { 
        //        selPersonList.Add(person); 
        //    }
        //}
        return personList.Where(x => x.added_at >= date).ToList();
    }

    public ParsedInvoices GetInvoices()
    {
        ParsedInvoices contInvoices = null;
        bool more_pages = true;
        int page_num = 1;
        var tmpLs = new List<Invoice>();
        MainForm.statusBox.AddStatusMsg("Getting Invoices from Populi...", StatusBox.MsgType.Info);
        var url = dev_url;
        if (BProdVer == "true")
            url = prod_url;

        url += "invoices";

        while (more_pages)
        {
            var request = WebRequest.Create(url);
            request.Headers.Add("Authorization", "Bearer " + auth_token);

            request.ContentType = "application/json";
            request.Method = "GET";

            var type = request.GetType();
            var currentMethod = type.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(request);

            var methodType = currentMethod.GetType();
            methodType.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(currentMethod, false);

            var json = @"{
                ""page"":" + page_num +
                       "}";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(json);
            }

            try
            {
#if !TEST_POP_FILE
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
#else
                    // Testing
                    string path = @"C:\Work\Populi_Prj\Tests\InvoiceCreate.json";
                    string readText = File.ReadAllText(path);
                    Console.WriteLine(readText);
                    var responseString = readText;
#endif
                contInvoices = JsonConvert.DeserializeObject<ParsedInvoices>(responseString);
#if TEST_POP_FILE
                    tmpLs = tmpLs.Concat(contInvoices.data).ToList();
                    break;
#endif
            }
            catch (Exception e)
            {
                var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine(DateTime.Now + " " + methodName + "(): Exception = " + e.Message);
            }

            more_pages = contInvoices.has_more;
            page_num++;
            tmpLs = tmpLs.Concat(contInvoices.data).ToList();
            MainForm.statusBox.ShowCountProcessedEntities(tmpLs.Count, contInvoices.results);
        }

        contInvoices.data = tmpLs;
        return contInvoices;
    }

    public static List<Invoice> SelectInvoicesByDate(List<Invoice> invoiceList, DateTime date)
    {
        var selInvList = new List<Invoice>();
        return invoiceList.Where(x => x.posted_on >= date).ToList();
    }*/
}