using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasyComLib
{
	public delegate void DOnMessage(Message message);

    public class ReCom
    {
		const string AutoCreateId = "AutoCreateId";
		const string NotFound = "NotFound";
		const string All = "All";
  
		string id;

		DOnMessage onMessage;

		//this variable contains all connections (from clients and to anothers servers)
		Dictionary<string, Socket> connections = new Dictionary<string, Socket>();

		//this upd clinet is used to respond device identifications message      
		private UdpClient udpClient = new UdpClient(22500);
		public ReCom(string id = AutoCreateId)
        {
			//save the id
			this.id = id;

			//start UPD listenner
			this.udpClient.BeginReceive(OnUdpClinetReceive, new object());
            
            //start TCP listenner         

        }

        /// <summary>
        /// Finds a device by the identifier.
        /// </summary>
        /// <returns>The by identifier.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="onfind">Onfind.</param>
		public ReCom ConnectTo(string id, Action onSucess, Action onError)
		{
			//looking in stablishd connections for a desired id

                //if one connection was found, checks if this is active

                    //if the found connection is active, return their device

                    //if the found connection isn't active, remove their for active devices and send udp pack

			//if anyone connection wasn't found, send upd pack looking for the remote id (lfi pack)
            
			return this;
		}  

		public ReCom Disconnect(string id = All){

			return this;
		}

		public ReCom SetOnMessage(DOnMessage onMessage)
		{
			this.onMessage = onMessage;         
			return this;
		}

		public ReCom waitNextMessage(string title, DOnMessage onNextMessage)
		{

			return this;
		}

		public ReCom sendMessage(string title, string[] arguments, string id = All)
		{

			return this;
		}

		private void OnUdpClinetReceive(IAsyncResult ar)
		{
			IPEndPoint ip = new IPEndPoint(IPAddress.Any, 15000);
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
					this.connectToTcpServer(senderIp, int.Parse(senderTcpPort), desiredId, delegate(){
					this.sendItsMePack(desiredId);
					}, delegate(){});
				}
                
			}
         
			this.udpClient.BeginReceive(OnUdpClinetReceive, new object());
		}


        private ReCom connectToTcpServer(string ip, int port, string remoteId, Action onSucess, Action onError)
		{
			TcpClient cli = new TcpClient();
            //start connection
			cli.BeginConnect(ip, port, delegate (IAsyncResult ar) {
				cli.EndConnect(ar);

				if (cli.Client.Connected)
				{
					this.connections[remoteId] = cli.Client;
					onSucess();
				}
				else
				{
					if (this.connections.ContainsKey(remoteId))
						this.connections.Remove(remoteId);
					onError();
				}
			
			}, new object());

			return this;
		}

		private ReCom sendItsMePack(string remoteId)
		{
			if (this.connections.ContainsKey(remoteId))
			{
				List<Byte> pack = new List<byte>();
				pack.Add(2);
				pack.Add(2);
				pack.AddRange(Encoding.UTF8.GetBytes("pkim;" + this.id));
				pack.Add(0xFF);
                pack.Add(0xFF);

				this.connections[remoteId].Send(pack.ToArray());
			}

			return this;
		}
  
    }
}
