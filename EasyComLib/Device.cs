using System;
namespace EasyComLib
{
	public delegate void DOnMessage(Message message);
    public class Device
    {
        public Device()
        {
			
        }

         /// <summary>
         /// Waits the next response.
         /// </summary>
         /// <returns>The next response.</returns>
         /// <param name="onMessage">On message.</param>
		public Device WaitNextResponse(DOnMessage onMessage)
        {

            return this;
        }

        /// <summary>
        /// Sends a message to the device.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
		public Device SendMessage(Message message)
		{

			return this;
		}

        /// <summary>
        /// Sends a message to the   device.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="title">Title.</param>
        /// <param name="arguments">Arguments.</param>
		public Device SendMessage(string title, string[] arguments)
		{

			return this;
		}
    }
}
