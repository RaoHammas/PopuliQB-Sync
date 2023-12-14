using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopuliQB1
{
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

    // Controls status box log messages
    public class StatusBox
    {
        public enum MsgType
        {
            Info,
            Err
        }

        private RichTextBox statusLog;
        private System.Drawing.Color col;
        private bool bShownCountMesAlready;

        public StatusBox(RichTextBox txBoxStatus) 
        { 
            statusLog = txBoxStatus;
            bShownCountMesAlready = false;
        }

        public void TestMes()
        {
            var mes = "Hello: ";
            //RichTextBoxExtensions.AppendText(statusLog, mes, System.Drawing.Color.Black);
            AddStatusMsg(mes, MsgType.Info);
            statusLog.AppendText("123");
            //RichTextBoxExtensions.AppendText(statusLog, "123", System.Drawing.Color.Black);
            //statusLog.Text = statusLog.Text.Remove(statusLog.Text.Length - 3, 3);
            ////statusLog.AppendText("124");
            //RichTextBoxExtensions.AppendText(statusLog, "124", System.Drawing.Color.Black);
            statusLog.SelectionStart = statusLog.Text.Length - 3;
            statusLog.SelectionLength = 3;
            statusLog.SelectedText = "124";
            //statusLog.AppendText("124");
            //statusLog.SelectedText.Replace(statusLog.SelectedText, "");
            //SendKeys.Send("DELETE");
        }

        public void ShowCountProcessedEntities(int processedCount, int totalCount)
        {
            if (!bShownCountMesAlready)
            {
                var headMes = "Count processed Entities: ";
                AddStatusMsg(headMes, MsgType.Info);
            }

            int number = 23;
            string sProcCount = string.Format("{0:d6}", processedCount);
            string sTotalCount = string.Format(" of{0:d} total.", totalCount);

            if (!bShownCountMesAlready)
            {
                statusLog.AppendText(sProcCount);
                statusLog.AppendText(sTotalCount);
            }
            else
            {
                // replace old count with a new one
                statusLog.SelectionStart = statusLog.Text.Length - sProcCount.Length - sTotalCount.Length;
                statusLog.SelectionLength = sProcCount.Length;
                statusLog.SelectedText = sProcCount;
            }

            statusLog.Refresh();
            bShownCountMesAlready = true;
        }

        public void AddStatusMsg(string mes, MsgType msgType)
        {
            if (statusLog.Text != "")
                RichTextBoxExtensions.AppendText(statusLog, "\r\n", System.Drawing.Color.Black);

            if (msgType == MsgType.Err)
                RichTextBoxExtensions.AppendText(statusLog, "Error: ", System.Drawing.Color.Red);
            else
                RichTextBoxExtensions.AppendText(statusLog, "Info: ", System.Drawing.Color.Green);

            RichTextBoxExtensions.AppendText(statusLog, mes, System.Drawing.Color.Black);
        }
    }
}
