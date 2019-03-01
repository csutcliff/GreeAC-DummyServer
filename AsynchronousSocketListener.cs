using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DummyServer
{
    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPAddress ipAddress = IPAddress.Parse("172.22.20.104");  
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            
            Console.WriteLine("Accepted Connection from "+handler.RemoteEndPoint.ToString());

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject) ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                //content = Encoding.ASCII.GetString(state.buffer);
                content = state.sb.ToString();
                //Console.WriteLine("read: "+content);
                if (content.IndexOf("\n") > -1)
                {
                    state.sb = new StringBuilder();
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    //    content.Length, content);

                    GreeHandlerResponse response = GreeHandler.process(content);
                    if (response.text != "")
                    {
                        state.keepAlive = response.keepAlive;
                        Send(state, response.text);
                    }

                    if (response.keepAlive)
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);
                    }
                }
                //else
                //{
                    // Not all data received. Get more.  
                //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                //        new AsyncCallback(ReadCallback), state);
                //}
                
                
            }
        }

        private static void Send(StateObject state, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            byte[] newArray = new byte[byteData.Length + 1];
            byteData.CopyTo(newArray, 0);
            newArray[byteData.Length] = 0x0a;
            
            Socket handler = state.workSocket;

            // Begin sending the data to the remote device.  
            handler.BeginSend(newArray, 0, newArray.Length, 0,
                new AsyncCallback(SendCallback), state);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                StateObject state = (StateObject) ar.AsyncState;
                Socket handler = state.workSocket;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                if (!state.keepAlive)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    Console.WriteLine("Connection shutdown");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}