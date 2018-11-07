/*
MIT License

Copyright 2018 SerraFullStack

Permission is hereby granted, free of charge, to any person obtaining a  copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the  rights
to  use,  copy,  modify, merge, publish, distribute, sublicense, and/or  sell
copies  of  the  Software,  and  to  permit  persons  to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this  permission  notice  shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS",  WITHOUT  WARRANTY  OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT  LIMITED  TO  THE  WARRANTIES  OF MERCHANTABILITY,
FITNESS FOR A  PARTICULAR  PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR  COPYRIGHT  HOLDERS  BE  LIABLE  FOR  ANY  CLAIM, DAMAGES OR OTHER
LIABILITY,  WHETHER  IN  AN  ACTION  OF  CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE  SOFTWARE OR THE USE OR OTHER DEALINGS
IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EasyComLib
{
    public delegate void DOnMessage(Message message);


    public class ReCom
    {
        public static Random rnd = new Random((int)DateTime.Now.ToBinary());
        const string AutoCreateId = "AutoCreateId";
        const string NotFound = "NotFound";
        const string All = "All";

        string id;
        event DOnMessage onMessage;
        TcpListener tcpListener;
        int tcpListenerPort = -1;

        //this variable contains all connections (from clients and to anothers servers)
        Dictionary<string, Socket> connections = new Dictionary<string, Socket>();

        //this upd clinet is used to respond device identifications message      
        private UdpClient udpClient = new UdpClient(22500);
        public ReCom(string id = AutoCreateId)
        {
            if (id == AutoCreateId)
            {
                id = DateTime.Now.ToString("ddMMyyhhmmss") + rnd.Next().ToString();
            }
            //save the id
            this.id = id.ToLower();

            //start UPD listenner
            this.udpClient.BeginReceive(OnUdpClinetReceive, new object());

            //start TCP listenner ****** change this to use a function to check if port is available
            int currTryingPort = 22500;
            while (true)
            {
                try
                {
                    tcpListener = new TcpListener(IPAddress.Any, currTryingPort);
                    tcpListener.Start();
                    this.tcpListenerPort = currTryingPort;

                    tcpListener.BeginAcceptSocket(this.tcpClientConnected, new object());
                    break;
                }
                catch { currTryingPort++; }
            }

        }



        /// <summary>
        /// Finds a device by the identifier.
        /// </summary>
        /// <returns>The by identifier.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="onfind">Onfind.</param>
        public ReCom ConnectTo(string id, Action onSucess, Action onError)
        {
            id = id.ToLower();
            //looking in stablishd connections for a desired id
            if (this.connections.ContainsKey(id))
            {
                //if one connection was found, checks if this is active
                if (this.connections[id].Connected)
                {
                    onSucess();
                    return this;
                }
                //if the found connection isn't active, remove their for active devices and send udp pack
                else
                    this.connections.Remove(id);
            }
            //if connection to desired id wasn't found, send upd pack looking for the remote id (lfi pack)

            DateTime s = DateTime.Now;

            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 22500);
            List<byte> sendBuffer = new List<byte>();
            sendBuffer.Add(0x02);
            sendBuffer.Add(0x02);
            sendBuffer.AddRange(Encoding.UTF8.GetBytes("lfi;" + this.id + ";"));
            sendBuffer.AddRange(Encoding.UTF8.GetBytes(this.getIp() + ";"));
            sendBuffer.AddRange(Encoding.UTF8.GetBytes(((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString() + ";"));
            sendBuffer.AddRange(Encoding.UTF8.GetBytes(id));



            while (DateTime.Now.Subtract(s).TotalMilliseconds <= 10000)
            {
                client.Send(sendBuffer.ToArray(), sendBuffer.Count, ip);
                Thread.Sleep(100);
                if (this.connections.ContainsKey(id))
                {
                    onSucess();
                    return this;
                }
            }
            onError();
            return this;
        }

        public ReCom Disconnect(string id = All)
        {

            return this;
        }

        public ReCom SetOnMessage(DOnMessage onMessage)
        {
            this.onMessage += onMessage;
            return this;
        }

        Dictionary<string, List<DOnMessage>> waitingMessages = new Dictionary<string, List<DOnMessage>>();
        public ReCom waitNextMessage(string title, DOnMessage onNextMessage)
        {
            if (!waitingMessages.ContainsKey(title))
                waitingMessages[title] = new List<DOnMessage>();

            waitingMessages[title].Add(onNextMessage);

            return this;
        }

        public ReCom sendMessage(string title, string[] arguments, string id = All)
        {
            id = id.ToLower();
            byte[] intAsByte = new byte[4];
            //prepare the buffer that will be sented to destination(s)
            List<byte> buffer = new List<byte>();
            buffer.Add(0x02);
            buffer.Add(0x02);
            buffer.AddRange(Encoding.UTF8.GetBytes("pkme;" + this.id + ';' + id + ';' + title + ';'));

            intAsByte = BitConverter.GetBytes(arguments.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(intAsByte);
            buffer.AddRange(intAsByte);
            buffer.Add(Convert.ToByte(';'));

            foreach (var c in arguments)
            {
                //add the size of argument
                intAsByte = BitConverter.GetBytes(c.Length);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(intAsByte);
                buffer.AddRange(intAsByte);
                buffer.Add(Convert.ToByte(';'));


                //add the argument
                buffer.AddRange(Encoding.UTF8.GetBytes(c));
                buffer.Add(Convert.ToByte(';'));
            }

            //add the pack end
            buffer.Add(0xFF);
            buffer.Add(0xFF);


            byte[] sendBuffer = buffer.ToArray();

            //send the pack
            if (id == All)
            {
                foreach (var c in this.connections)
                    c.Value.Send(sendBuffer);
            }
            else
            {
                if (this.connections.ContainsKey(id) && !this.connections[id].Connected)
                    this.connections.Remove(id);

                if (!this.connections.ContainsKey(id))
                {
                    ConnectTo(id, delegate ()
                    {
                        this.connections[id].Send(sendBuffer);
                    }, delegate () { });
                }
                else
                    this.connections[id].Send(sendBuffer);
            }

            return this;
        }

        private void tcpClientConnected(IAsyncResult ar)
        {
            Socket clientSocket = this.tcpListener.EndAcceptSocket(ar);
            tcpListener.BeginAcceptSocket(this.tcpClientConnected, new object());


            startReadSocket(clientSocket);
        }

        private void startReadSocket(Socket socket)
        {
            Thread th = new Thread(delegate ()
            {
                string state = "readingStx";
                List<byte> buffer = new List<byte>();
                while (socket.Connected)
                {
                    if (socket.Available > 0)
                    {
                        byte[] temp = new byte[socket.Available];
                        socket.Receive(temp);
                        List<byte> rawBuffer = new List<byte>(temp);

                        for (int count = 0; count < rawBuffer.Count; count++)
                        {
                            byte c = rawBuffer[count];
                            switch (state)
                            {
                                case "readingStx":
                                    buffer.Add(c);
                                    if (buffer.Count == 2)
                                    {
                                        if ((buffer[0] == 0x02) && (buffer[1] == 0x02))

                                            state = "readingPackType";

                                        buffer.Clear();
                                    }
                                    break;
                                case "readingPackType":
                                    if (Convert.ToByte(';') == c)
                                    {
                                        string packType = Encoding.UTF8.GetString(buffer.ToArray()).ToLower();
                                        if (packType == "pkim")
                                        {
                                            rawBuffer.RemoveRange(0, count + 1);
                                            rawBuffer = this.readItsMePack(socket, rawBuffer);
                                        }
                                        else if (packType == "pkme")
                                        {
                                            rawBuffer.RemoveRange(0, count + 1);
                                            rawBuffer = this.readMessagePack(socket, rawBuffer);
                                        }

                                        buffer.Clear();
                                        state = "readingStx";
                                    }
                                    else
                                        buffer.Add(c);

                                    break;
                            }
                        }

                    }
                    else
                        Thread.Sleep(10);
                }
            });
            th.Start();
        }


        private List<byte> readItsMePack(Socket clientSocket, List<byte> startBuffer)
        {
            //read remote id, until receive a 0xFF 
            byte c = 0;

            bool done = false;

            string remoteId = "";

            while (!done)
            {
                while (startBuffer.Count > 0)
                {
                    c = startBuffer[0];
                    startBuffer.RemoveAt(0);

                    //read the id until found  a 0xFF byte
                    if ((c == 0xff) || (id.Length >= 1024))
                        done = true;
                    else
                        remoteId += Convert.ToChar(c);
                }

                if (clientSocket.Available > 0)
                {
                    byte[] temp = new byte[clientSocket.Available];
                    clientSocket.Receive(temp);
                    startBuffer.AddRange(temp);

                }
            }

            //register the new connection
            this.connections[remoteId.ToLower()] = clientSocket;

            return startBuffer;

        }

        private List<byte> readMessagePack(Socket clientSocket, List<byte> startBuffer)
        {
            //read remote id, until receive a 0xFF 
            byte c = 0;

            bool done = false;
            string state = "readingSenderId";
            Message incomingMessage = new Message();

            byte[] intAsArray = new byte[4];
            int intAsArrayCount = 0;

            UInt32 readedArguments = 0;
            UInt32 totalArguments = 0;

            byte[] currentArgumentBuffer = new byte[0];
            UInt32 currentArgumentBufferSize = 0;
            UInt32 currentArgumentBufferReadedBytes = 0;

            while (!done)
            {
                while ((!done) && (startBuffer.Count > 0))
                {
                    c = startBuffer[0];
                    startBuffer.RemoveAt(0);
                    switch (state)
                    {
                        case "readingSenderId":
                            if (c != ';')
                                incomingMessage.SenderId += Convert.ToChar(c);
                            else
                                state = "readingDestinationId";
                            break;
                        case "readingDestinationId":
                            if (c != ';')
                                incomingMessage.DestinationId += Convert.ToChar(c);
                            else
                                state = "readingMessageTitle";
                            break;
                        case "readingMessageTitle":
                            if (c != ';')
                                incomingMessage.Title += Convert.ToChar(c);
                            else
                                state = "readingArgumentCount";
                            break;
                        case "readingArgumentCount":
                            intAsArray[intAsArrayCount++] = c;
                            if (intAsArrayCount == 4)
                            {
                                if (!BitConverter.IsLittleEndian)
                                    Array.Reverse(intAsArray);

                                totalArguments = BitConverter.ToUInt32(intAsArray, 0);


                                intAsArray = new byte[4];
                                intAsArrayCount = 0;
                                if (totalArguments > 0)
                                    state = "readingNextArgumentSize";
                                else
                                {
                                    state = "done";
                                    done = true;
                                }

                            }
                            break;
                        case "readingNextArgumentSize":
                            if (intAsArrayCount <= 4)
                                intAsArray[intAsArrayCount++] = c;
                            //skip a ';'
                            if (intAsArrayCount >= 5)
                            {
                                if (!BitConverter.IsLittleEndian)
                                    Array.Reverse(intAsArray);

                                currentArgumentBufferSize = BitConverter.ToUInt32(intAsArray, 0);
                                currentArgumentBuffer = new byte[currentArgumentBufferSize];
                                currentArgumentBufferReadedBytes = 0;

                                intAsArray = new byte[4];
                                intAsArrayCount = 0;
                                if (currentArgumentBuffer.Length > 0)
                                    state = "readingNextArgument";

                            }
                            break;
                        case "readingNextArgument":
                            if (currentArgumentBufferReadedBytes < currentArgumentBufferSize)
                                currentArgumentBuffer[currentArgumentBufferReadedBytes++] = c;
                            //skyp the ';' at end
                            else if (currentArgumentBufferReadedBytes > currentArgumentBufferSize)
                            {
                                incomingMessage.Arguments.Add(currentArgumentBuffer);
                                currentArgumentBuffer = new byte[0];

                                readedArguments++;
                                if (readedArguments < totalArguments)
                                    state = "readingNextArgumentSize";
                                else
                                {
                                    state = "done";
                                    done = true;
                                }
                            }

                            break;
                    }
                }

                //checks if exists more data to receive by socket
                if (clientSocket.Available > 0)
                {
                    byte[] temp = new byte[clientSocket.Available];
                    clientSocket.Receive(temp);
                    startBuffer.AddRange(temp);
                }
            }

            //cal onMessage events
            this.onMessage.Invoke(incomingMessage);

            return startBuffer;
        }


        private void OnUdpClinetReceive(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 22500);
            byte[] bytes = this.udpClient.EndReceive(ar, ref ip);
            string message = Encoding.ASCII.GetString(bytes);

            //parse the received message (lft = looking for type, lfi = looking for id)
            if (message.Contains("lfi"))
            {
                //check if the request id is the same of this.id
                //[0x02 0x02]lfi;[senderId];[sender ip];[sender tcp port];[desired id]
                string[] messageParts = message.Split(';');
                string senderId = messageParts[1];
                string senderIp = messageParts[2];
                string senderTcpPort = messageParts[3];
                string desiredId = messageParts[4];

                if (desiredId == this.id)
                {
                    this.connectToTcpServer(senderIp, int.Parse(senderTcpPort), desiredId, delegate () {
                        this.sendItsMePack(desiredId);
                    }, delegate () { });
                }

            }

            this.udpClient.BeginReceive(OnUdpClinetReceive, new object());
        }

        /// <summary>
        /// This list is used to prevent a excessive connections to a server when a lot of "looking for id" was received 
        /// in sequence.
        /// </summary>
        List<string> connectingPendings = new List<string>();
        private ReCom connectToTcpServer(string ip, int port, string remoteId, Action onSucess, Action onError)
        {
            if (connectingPendings.Contains(remoteId))
                return this;
            connectingPendings.Add(remoteId);

            TcpClient cli = new TcpClient();
            //start connection
            cli.BeginConnect(ip, port, delegate (IAsyncResult ar)
            {
                cli.EndConnect(ar);

                if (cli.Client.Connected)
                {
                    this.connections[remoteId] = cli.Client;
                    startReadSocket(cli.Client);
                    onSucess();
                }
                else
                {
                    if (this.connections.ContainsKey(remoteId))
                        this.connections.Remove(remoteId);
                    onError();
                }

            }, new object());

            if (connectingPendings.Contains(remoteId))
                connectingPendings.Remove(remoteId);
            return this;
        }

        private ReCom sendItsMePack(string remoteId)
        {
            //checks if have a stablished connection with remoteId
            if (this.connections.ContainsKey(remoteId))
            {
                //prepare the It's me pack
                List<Byte> pack = new List<byte>();
                pack.Add(2);
                pack.Add(2);
                pack.AddRange(Encoding.UTF8.GetBytes("pkim;" + this.id));
                pack.Add(0xFF);
                pack.Add(0xFF);

                //send pack to remoteId
                this.connections[remoteId].Send(pack.ToArray());
            }

            return this;
        }

        private string getIp()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr.ToString();
                }
            }

            return "";
        }

    }
}
