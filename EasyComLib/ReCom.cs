using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasyComLib
{
	public delegate void DOnDeviceFind(Device device);

    public class ReCom
    {
		const string AutoCreateId = "AutoCreateId";
		const string NotFound = "NotFound";

		string type;
		string id;

		//this variable contains all connections (from clients and to anothers servers)
		List<Socket> connections = new List<Socket>();

		//this upd clinet is used to respond device identifications message      
		private UdpClient udpClient = new UdpClient(22500);
		public ReCom(string type, string id = AutoCreateId)
        {
			//save the type and id
			this.type = type;
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
		public ReCom FindById(string id, DOnDeviceFind onfind)
		{
			//looking in stablishd connections for a desired id

                //if one connection was found, checks if this is active

                    //if the found connection is active, return their device

                    //if the found connection isn't active, remove their for active devices and send udp pack

                //if anyone connection wasn't found, send upd pack looking for
            
			return this;
		}

        /// <summary>
		/// Finds devices by type.
        /// </summary>
        /// <returns>The by type.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="onfind">Onfind.</param>
		public ReCom FindByType(string id, DOnDeviceFind onfind)
        {

            return this;
        }

        /// <summary>
		/// Finds one device by the type.
        /// </summary>
        /// <returns>The by one by type.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="onfind">Onfind.</param>
		public ReCom FindByOneByType(string id, DOnDeviceFind onfind)
        {

            return this;
        }      

		private void OnUdpClinetReceive(IAsyncResult ar){
			IPEndPoint ip = new IPEndPoint(IPAddress.Any, 15000);
			byte[] bytes = this.udpClient.EndReceive(ar, ref ip);
            string message = Encoding.ASCII.GetString(bytes);

			//parse the received message (lft = looking for type, lfi = looking for id)
			if (getValueFromPack(message, "pk") == "lft")
			{
				
			}
			else if (getValueFromPack(message, "pk") == "lfi")
			{
				
			}
            

			this.udpClient.BeginReceive(OnUdpClinetReceive, new object());
		}

        private string getValueFromPack(string pack, string key, string defaultValue = NotFound)
		{
			key = key.ToLower();
			string[] parts = pack.Split(';');
			foreach (var c in parts)
			{
				if (c.Contains("=") && (c.Substring(0, c.IndexOf("=")).ToLower() == key))
					return "";
			}

			return defaultValue;
			
		}
    }
}
