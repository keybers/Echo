using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EchoServer
{
    class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024];//byte消息藏着了
    }

    class Program
    {
        //监听socket
        static Socket listenfd;
        //客户端socket及当前的状态信息
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //bind
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEndPoint = new IPEndPoint(ipAdr, 8888);
            listenfd.Bind(iPEndPoint);

            //listen
            listenfd.Listen(0);
            Console.WriteLine("[Server] Success");

            //checkread
            List<Socket> checkRead = new List<Socket>();

            //主循环
            while (true)
            {
                try
                {
                    //把clients都填充到checkread列表当中
                    checkRead.Clear();
                    checkRead.Add(listenfd);//listenfd:ServerSocket是一定要的添加的

                    foreach (ClientState cs in clients.Values)
                    {
                        checkRead.Add(cs.socket);
                    }

                    //选择出check出的对象
                    Socket.Select(checkRead, null, null, 1000);

                    foreach (Socket s in checkRead)
                    {
                        if (s == listenfd)
                        {
                            ReadListenfd(s);
                        }
                        else
                        {
                            ReadClientfd(s);
                        }
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine("error" + se.ToString());
                }

            }
        }
        /// <summary>
        /// 用来解决存在于缓存的中需要处理接收数据的特殊事件，所以用同步处理,Receive过程
        /// </summary>
        /// <param name="clientfd"></param>
        /// <returns></returns>
        private static bool ReadClientfd(Socket clientfd)
        {
            ClientState clientState = clients[clientfd];

            int count;
            try
            {
                count = clientfd.Receive(clientState.readBuff);
            }
            catch (SocketException se)//接收故障:不是未收到,紧急调停
            {
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Socket Close:Reception failure:" + se.ToString());
                return false;
            }

            //客户端关闭
            if (count == 0)
            {
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Socket Close:Client shutdown");
                return false;
            }

            //广播
            string recvStr = Encoding.UTF8.GetString(clientState.readBuff, 2, count - 2);
            Console.WriteLine("[Server] receive:" + recvStr);

            byte[] sendByte = new byte[count];
            Array.Copy(clientState.readBuff, 0, sendByte, 0, count);
            foreach (ClientState cs in clients.Values)
            {
                cs.socket.Send(sendByte);
            }
            return true;

        }
        /// <summary>
        /// 用来解决存在于缓存的中需要处理监听的特殊事件，所以用同步处理,Accept过程
        /// </summary>
        /// <param name="listenfd"></param>
        private static void ReadListenfd(Socket listenfd)
        {
            Socket clientfd = listenfd.Accept();
            ClientState clientState = new ClientState();
            clientState.socket = clientfd;

            clients.Add(clientfd, clientState);
            Console.WriteLine("[Server] Accept");
        }

        private static void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("[SERVER] Accept");
                Socket listenfd = (Socket)ar.AsyncState;
                Socket clientfd = listenfd.EndAccept(ar);

                ClientState clientState = new ClientState();
                clientState.socket = clientfd;
                //readBuffer还没有东西所以莫得

                clients.Add(clientfd, clientState);

                clientfd.BeginReceive(clientState.readBuff, 0, 1024, 0, ReceiveCallBack, clientState);
                listenfd.BeginAccept(AcceptCallBack, listenfd);

            }
            catch (SocketException se)
            {
                Console.WriteLine("Socket Accept fail:" + se.ToString());
            }


        }

        private static void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                ClientState clientState = (ClientState)ar.AsyncState;
                Socket clientfd = clientState.socket;
                int count = clientfd.EndReceive(ar);

                //client关闭
                if (count == 0)
                {
                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine("Socket Client Close");
                    return;
                }

                string recvStr = Encoding.UTF8.GetString(clientState.readBuff, 0, count);
                string sendStr = clientfd.RemoteEndPoint.ToString() + ":" + recvStr;
                byte[] sendByte = Encoding.UTF8.GetBytes(sendStr);

                //对所有连接的客户端发送消息
                foreach (ClientState c in clients.Values)
                {
                    c.socket.Send(sendByte);
                }

                clientfd.BeginReceive(clientState.readBuff, 0, 1024, 0, ReceiveCallBack, clientState);
                Console.WriteLine("Socket Receive Success");

            }
            catch (SocketException se)
            {
                Console.WriteLine("Socket Receive fail:" + se.ToString());
            }
        }
    }
}

