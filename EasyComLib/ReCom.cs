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
		List<Socket> connections = new List<Socket>();

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

                //if anyone connection wasn't found, send upd pack looking for
            
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

		private void OnUdpClinetReceive(IAsyncResult ar){
			IPEndPoint ip = new IPEndPoint(IPAddress.Any, 15000);
			byte[] bytes = this.udpClient.EndReceive(ar, ref ip);
            string message = Encoding.ASCII.GetString(bytes);

			//parse the received message (lft = looking for type, lfi = looking for id)
			if (message.Contains("lti"))
			{
				
			}
         
			this.udpClient.BeginReceive(OnUdpClinetReceive, new object());
		}
  
    }
}
