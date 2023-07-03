# S7.Communications
西门子plc s7.net 的 进一步集成与扩展/The further integration and expansion of Siemens' PLC S7.NET.

文档包含中文与英文
Doc contain Chinese and English.


# Introduction and Example of SiemenzTcpPlcBase

After using the code of SiemenzTcpPlc, I have been thinking about how to make the function easier to call, reduce the number of parameters, make the logic clearer and less error-prone, make the communication more stable, make the function more powerful, and increase the readability of the log. More importantly, how to reduce the workload and maintenance difficulty of developers.

With these questions in mind, SiemenzTcpPlcBase class was rewritten, which requires a new s7.net reference.

Features and optimizations
- Optimization method, reduce parameters
- When reading status, you can choose to record log mode. Recording when changing can reduce the amount of log.
- When writing data, you can add custom log descriptions to increase the readability of logs.
- Use generics to reduce the number of read and write methods.
- The communication sleep method is changed to wait, and the code to improve communication stability is added.
- Add SiemenzPlcHelper class, which currently includes data conversion methods.
- Support reading all status data at once and increase byte processing methods.
- Reference new S7.net library.


## 1 Basic knowledge
A byte has 8 bits, 1B = 8b. 
When the PLC reads data, 1 byte is usually used to represent bool in C#.
Data correspondence and size

| C#             | PLC  | Byte |
| -------------- | ---- | ---- |
| Int16（short） | Int  | 2    |
| Int32（int）   | DInt | 4    |
| float          | Real | 4    |

Int and Real are usually used in PLC projects.

## 2 Parameters

The main parameters are as follows, and the default values will be set automatically when the object is created.
```c#
public int CommunicationIntervalMSec { get; set; }// Interval time before sending command
public int RepeatCnt { get; set; } //Number of retries
public bool LogDescription { get; set; } //Whether to log description
public StatusLogRules StatusLogRule { get; set; }

/// <summary>
/// Status read communication log rule, status read takes up a lot of space in the log,
/// </summary>
public enum StatusLogRules
    {
        LogAll,//Record complete data every time
        LogOnlyOnChange,//Record complete data when changing
        LogChangeAndSimpleSameReceive,//Record complete data when changing, and record simply when not changing
        LogNothing, //Do not record
    }
```

## 3 Reading

### 3.1 int value

Use the readObject function, which does not need to specify the number of bytes, but needs to specify the return value type. The readObject can only read one value at a time.
```c#
public T readObject<T>(int db, int startIndex)
```
example：
```c#
Int16 value = readObject<Int16>(2, 6);
Int32 value32 = readObject<Int32>(2, 6);
```

### 3.2 float value

Similarly, use the readObject function, which does not need to specify the number of bytes, but needs to specify the return value type. The readObject can only read one value at a time.

example：

```c#
float valuefloat = readObject<float>(2, 32);
```
### 3.3 bool (status)

The status value usually needs to be read all at once. 8 bool values occupy one byte. First, let's take a look at the previous method, which can still be used now.
```c#

var recv = read(S7.Net.DataType.DataBlock, 1, 540, 1);
if (recv != null && recv.Length >= 1)
{
    if ((recv[0] & 0x01) == 0x01)
        plcStatus.IsReadingData = true;
    if ((recv[0] & 0x02) == 0x02)
        plcStatus.IsPressureLess = true;
    if ((recv[0] & 0x04) == 0x04)
        plcStatus.IsSampleLess = true;
    if ((recv[0] & 0x08) == 0x08)
        plcStatus.IsStuck = true;
    if ((recv[0] & 0x10) == 0x10)
        plcStatus.IsStopped = true;
    if ((recv[0] & 0x20) == 0x20)
        plcStatus.IsPushing = true;
}


```
Now there is a simpler and clearer method.
```c#
public byte[] readStatus(int db, int startIndex, int count)
```
example：
```c#
    byte[] readResult = readStatus(2, 0, 1);
    BitArray bitArray = new BitArray(readResult);
    bool result0 = bitArray[0];
    bool result1 = bitArray[1];
```
### 3.4 Read a set of states and values at one time
In some large projects, in order to reduce the number of interactions and improve the efficiency of interactions, data can be read at one time, and then the byte[] can be disassembled and converted into the required data.


```c#
public byte[] readStatus(int db, int startIndex, int count)
public byte[] readBytes(int db, int startIndex, int count)
```
example：

```c#

// Read all data at one time
byte[] readResult = readStatus(2, 0, 60);
//byte[] readResult = readBytes(2, 0, 60);

byte[] byteOne = new byte[1];
BitArray bitArray;

// Convert 1 byte each time for clearer logic. It can be converted at one time
System.Array.Copy(readResult, 0, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 1, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 2, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 3, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

// Call SiemenzPlcHelper to convert byte[] to Int16 at one time
// Reserve space to copy data
byte[] byteInts = new byte[14 * 2];
Array.Copy(readResult, 4, byteInts, 0, 14 * 2);
Int16[] int16Value = SiemenzPlcHelper.parseBytes<Int16[]>(byteInts);

// Call SiemenzPlcHelper to convert byte[] to float at one time
// There are 7 floats in total
byte[] byteFloats = new byte[7 * 4];
Array.Copy(readResult, 32, byteFloats, 0, 7 * 4);
float[] floatValue = SiemenzPlcHelper.parseBytes<float[]>(byteFloats);

```
The difference between readStatus and readBytes is that readStatus is affected by the StatusLogRule parameter and records communication logs according to the specified rules, while readBytes records communication logs in the default way (record all) and has no other differences.

```c#
2022/12/09 14:34:51  demo
14:34:50.658 << 01 02 06 20 00 04 00 05 00 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 01 47 AE 00 00 00 00 42 21 99 9A 00 00 00 00 00 00 00 00 00 00 00 00 42 62 44 44 // [PLC: 192.168.1.116], db: 2, startIndex: 0
14:34:51.174 << // [PLC: 192.168.1.116], db: 2, startIndex: 0 result same as last
14:34:51.698 << // [PLC: 192.168.1.116], db: 2, startIndex: 0 result same as last
```

## 4 Read

### 4.1 int value
Use the writeObject function without specifying the type.
```c#
public void writeObject(int db, int startIndex, object value)
public void writeObject(int db, int startIndex, object value, string exDescription)
```
example：
```c#
writeObject(db, startIndex, value, cmd.GetDescription());
```
exDescription is the extended description that the programmer wants to add to the communication log. If exDescription is not empty and LogDescription is true, exDescription will be recorded in the log.

```c#
2022/12/12 14:27:27  demo
14:27:25.971 << 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 // RS [PLC: 192.168.1.116], db: 2, startIndex: 0
14:27:25.975 >> 01 // WB [PLC: 192.168.1.116], db: 4, startIndex: 0.0, desc:自检和标定
14:27:26.514 >> 01 // WB [PLC: 192.168.1.116], db: 4, startIndex: 0.1, desc:公共部分------报警灯红
14:27:27.043 >> 00 0C // WO Value: 12, [PLC: 192.168.1.116], db: 4, startIndex: 12, desc:抓取单元------Y电机距离设置
```

### 4.2 bool value

Use writeBit to write a bool value. Note that this method is used when there is an addr. In the PLC program address, the offset will be written as a value such as 6.1.

```c#
public void writeBit(int db, int startIndex, int addr, bool value)
public void writeBit(int db, int startIndex, int addr, bool value, string exDescription)
```
example：
```c#
writeBit(db, startIndex, addr, boolValue, cmd.GetDescription());
```
When there is no addr, you can use writeObject to write new byte[] { 1 } or new byte[] { 0 }, or try to write addr as 0 to see if it is successfully issued.

### 4.3 Write a group of states and values at one time

Reduce the number of interactions and improve interaction efficiency.

example：
```c#
SiemensPlc plc = new SiemensPlc();
bool succ = plc.connect(3);

//Specify the array size first
byte[] mybyte = new byte[22];
//Assign BitArray, and then fill it into the array
BitArray ba = new BitArray(8);
ba[0] = true;
ba[1] = true;
ba[2] = true;
ba[3] = true;
ba[4] = true;
ba.CopyTo(mybyte, 4);

ba = new BitArray(8);
ba[4] = true;
ba[5] = true;
ba[6] = true;
ba[7] = true;
ba.CopyTo(mybyte, 5);

//Use SiemenzPlcHelper to convert values to byte[]
Int16 value16 = 20;
byte[] byteInt16 = SiemenzPlcHelper.serializeValue(value16);
byteInt16.CopyTo(mybyte, 20);

value16 = 17161;
byteInt16 = SiemenzPlcHelper.serializeValue(value16);
byteInt16.CopyTo(mybyte, 16);

//Send data
plc.writeObject(4, 0, mybyte);

plc.saveLog("demo");

plc.disconnect();
```


## 5 Questions

### 5.1 How to convert byte to common types

Use SiemenzPlcHelper.parseBytes currently supports int16, int32, float and corresponding arrays, and new types may be added later
```c#
Int16[] int16Value = SiemenzPlcHelper.parseBytes<Int16[]>(byteInts);
```
### 5.2 How to convert common types to byte[]

Use SiemenzPlcHelper.serializeValue() currently supports almost all types, but only one value can be converted at a time
```c#
byte[] byteInt16 = SiemenzPlcHelper.serializeValue(value16);
```

### 5.3 Details to note when using SiemenzTcpPlcBase

Parameter values can be assigned when creating objects, or parameter values can be changed after creation.
```c#
public SiemensPlc() : base(S7.Net.CpuType.S71200, ConfigHelper.getSiemensPlcIp(), 0, 1)
{
    //  You can set the log recoard strength method
    CommunicationIntervalMSec = 100;
    RepeatCnt = 3;
    LogDescription = true;
    StatusLogRule = StatusLogRules.LogChangeAndSimpleSameReceive;
}
```
When you need to save communication logs, you need to call the saveLog method
```c#
plc.saveLog("demo");
```

After the communication is over, remember to call the disconnect method.
```c#
disconnect();
```



# SiemenzTcpPlcBase 简介 与 例子

在使用之前使用SiemenzTcpPlc的代码后，一直在思考如何使函数更容易调用，参数更少，逻辑更清晰不易出错，通讯更稳定，功能更强大，log可读性增加。更重要的是如何减少开发人员工作量与维护难度。

带着这些问题，重写出SiemenzTcpPlcBase 类，需要引用新的s7.net。

特征与优化
- 优化方法，减少参数
- 读取状态时可选择记录log模式，变化时记录可减少log量
- 写数据时可增加自定义log描述，增加日志可读性
- 使用泛型，减少读写方法数量
- 通讯sleep方法改为wait 增加改善通讯稳定性的代码
- 增加SiemenzPlcHelper类，目前包含数据转换方法
- 支持一次性读完所有状态数据数据，增加字节处理方法
- 引用新的S7.net库 


## 1 基础知识
一个字节（byte）有8个比特（bit），1B = 8b
plc读取数据通常使用1个字节代表c#中的bool，
数据对应关系与所占大小

| c#             | plc  | byte |
| -------------- | ---- | ---- |
| Int16（short） | Int  | 2    |
| Int32（int）   | DInt | 4    |
| float          | Real | 4    |

plc项目通常使用  Int 与  Real

## 2 参数

主要参数如下，创建对象时会自动设置默认值。
```c#
public int CommunicationIntervalMSec { get; set; }// 发送指令前的间隔时间间隔
public int RepeatCnt { get; set; } //重发次数
public bool LogDescription { get; set; } //是否log 描述
public StatusLogRules StatusLogRule { get; set; }

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
```

## 3 读取

### 3.1 int值

使用readObject函数，无需指定 byte数量，需要指定返回值类型，readObject每次只能读一个值
```c#
public T readObject<T>(int db, int startIndex)
```
example：
```c#
Int16 value = readObject<Int16>(2, 6);
Int32 value32 = readObject<Int32>(2, 6);
```

### 3.2 float值

同样使用readObject函数，无需指定 byte数量，需要指定返回值类型，无需指定 byte数量，readObject每次只能读一个值

example：

```c#
float valuefloat = readObject<float>(2, 32);
```
### 3.3 bool(状态)

状态值一般需要一次性读完，8个bool值占一个byte，首先看看之前的写法，这种写法目前也可以用
```c#

var recv = read(S7.Net.DataType.DataBlock, 1, 540, 1);
if (recv != null && recv.Length >= 1)
{
    if ((recv[0] & 0x01) == 0x01)
        plcStatus.IsReadingData = true;
    if ((recv[0] & 0x02) == 0x02)
        plcStatus.IsPressureLess = true;
    if ((recv[0] & 0x04) == 0x04)
        plcStatus.IsSampleLess = true;
    if ((recv[0] & 0x08) == 0x08)
        plcStatus.IsStuck = true;
    if ((recv[0] & 0x10) == 0x10)
        plcStatus.IsStopped = true;
    if ((recv[0] & 0x20) == 0x20)
        plcStatus.IsPushing = true;
}


```
现在有更简便清晰的方法
```c#
public byte[] readStatus(int db, int startIndex, int count)
```
example：
```c#
    byte[] readResult = readStatus(2, 0, 1);
    BitArray bitArray = new BitArray(readResult);
    bool result0 = bitArray[0];
    bool result1 = bitArray[1];
```

### 3.4 一次性读完一组状态与数值
在一些大型项目中，为了减少交互次数，提升交互效率，可以将数据一次性读完，再将byte[]拆解、转换成所需数据。


```c#
public byte[] readStatus(int db, int startIndex, int count)
public byte[] readBytes(int db, int startIndex, int count)
```
example：

```c#

// 一次性读完所有数据
byte[] readResult = readStatus(2, 0, 60);
//byte[] readResult = readBytes(2, 0, 60);

byte[] byteOne = new byte[1];
BitArray bitArray;

// 为了逻辑更清晰 每次转换1个byte   可以一次性转换完
System.Array.Copy(readResult, 0, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 1, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 2, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

System.Array.Copy(readResult, 3, byteOne, 0, 1);
bitArray = new BitArray(byteOne);

//Int16 调用SiemenzPlcHelper 将 byte[] 一次性转换完
// 预留空间 将数据复制出来
byte[] byteInts = new byte[14 * 2];
Array.Copy(readResult, 4, byteInts, 0, 14 * 2);
Int16[] int16Value = SiemenzPlcHelper.parseBytes<Int16[]>(byteInts);

//一共7个float 调用SiemenzPlcHelper 将 byte[] 一次性转换完
byte[] byteFloats = new byte[7 * 4];
Array.Copy(readResult, 32, byteFloats, 0, 7 * 4);
float[] floatValue = SiemenzPlcHelper.parseBytes<float[]>(byteFloats);

```
readStatus与readBytes区别在与readStatus会受到StatusLogRule参数的影响，使用指定的规则记录通讯日志，readBytes会默认方式（记录所有）通讯日志，其他方面没有区别

```c#
2022/12/09 14:34:51  demo
14:34:50.658 << 01 02 06 20 00 04 00 05 00 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 42 01 47 AE 00 00 00 00 42 21 99 9A 00 00 00 00 00 00 00 00 00 00 00 00 42 62 44 44 // [PLC: 192.168.1.116], db: 2, startIndex: 0
14:34:51.174 << // [PLC: 192.168.1.116], db: 2, startIndex: 0 result same as last
14:34:51.698 << // [PLC: 192.168.1.116], db: 2, startIndex: 0 result same as last
```

## 4 读取

### 4.1 int值
使用writeObject函数，无需指定类型。
```c#
public void writeObject(int db, int startIndex, object value)
public void writeObject(int db, int startIndex, object value, string exDescription)
```
example：
```c#
writeObject(db, startIndex, value, cmd.GetDescription());
```
exDescription是编程人员想在通讯日志中加入的扩展描述。exDescription不为空且LogDescription为true时，会记录exDescription进入日志中

```c#
2022/12/12 14:27:27  demo
14:27:25.971 << 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 // RS [PLC: 192.168.1.116], db: 2, startIndex: 0
14:27:25.975 >> 01 // WB [PLC: 192.168.1.116], db: 4, startIndex: 0.0, desc:自检和标定
14:27:26.514 >> 01 // WB [PLC: 192.168.1.116], db: 4, startIndex: 0.1, desc:公共部分------报警灯红
14:27:27.043 >> 00 0C // WO Value: 12, [PLC: 192.168.1.116], db: 4, startIndex: 12, desc:抓取单元------Y电机距离设置
```

### 4.2 bool值

使用writeBit写入一个bool值，注意此方法用于 有addr的时候，在plc程序地址中偏移量会写成如 6.1 这样的值。

```c#
public void writeBit(int db, int startIndex, int addr, bool value)
public void writeBit(int db, int startIndex, int addr, bool value, string exDescription)
```
example：
```c#
writeBit(db, startIndex, addr, boolValue, cmd.GetDescription());
```
没有addr的时，可使用writeObject写入 new byte[] { 1 } 或者new byte[] { 0 }，也可以尝试将addr写成0，看是否下发成功。

### 4.3 一次性写完一组状态与数值

减少交互次数，提升交互效率。

example：
```c#
SiemensPlc plc = new SiemensPlc();
bool succ = plc.connect(3);

//先指定数组大小
byte[] mybyte = new byte[22];
//BitArray赋值，然后填入数组中
BitArray ba = new BitArray(8);
ba[0] = true;
ba[1] = true;
ba[2] = true;
ba[3] = true;
ba[4] = true;
ba.CopyTo(mybyte, 4);

ba = new BitArray(8);
ba[4] = true;
ba[5] = true;
ba[6] = true;
ba[7] = true;
ba.CopyTo(mybyte, 5);

//使用SiemenzPlcHelper将数值转换为byte[]
Int16 value16 = 20;
byte[] byteInt16 = SiemenzPlcHelper.serializeValue(value16);
byteInt16.CopyTo(mybyte, 20);

value16 = 17161;
byteInt16 = SiemenzPlcHelper.serializeValue(value16);
byteInt16.CopyTo(mybyte, 16);

//下发数据
plc.writeObject(4, 0, mybyte);

plc.saveLog("demo");

plc.disconnect();
```


## 5 问题

### 5.1 如何将byte转换为常用类型

使用 SiemenzPlcHelper.parseBytes 目前支持int16、int32、float以及对应的数组，后续可能会增加新的类型
```c#
Int16[] int16Value = SiemenzPlcHelper.parseBytes<Int16[]>(byteInts);
```
### 5.2 如何将常用类型转换为byte[]

使用 SiemenzPlcHelper.serializeValue() 目前各种类型都基本上支持，不过每次只能转换一个值
```c#
byte[] byteInt16 = SiemenzPlcHelper.serializeValue(value16);
```

### 5.3 使用SiemenzTcpPlcBase需要注意的细节

创建对象时可赋参数值 ，也可在创建后更改参数值。
```c#
public SiemensPlc() : base(S7.Net.CpuType.S71200, ConfigHelper.getSiemensPlcIp(), 0, 1)
{
    //  可设置log recoard 强度 方法
    CommunicationIntervalMSec = 100;
    RepeatCnt = 3;
    LogDescription = true;
    StatusLogRule = StatusLogRules.LogChangeAndSimpleSameReceive;
}
```
在需要保存通讯日志的时机，需要调用saveLog方法
```c#
plc.saveLog("demo");
```

通讯结束后，记得调用disconnect方法。
```c#
disconnect();
```






