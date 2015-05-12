using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Dialogue_Data_Entry
{
    public class SynchronousSocketListener
    {

        // Incoming data from the client.
        public string data = null;
        private Socket handler = null;


        public void StartListening()
        {
            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
           //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); 
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 4510);

            MessageBox.Show(ipHostInfo.HostName);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    // Program is suspended while waiting for an incoming connection.
                    handler = listener.Accept();
                    if (handler != null)
                    {
                        break;
                    }
                    //Shutdown the server
                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void CloseServer()
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        public string ReceieveDataFromClient()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];
            data = null;

            bytes = new byte[1024];
            int bytesRec = handler.Receive(bytes);
            Console.Write(" Data: " + bytesRec + "\n");
            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
            if (data.IndexOf("<EOF>") > -1)
            {
              // Show the data on the console.
              Console.WriteLine("Text received : {0}", data);
              return data;  
            }
            return data; // return "";
        }

        public void SendDataToClient(string data)
        {
            // Echo the data back to the client.
            byte[] msg = Encoding.ASCII.GetBytes(data);
            handler.Send(msg);
        }

    }
}
