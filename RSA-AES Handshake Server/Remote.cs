using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace RSA_AES_Handshake_Server
{
    public class Remote
    {
        ///<summary>Attempt RSA encrypted handshake to exchange AES key/iv for further communication.</summary>
        public static bool KeyExchangeHandshake(TcpClient client) 
        {
            try
            {
                NetworkStream networkStream = client.GetStream();

                //receive request token from client (containing RSA public key xml string)
                var hsReqBuffer = ReadNetworkStream(client, networkStream);
                var hsRSAKey = Encoding.UTF8.GetString(hsReqBuffer).Substring(0, Encoding.UTF8.GetString(hsReqBuffer).IndexOf("\0", StringComparison.Ordinal));

                //generate handshake token (containing AES key/iv) for current session. default key size = 256, method accepts a keySize argument (optional)
                byte[] tkBytes = Crypto.GenerateHandshakeToken(); 

                //encrypt and encode 'handshake token' with RSA public key and transmit to client
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(hsRSAKey); //load rsa public key info
                    
                    var encTkStr = Convert.ToBase64String(Crypto.RSAEncrypt(tkBytes, rsa.ExportParameters(false), false));
                    
                    byte[] hsBuffer = Encoding.UTF8.GetBytes(encTkStr);
                    WriteNetworkStream(hsBuffer, networkStream);
                }

                //return and decode 'handshake challenge' response
                var hsChalBuffer = ReadNetworkStream(client, networkStream);
                var hsEncData = Encoding.UTF8.GetString(hsChalBuffer).Substring(0, Encoding.UTF8.GetString(hsChalBuffer).IndexOf("\0", StringComparison.Ordinal));

                //decrypt handshake response with session keys
                var hsResponse = Crypto.AESDecryptFromBytes(Convert.FromBase64String(hsEncData), Crypto.aesSessionKey, Crypto.aesSessionIV);

                //check 'handshake challenge' and respond with status
                if (hsResponse == "challenge")
                {
                    var hsStatus = Convert.ToBase64String(Crypto.AESEncryptToBytes("accepted", Crypto.aesSessionKey, Crypto.aesSessionIV)); //encrypt success response
                    
                    byte[] outStream = Encoding.UTF8.GetBytes(hsStatus);
                    WriteNetworkStream(outStream, networkStream);

                    Console.WriteLine("Server >> (TCP) Handshake Challenge Accepted!");
                    return true;
                }
                else
                {
                    byte[] outStream = Encoding.UTF8.GetBytes("failed");
                    WriteNetworkStream(outStream, networkStream);
                    
                    Console.WriteLine("Server >> (TCP) Handshake Challenge Rejected!");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server >> (TCP) Handshake Exception: {0}", e.Message);
                return false;
            }
        }

        ///<summary>Transmit [data] to [ip]:[port] using TCP socket.</summary>
        public static void TCPShellListener(int port = 8888, bool recursion = true) 
        {
            //create and start tcp listener
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Console.WriteLine("Server >> (TCP) Listener started! [ip: {0}] [port: {1}]", GetLocalIPAddress(), port);

            //wait for client connection (blocking)
            var client = server.AcceptTcpClient();

            Console.WriteLine("Server >> (TCP) Accepted connection from: [{0}]", ((IPEndPoint)client.Client.RemoteEndPoint).Address);

            //attempt rsa encrypted handshake to exchange AES key/iv for further communication (recursively restart listener upon error if specified)
            if (!KeyExchangeHandshake(client)) { CloseListener(client, server, recursion); return; }

            //keep connection alive until client terminates it
            while (client.Connected)
            {
                try
                {
                    NetworkStream networkStream = client.GetStream(); //return client network stream

                    //return & decode data buffer
                    byte[] fromBuffer = ReadNetworkStream(client, networkStream);
                    var clientData = Encoding.UTF8.GetString(fromBuffer).Substring(0, Encoding.UTF8.GetString(fromBuffer).IndexOf("\0", StringComparison.Ordinal));

                    //decrypt data using session aes key/iv
                    var decCommand = Crypto.AESDecryptFromBytes(Convert.FromBase64String(clientData), Crypto.aesSessionKey, Crypto.aesSessionIV);
                    
                    Console.WriteLine("Server >> (TCP) Data received: [{0}]", decCommand);

                    //encrypt response using session aes key/iv
                    var encResponse = Convert.ToBase64String(Crypto.AESEncryptToBytes("Received data: " + decCommand, Crypto.aesSessionKey, Crypto.aesSessionIV));

                    //send response string back to client
                    byte[] sendBuffer = Encoding.UTF8.GetBytes(encResponse);
                    WriteNetworkStream(sendBuffer, networkStream);

                    Console.WriteLine("Server >> (TCP) Response sent to: [{0}]", ((IPEndPoint)client.Client.RemoteEndPoint).Address);
                }
                catch (ArgumentOutOfRangeException) //usually indicates client has closed the connection. TODO: implement more precise alternative
                {
                    Console.WriteLine("Server >> (TCP) Client has closed connection...");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server >> (TCP) Exception encountered! Closing connection...");
                    Console.WriteLine("Server >> (TCP) Details: {0}", ex);
                    break;
                }
            }

            //clear session keys
            Crypto.aesSessionKey = null;
            Crypto.aesSessionIV = null;

            //close client connection, stop tcp listener and restart if recursion = true
            CloseListener(client, server, recursion);
        }

        public static byte[] ReadNetworkStream(TcpClient client, NetworkStream networkStream) 
        {
            var fromBuffer = new byte[10025]; //create client data buffer
            networkStream.Read(fromBuffer, 0, client.ReceiveBufferSize); //read data from client network stream

            return fromBuffer;
        }

        public static void WriteNetworkStream(byte[] sendBuffer, NetworkStream networkStream) 
        {
            networkStream.Write(sendBuffer, 0, sendBuffer.Length);
            networkStream.Flush();
        }

        private static void CloseListener(TcpClient client, TcpListener server, bool recursion) 
        {
            client.Close();
            server.Stop();

            if (recursion)
                TCPShellListener();
        }

        public static string GetLocalIPAddress() 
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            
            foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
                return ip.ToString();
            throw new Exception("Error: Cannot locate Local IP Address!");
        }
    }
}
