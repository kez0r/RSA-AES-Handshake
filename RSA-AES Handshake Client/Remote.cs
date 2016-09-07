using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RSA_AES_Handshake_Client
{
    public class Remote
    {
        static readonly TcpClient ClientSocket = new TcpClient();

        ///<summary>Transmit [data] to [ip]:[port] using TCP socket.</summary>
        public static void TcpTransmit(string data, string ipAddress, int port = 8888)
        {
            try
            {
                Console.WriteLine("Client >> Sending data: {0}", data);
                
                //encrypt the data string 
                string encData = Convert.ToBase64String(Crypto.AESEncryptToBytes(data, Crypto.aesSessionKey, Crypto.aesSessionIV));

                //create network stream
                NetworkStream serverStream = ClientSocket.GetStream();

                //write to stream
                byte[] outStream = Encoding.UTF8.GetBytes(encData);
                WriteNetworkStream(outStream, serverStream);

                //receive server response and decode
                byte[] inStream = ReadNetworkStream(ClientSocket, serverStream);
                var returnData = Encoding.UTF8.GetString(inStream);
                returnData = returnData.Substring(0, returnData.IndexOf("\0", StringComparison.Ordinal));

                //decrypt data with session variables
                var decResponse = Crypto.AESDecryptFromBytes(Convert.FromBase64String(returnData), Crypto.aesSessionKey, Crypto.aesSessionIV);

                Console.WriteLine("Client >> Server response: \"{0}\"", decResponse);
            }
            catch (Exception e) { Console.WriteLine("Client >> Error: {0}", e.Message); }
        }

        ///<summary>Attempt to connect to [ip]:[port] and carry out RSA/AES key exchange handshake.</summary>
        public static bool InitiateHandshake(string ipAddress, int port = 8888)
        {
            try
            {
                Console.WriteLine("Client >> Attempting to connect to: {0}:{1}", ipAddress, port);
                
                //initialize objects
                var rsa = new RSACryptoServiceProvider(2048);
                var rsaPrivKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ToXmlString(true))); //session rsa private key

                ClientSocket.Connect(ipAddress, port); //connect to server

                NetworkStream networkStream = ClientSocket.GetStream();

                //transmit 'request token' (just an rsa public key in this example, some syntax/delimiters could be added if desired)
                byte[] requestToken = Convert.FromBase64String(rsaPrivKey);
                WriteNetworkStream(requestToken, networkStream);

                //return 'handshake token' containing AES session variables from server
                var hsTkBuffer = ReadNetworkStream(ClientSocket, networkStream);
                var hsEncToken = Encoding.UTF8.GetString(hsTkBuffer).Substring(0, Encoding.UTF8.GetString(hsTkBuffer).IndexOf("\0", StringComparison.Ordinal));
                
                //attempt to decrypt 'handshake token' & convert to base64 string
                var decTkStr = Encoding.UTF8.GetString(Crypto.RSADecrypt(Convert.FromBase64String(hsEncToken), rsa.ExportParameters(true), false));

                Console.WriteLine("Client >> Handshake token received: {0}", decTkStr); //print token to console

                //store session aes key & iv from 'handshake token'
                var keyInfo = Regex.Split(decTkStr, @"\|\|"); //split key info at the delimiter into an array
                Crypto.aesSessionKey = Convert.FromBase64String(keyInfo[0]); //key
                Crypto.aesSessionIV = Convert.FromBase64String(keyInfo[1]); //iv
                
                //encrypt and transmit 'challenge token' with AES session information received from 'handshake token' 
                var challenge = Convert.ToBase64String(Crypto.AESEncryptToBytes("challenge", Crypto.aesSessionKey, Crypto.aesSessionIV));
                byte[] chalBuffer = Encoding.UTF8.GetBytes(challenge);
                WriteNetworkStream(chalBuffer, networkStream);

                Console.WriteLine("Client >> Handshake challenge sent!");

                //receive challenge response from server
                var chalResponseBuffer = ReadNetworkStream(ClientSocket, networkStream);
                var encChalResponse = Encoding.UTF8.GetString(chalResponseBuffer).Substring(0, Encoding.UTF8.GetString(chalResponseBuffer).IndexOf("\0", StringComparison.Ordinal));

                //decrypt challenge response
                var decChalResponse = Crypto.AESDecryptFromBytes(Convert.FromBase64String(encChalResponse), Crypto.aesSessionKey, Crypto.aesSessionIV);

                //return false if challenge is not accepted
                if (decChalResponse != "accepted") return false;

                Console.WriteLine("Client >> Handshake successful!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Client >> Handshake error: {0}", e.Message);
                return false;
            }
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
    }
}
