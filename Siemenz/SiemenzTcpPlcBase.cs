using S7.Net;
using System.ComponentModel.DataAnnotations;
using DataType = S7.Net.DataType;

namespace S7.Communications.Siemenz
{
    public class SiemenzTcpPlcBase : CLogOperator
    {
        public int CommunicationIntervalMSec { get; set; }// 发送指令前的间隔时间间隔
        public int RepeatCnt { get; set; } //重发次数
        public bool LogDescription { get; set; } //是否log 描述
        public StatusLogRules StatusLogRule { get; set; }

        private long _lastCommunicationTimeval = -1;
        private Plc _plc;
        private volatile bool _processing = false;
        private byte[] _lastStatuesBytes;

        /// <summary>
        /// 状态读取通讯 log规则，状态读取在log中占空间较多，
        /// </summary>
        public enum StatusLogRules
        {
            LogAll,//记录每次完整数据
            LogOnlyOnChange,//变化时记录一次完整数据
            LogChangeAndSimpleSameReceive,//变化时记录完整数据,不变化时简单记录
            LogNothing, //不记录
        }

        public SiemenzTcpPlcBase(CpuType cpuType, string ip, short rack, short slot)
        {
            _plc = new Plc(cpuType, ip, rack, slot);
            CommunicationIntervalMSec = 100;
            RepeatCnt = 3;
            LogDescription = false;
            StatusLogRule = StatusLogRules.LogAll;
        }

        public bool isConnected()
        {
            return _plc.IsConnected;
        }

        /// <summary>
        /// 连接PLC，如果连接失败会抛出异常
        /// </summary>
        /// <param name="connectCnt"></param>
        public void connect(int connectCnt)
        {
            for (int i = 0; i < connectCnt; i++)
            {
                try
                {
                    _plc.Open();
                }
                catch
                {
                    if (i == connectCnt - 1)
                        throw;
                    else
                        continue;
                }
            }
        }

        public void disconnect()
        {
            _plc.Close();
        }

        #region 内部方法

        private void wait()
        {
            try
            {
                for (int i = 0; i < 50; i++)
                {
                    // 在多个线程同时访问 wait 方法时  等待一下  因为时间可能会被2个线程同步刷新，不过这个方法也有局限性 有极限等待时间 ，有可能时间顺序混乱，真正完美的方法是用队列
                    // 建议通讯线程最多为 2  一个获取  一个下发， 最好在外层加延时 不管是下发还是获取，留给其他线程通讯时间

                    //  报错坑在于对_plc对象的重复使用，那如果使用2个plc对象 分别做下发与上传呢？可测试多对象访问状态测试
                    //  可进一步了解线程锁
                    if (_processing)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        break;
                    }
                }
                _processing = true;
                var timeSpan = new TimeSpan(System.DateTime.Now.Ticks - _lastCommunicationTimeval);
                //不知道是否会出现2个线程同时执行到此处的情况 可将外层延时设置低一些测试
                ////执行前记录为防止多线程同时访问与执行
                //_lastCommunicationTimeval = DateTime.Now.Ticks;
                if (timeSpan.TotalMilliseconds < 0)
                {
                    Thread.Sleep(CommunicationIntervalMSec);
                }
                else
                {
                    var leftMSec = CommunicationIntervalMSec - (int)timeSpan.TotalMilliseconds;
                    if (leftMSec > 0)
                        Thread.Sleep(leftMSec);
                    //如果大于了间隔时间 会立即发送 没有强制等待
                }
            }
            catch (Exception ex)
            {
                addLog(null, $"wait exception: ", false);
                addLog(null, ex.ToString(), false);
            }
        }

        private void processDone()
        {
            try
            {
                //执行完成后记录让延时更严谨
                _lastCommunicationTimeval = System.DateTime.Now.Ticks;
                _processing = false;
            }
            catch (Exception ex)
            {
                addLog(null, $"processDone exception: ", false);
                addLog(null, ex.ToString(), false);
            }
        }

        /// <summary>
        /// 数组比较是否相等
        /// </summary>
        /// <param name="bt1">数组1</param>
        /// <param name="bt2">数组2</param>
        /// <returns>true:相等，false:不相等</returns>
        private bool compareByteArray(byte[] bt1, byte[] bt2)
        {
            var len1 = bt1.Length;
            var len2 = bt2.Length;
            if (len1 != len2)
            {
                return false;
            }
            for (var i = 0; i < len1; i++)
            {
                if (bt1[i] != bt2[i])
                    return false;
            }
            return true;
        }

        #endregion

        #region 基础读写方法

        public void writeObject(DataType dataType, int db, int startIndex, object value, string exDescription, int writeCnt)
        {
            for (int i = 0; i < writeCnt; i++)
            {
                try
                {
                    wait();
                    string description = "";
                    if (!string.IsNullOrEmpty(exDescription) && LogDescription)
                    {
                        description = ", desc:" + exDescription;
                    }
                    addLog(SiemenzPlcHelper.serializeValue(value), $" WO Value: {value.ToString()}, [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}" + description, false);
                    _plc.Write(dataType, db, startIndex, value);
                    processDone();
                    return;
                }
                catch
                {
                    addLog(null, $"write exception: {i}", false);
                    if (i == writeCnt - 1)
                        throw;
                }
            }
        }

        public void writeBit(DataType dataType, int db, int startIndex, int addr, bool value, string exDescription, int writeCnt)
        {
            for (int i = 0; i < writeCnt; i++)
            {
                try
                {
                    wait();
                    string description = "";
                    if (!string.IsNullOrEmpty(exDescription) && LogDescription)
                    {
                        description = ", desc:" + exDescription;
                    }
                    addLog(new byte[] { value ? (byte)1 : (byte)0 }, $" WB [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}.{addr}" + description, false);
                    _plc.Write(dataType, db, startIndex, value, (byte)addr);
                    processDone();
                    return;
                }
                catch
                {
                    addLog(null, $"writeBit exception: {i}", false);
                    if (i == writeCnt - 1)
                        throw;
                }
            }
        }

        public byte[] readStatus(DataType dataType, int db, int startIndex, int count, StatusLogRules statusLogRule, int readCnt)
        {
            for (int i = 0; i < readCnt; i++)
            {
                try
                {
                    wait();
                    byte[] ret = _plc.ReadBytes(dataType, db, startIndex, count);

                    if (statusLogRule == StatusLogRules.LogAll)
                    {
                        addLog(ret, $" RS [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                    }
                    else if (statusLogRule == StatusLogRules.LogOnlyOnChange)
                    {
                        if (_lastStatuesBytes == null || _lastStatuesBytes.Length == 0 || !compareByteArray(_lastStatuesBytes, ret))
                        {
                            _lastStatuesBytes = ret;
                            addLog(ret, $" RS [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                        }
                    }
                    else if (statusLogRule == StatusLogRules.LogChangeAndSimpleSameReceive)
                    {
                        if (_lastStatuesBytes == null || _lastStatuesBytes.Length == 0 || !compareByteArray(_lastStatuesBytes, ret))
                        {
                            _lastStatuesBytes = ret;
                            addLog(ret, $" RS [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                        }
                        else
                        {
                            addLog(null, $" RS [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}  same as last", true);
                        }
                    }
                    else if (statusLogRule == StatusLogRules.LogNothing)
                    {

                    }
                    else
                    {
                        addLog(ret, $" RS [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                    }

                    processDone();
                    return ret;
                }
                catch
                {
                    addLog(null, $"read exception: {i}", true);
                    if (i == readCnt - 1)
                        throw;
                }
            }

            return null;
        }

        public byte[] readBytes(DataType dataType, int db, int startIndex, int count, int readCnt)
        {
            for (int i = 0; i < readCnt; i++)
            {
                try
                {
                    wait();
                    var ret = _plc.ReadBytes(dataType, db, startIndex, count);
                    addLog(ret, $"RB [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                    processDone();
                    return ret;
                }
                catch
                {
                    addLog(null, $"read exception: {i}", true);
                    if (i == readCnt - 1)
                        throw;
                }
            }
            return null;
        }

        public object readObject(DataType dataType, int db, int startIndex, VarType varType, int readCnt)
        {
            for (int i = 0; i < readCnt; i++)
            {
                try
                {
                    wait();
                    var ret = _plc.Read(dataType, db, startIndex, varType, 1);
                    addLog(SiemenzPlcHelper.serializeValue(ret), $" RO Value: {ret.ToString()}, [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                    processDone();
                    return ret;
                }
                catch
                {
                    addLog(null, $"read exception: {i}", true);
                    if (i == readCnt - 1)
                        throw;
                }
            }
            return null;
        }

        #endregion

        #region 封装读写方法  外部建议使用

        public void writeObject(int db, int startIndex, object value)
        {
            writeObject(DataType.DataBlock, db, startIndex, value, null, RepeatCnt);
        }

        public void writeObject(int db, int startIndex, object value, string exDescription)
        {
            writeObject(DataType.DataBlock, db, startIndex, value, exDescription, RepeatCnt);
        }

        public void writeBit(int db, int startIndex, int addr, bool value)
        {
            writeBit(DataType.DataBlock, db, startIndex, addr, value, null, RepeatCnt);
        }

        public void writeBit(int db, int startIndex, int addr, bool value, string exDescription)
        {
            writeBit(DataType.DataBlock, db, startIndex, addr, value, exDescription, RepeatCnt);
        }

        public byte[] readStatus(int db, int startIndex, int count)
        {
            return readStatus(DataType.DataBlock, db, startIndex, count, StatusLogRule, RepeatCnt);
        }

        public byte[] readBytes(int db, int startIndex, int count)
        {
            return readBytes(DataType.DataBlock, db, startIndex, count, RepeatCnt);
        }

        public object readObject(int db, int startIndex, VarType varType)
        {
            return readObject(DataType.DataBlock, db, startIndex, varType, RepeatCnt);
        }

        public T readObject<T>(int db, int startIndex)
        {
            var type = typeof(T);
            VarType varType = VarType.Bit;
            if (type == typeof(Int16))
            {
                varType = VarType.Int;
            }
            else if (type == typeof(int))
            {
                varType = VarType.DInt;
            }
            else if (type == typeof(float))
            {
                varType = VarType.Real;
            }
            else
            {
                return default(T);
            }

            return (T)(readObject(DataType.DataBlock, db, startIndex, varType, RepeatCnt));
        }

        #endregion

        #region 旧版本兼容方法 建议新项目使用新接口

        public void write(DataType dataType, int db, int startIndex, byte[] values)
        {
            write(dataType, db, startIndex, values, 1);
        }

        public void write(DataType dataType, int db, int startIndex, byte[] values, int writeCnt)
        {
            for (int i = 0; i < writeCnt; i++)
            {
                try
                {
                    addLog(values, $" [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", false);
                    wait();
                    _plc.WriteBytes(dataType, db, startIndex, values);
                    processDone();
                    return;
                }
                catch
                {
                    addLog(null, $"write exception: {i}", false);
                    if (i == writeCnt - 1)
                        throw;
                }
            }
        }

        public byte[] read(DataType dataType, int db, int startIndex, int count)
        {
            return read(dataType, db, startIndex, count, 1);
        }

        public byte[] read(DataType dataType, int db, int startIndex, int count, int readCnt)
        {
            for (int i = 0; i < readCnt; i++)
            {
                try
                {
                    wait();
                    var ret = _plc.ReadBytes(dataType, db, startIndex, count);
                    addLog(ret, $" [PLC: {_plc.IP}], db: {db}, startIndex: {startIndex}", true);
                    processDone();
                    return ret;
                }
                catch
                {
                    addLog(null, $"read exception: {i}", true);
                    if (i == readCnt - 1)
                        throw;
                }
            }

            return null;
        }

        #endregion

    }
}
