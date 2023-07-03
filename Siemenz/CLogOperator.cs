using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7.Communications.Siemenz
{
    public class CLogOperator
    {
        private List<CLogInfo> _lsLogs = new List<CLogInfo>();
        protected void addLog(byte[] content, string description, bool isReceived)
        {
            _lsLogs.Add(new CLogInfo(DateTime.Now.Ticks, content, description, isReceived));
        }

        protected void addLog(byte[] content, int index, int count, string description, bool isReceived)
        {
            byte[] des = new byte[count];
            Array.Copy(content, index, des, 0, count);
            _lsLogs.Add(new CLogInfo(DateTime.Now.Ticks, des, description, isReceived));
        }

        protected void addLogString(string content, string description, bool isReceived)
        {
            _lsLogs.Add(new CLogInfo(DateTime.Now.Ticks, content, description, isReceived));
        }

        public List<CLogInfo> getLog()
        {
            return _lsLogs;
        }

        public void reAllocateLog(int capacity)
        {
            _lsLogs = new List<CLogInfo>(capacity);
        }

        public void clearLog()
        {
            _lsLogs.Clear();
        }

        public virtual void saveLog(string title)
        {
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "log");
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "log/" + DateTime.Now.ToString("yyyy年MM月dd日") + ".txt", true, Encoding.Default))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "  " + title);
                outputLog(_lsLogs, sw);
                sw.WriteLine();
                sw.WriteLine();
            }
        }

        public virtual void outputLog(List<CLogInfo> logs, StreamWriter sw)
        {
            foreach (var log in logs)
            {
                DateTime logTime = new DateTime(log.ticks);
                StringBuilder sbData = new StringBuilder();
                if (log.data != null)
                {
                    /* 16进制的内容 */
                    foreach (var b in log.data)
                    {
                        string word = Convert.ToString(b, 16).ToUpper();
                        if (word.Length != 2)
                            word = "0" + word;
                        sbData.Append(word);
                        sbData.Append(" ");
                    }
                }
                else if (log.dataStr != null)
                {
                    /* string 内容 */
                    sbData.Append(log.dataStr);
                }
                sw.WriteLine(logTime.ToString("HH:mm:ss.fff") + (log.isRecv ? " << " : " >> ") + sbData.ToString() + (log.error == null ? "" : ("//" + log.error)));
            }
        }
    }
}
