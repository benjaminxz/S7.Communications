using S7.Net;
using S7.Net.Types;
using Timer = S7.Net.Types.Timer;

namespace S7.Communications.Siemenz
{
    public static class SiemenzPlcHelper
    {
        /// <summary>

        /// 将byte转换为常用类型  目前支持int16、int32、float以及对应的数组，后续可能会增加新的类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T parseBytes<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return (T)(object)null;
            var type = typeof(T);
            VarType varType = VarType.Bit;
            int varCount = 1;
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
            else if (type == typeof(Int16[]))
            {
                varType = VarType.Int;
                varCount = 999;
            }
            else if (type == typeof(int[]))
            {
                varType = VarType.DInt;
                varCount = 999;
            }
            else if (type == typeof(float[]))
            {
                varType = VarType.Real;
                varCount = 999;
            }
            else
            {
                return (T)(object)null;
            }
            return (T)parseBytes(varType, bytes, varCount);
        }

        private static object parseBytes(VarType varType, byte[] bytes, int varCount, byte bitAdr = 0)
        {
            if (bytes == null || bytes.Length == 0)
                return (object)null;
            switch (varType)
            {
                case VarType.Bit:
                    if (varCount != 1)
                        return (object)Bit.ToBitArray(bytes);
                    if (bitAdr > (byte)7)
                        return (object)null;
                    return (object)Bit.FromByte(bytes[0], bitAdr);
                case VarType.Byte:
                    if (varCount == 1)
                        return (object)bytes[0];
                    return (object)bytes;
                case VarType.Word:
                    if (varCount == 1)
                        return (object)Word.FromByteArray(bytes);
                    return (object)Word.ToArray(bytes);
                case VarType.DWord:
                    if (varCount == 1)
                        return (object)DWord.FromByteArray(bytes);
                    return (object)DWord.ToArray(bytes);
                case VarType.Int:
                    if (varCount == 1)
                        return (object)Int.FromByteArray(bytes);
                    return (object)Int.ToArray(bytes);
                case VarType.DInt:
                    if (varCount == 1)
                        return (object)DInt.FromByteArray(bytes);
                    return (object)DInt.ToArray(bytes);
                case VarType.Real:
                    if (varCount == 1)
                        return (object)S7.Net.Types.Single.FromByteArray(bytes);
                    return (object)S7.Net.Types.Single.ToArray(bytes);
                case VarType.String:
                    return (object)S7.Net.Types.String.FromByteArray(bytes);
                //case VarType.StringEx:
                //    return (object)StringEx.FromByteArray(bytes);
                case VarType.Timer:
                    if (varCount == 1)
                        return (object)Timer.FromByteArray(bytes);
                    return (object)Timer.ToArray(bytes);
                case VarType.Counter:
                    if (varCount == 1)
                        return (object)Counter.FromByteArray(bytes);
                    return (object)Counter.ToArray(bytes);
                default:
                    return (object)null;
            }
        }

        /// <summary>
        /// object to  byte array ,拷贝s7源代码
        /// 将常用类型转换为byte 目前各种类型都基本上支持，不过每次只能转换一个值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] serializeValue(object value)
        {
            switch (value.GetType().Name)
            {
                case "Boolean":
                    return new byte[1]
                    {
                        (bool) value ? (byte) 1 : (byte) 0
                    };
                case "Byte":
                    return S7.Net.Types.Byte.ToByteArray((byte)value);
                case "Byte[]":
                    return (byte[])value;
                case "DateTime":
                    return S7.Net.Types.DateTime.ToByteArray((System.DateTime)value);
                case "DateTimeLong[]":
                    return DateTimeLong.ToByteArray((System.DateTime[])value);
                case "DateTime[]":
                    return S7.Net.Types.DateTime.ToByteArray((System.DateTime[])value);
                case "Double":
                    return LReal.ToByteArray((double)value);
                case "Double[]":
                    return LReal.ToByteArray((double[])value);
                case "Int16":
                    return Int.ToByteArray((short)value);
                case "Int16[]":
                    return Int.ToByteArray((short[])value);
                case "Int32":
                    return DInt.ToByteArray((int)value);
                case "Int32[]":
                    return DInt.ToByteArray((int[])value);
                case "Single":
                    return Real.ToByteArray((float)value);
                case "Single[]":
                    return Real.ToByteArray((float[])value);
                case "String":
                    string str = (string)value;
                    return S7.Net.Types.String.ToByteArray(str, str.Length);
                case "UInt16":
                    return Word.ToByteArray((ushort)value);
                case "UInt16[]":
                    return Word.ToByteArray((ushort[])value);
                case "UInt32":
                    return DWord.ToByteArray((uint)value);
                case "UInt32[]":
                    return DWord.ToByteArray((uint[])value);
                default:
                    return null;
            }
        }

    }
}
