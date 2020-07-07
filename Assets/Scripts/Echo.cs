using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Echo : MonoBehaviour
{
    //定义套接字
    Socket socket;
    //接收缓存区
    byte[] readBuff = new byte[1024];
    string recvStr;
    List<Socket> checkRead;

    public InputField inputField;
    public Button cancellationButton;
    public Text text;

    public void Connection()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect("127.0.0.1", 8888);
    }

    public void ConnectionCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("Socket Connect Succ");
            socket.BeginReceive(readBuff, 0, 1024, 0, ReceiveCallBack, socket);
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
            int count = socket.EndReceive(ar);
            recvStr = Encoding.UTF8.GetString(readBuff, 0, count) + "\n" + recvStr;

            socket.BeginReceive(readBuff, 0, 1024, 0, ReceiveCallBack, socket);
        }
        catch(SocketException se)
        {
            Debug.LogError("Socket Connetion fail:" + se.ToString());
        }
    }

    public void Send()
    {
        //send
        string sendstr = inputField.text;
        byte[] sendBytes = Encoding.UTF8.GetBytes(sendstr);
        socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, socket);
    }

    public void SendCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndSend(ar);
            Debug.Log("Socket send success");
        }
        catch(SocketException se)
        {
            Debug.LogError("Send is fail:" + se.ToString());
        }
    }

    public void Update()
    {
        if (socket == null) return;

        //填充checkRead列表
        checkRead.Clear();
        checkRead.Add(socket);

        //select
        Socket.Select(checkRead, null, null, 0);

        //Receive,checkread虽然只有一个Socket,但得按规矩来
        foreach (Socket s in checkRead)
        {
            byte[] readBuff = new byte[1024];
            int count = s.Receive(readBuff);
            string recveStr = Encoding.UTF8.GetString(readBuff, 0, count);

            text.text += recveStr + "\n";
        }
    }

    private void Start()
    {
        text.text = null;
        checkRead = new List<Socket>();
    }
}
