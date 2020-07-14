using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Echo : MonoBehaviour
{
    //定义套接字
    Socket socket;
    //接收缓存区
    ByteArray readBuff = new ByteArray();
    Queue<ByteArray> writeQueue = new Queue<ByteArray>();

    //现实文字
    string recvStr = "";

    public InputField inputField;
    public Text text;
    private List<Socket> checkRead;

    public void Connection()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect("127.0.0.1", 8888);
        socket.BeginReceive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0, ReceiveCallBack, socket);
    }

    public void ConnectionCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Succ");
            socket.BeginReceive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0, ReceiveCallBack, socket);
        }
        catch (SocketException se)
        {
            Debug.LogError("Socket Connect fail:" + se.ToString());
        }
    }

    public void ReceiveCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket) ar.AsyncState;
            //获取接收数据长度
            int count = socket.EndReceive(ar);
            readBuff.writeIndex += count;
            //处理二进制消息
            OnReceiveData();
            //继续收集数据
            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.ReSize(readBuff.length * 2);
            }
            socket.BeginReceive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0, ReceiveCallBack, socket);
        }
        catch (SocketException se)
        {
            Debug.LogError("Socket Connetion fail:" + se.ToString());
        }
    }

    private void OnReceiveData()
    {
        Debug.Log("[Rece 1] buffCount:" + readBuff.length);
        Debug.Log("[Rece 2] readBuff:" + readBuff.ToString());

        if (readBuff.length <= 2) return;

        //消息长度
        int readIndex = readBuff.readIndex;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)(bytes[readIndex + 1] << 8 | bytes[readIndex]);
        if (readBuff.length < bodyLength + 2) return;

        readBuff.readIndex += 2;
        Debug.Log("[Recv 3] bodyLength = " + bodyLength);
        //消息体
        byte[] stringByte = new byte[bodyLength];
        readBuff.Read(stringByte, 0, bodyLength);
        string s = Encoding.UTF8.GetString(stringByte);

        Debug.Log("[Recv 4] s = " + s);
        Debug.Log("[Recv 5] readbuff = " + readBuff.ToString());
        //消息处理
        recvStr = s + "\n" + recvStr;
        //继续读取消息
        if (readBuff.length > 2)
        {
            OnReceiveData();
        }
    }


    public void Send()
    {
        //send
        string sendstr = inputField.text;
        //组装协议
        byte[] bodyByte = Encoding.UTF8.GetBytes(sendstr);
        Int16 len = (Int16)bodyByte.Length;
        byte[] lenByte = BitConverter.GetBytes(len);

        //大小端编码判断,写入缓冲区的数字必须是小端编码
        if (!BitConverter.IsLittleEndian)
        {
            Debug.Log("[Send] Reverse lenBytes");
            lenByte.Reverse();//将大端编码转换成小端编码
        }

        byte[] sendByte = lenByte.Concat(bodyByte).ToArray();
        ByteArray byteArray = new ByteArray(sendByte);

        //加锁
        int count = 0;
        lock (writeQueue)
        {
            writeQueue.Enqueue(byteArray);//排队
            count = writeQueue.Count;
        }

        //send
        if(writeQueue.Count == 1)
        {
            socket.BeginSend(byteArray.bytes, byteArray.readIndex, byteArray.length, 0, SendCallBack, socket);
        }
        Debug.Log("[Send]" + BitConverter.ToString(sendByte));
    }

    public void SendCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndSend(ar);
            
            ByteArray byteArray;
            lock (writeQueue)
            {
                byteArray = writeQueue.First();
            }

            byteArray.readIndex += count;
            if(byteArray.length == 0)
            {
                lock (writeQueue)
                {
                    writeQueue.Dequeue();//出列
                    byteArray = writeQueue.First();
                }
            }
            if(byteArray != null)
            {
                socket.BeginSend(byteArray.bytes, byteArray.readIndex, byteArray.length, 0, SendCallBack, socket);
            }

            Debug.Log("Socket send success");
        }
        catch(SocketException se)
        {
            Debug.LogError("Send is fail:" + se.ToString());
        }
    }

    public void Update()
    {
        text.text = recvStr;
    }

    private void Start()
    {
        text.text = null;
        checkRead = new List<Socket>();
    }
}
