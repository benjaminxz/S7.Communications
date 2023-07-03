using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7.Communications.Siemenz
{
    public class CLogInfo
    {
        public CLogInfo(long ticks_, byte[] data_, string error_, bool isRecv_)
        {
            ticks = ticks_;
            data = data_;
            dataStr = null;
            error = error_;
            isRecv = isRecv_;
        }

        public CLogInfo(long ticks_, string dataStr_, string error_, bool isRecv_)
        {
            ticks = ticks_;
            data = null;
            dataStr = dataStr_;
            error = error_;
            isRecv = isRecv_;
        }

        public byte[] data;
        public string dataStr;
        public string error;
        public bool isRecv;
        public long ticks;
    }
}
