/*
 *  Authors:  Benton Stark
 * 
 *  Copyright (c) 2007-2012 Starksoft, LLC (http://www.starksoft.com) 
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace Starksoft.Net.Proxy
{
    /// <summary>
    /// Socks4 connection proxy class.  This class implements the Socks4 standard proxy protocol.
    /// </summary>
    /// <remarks>
    /// This class implements the Socks4 proxy protocol standard for TCP communciations.
    /// </remarks>
    public class Socks4ProxyClient : IProxyClient
    {
        private const int WAIT_FOR_DATA_INTERVAL = 50;   // 50 ms
        private const int WAIT_FOR_DATA_TIMEOUT = 15000; // 15 seconds
        private const string PROXY_NAME = "SOCKS4";
        private TcpClient _tcpClient;
        private TcpClient _tcpClientCached;

        private string _proxyHost;
        private int _proxyPort;
        private string _proxyUserId;

        /// <summary>
        /// Default Socks4 proxy port.
        /// </summary>
        internal const int SOCKS_PROXY_DEFAULT_PORT = 1080;
        /// <summary>
        /// Socks4 version number.
        /// </summary>
        internal const byte SOCKS4_VERSION_NUMBER = 4;
        /// <summary>
        /// Socks4 connection command value.
        /// </summary>
        internal const byte SOCKS4_CMD_CONNECT = 0x01;
        /// <summary>
        /// Socks4 bind command value.
        /// </summary>
        internal const byte SOCKS4_CMD_BIND = 0x02;
        /// <summary>
        /// Socks4 reply request grant response value.
        /// </summary>
        internal const byte SOCKS4_CMD_REPLY_REQUEST_GRANTED = 90;
        /// <summary>
        /// Socks4 reply request rejected or failed response value.
        /// </summary>
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED = 91;
        /// <summary>
        /// Socks4 reply request rejected becauase the proxy server can not connect to the IDENTD server value.
        /// </summary>
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD = 92;
        /// <summary>
        /// Socks4 reply request rejected because of a different IDENTD server.
        /// </summary>
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD = 93;

        /// <summary>
        /// Create a Socks4 proxy client object.  The default proxy port 1080 is used.
        /// </summary>
        public Socks4ProxyClient() { }

        /// <summary>
        /// Creates a Socks4 proxy client object using the supplied TcpClient object connection.
        /// </summary>
        /// <param name="tcpClient">A TcpClient connection object.</param>
        public Socks4ProxyClient(TcpClient tcpClient)
        {
            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            _tcpClientCached = tcpClient;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.  The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyUserId">Proxy user identification information.</param>
        public Socks4ProxyClient(string proxyHost, string proxyUserId) 
        {
            if (String.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException("proxyHost");
            
            if (proxyUserId == null)
                throw new ArgumentNullException("proxyUserId");

            _proxyHost = proxyHost;
            _proxyPort = SOCKS_PROXY_DEFAULT_PORT;
            _proxyUserId = proxyUserId;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        /// <param name="proxyUserId">Proxy user identification information.</param>
        public Socks4ProxyClient(string proxyHost, int proxyPort, string proxyUserId)
        {
            if (String.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException("proxyHost");

            if (proxyPort <= 0 || proxyPort > 65535)
                throw new ArgumentOutOfRangeException("proxyPort", "port must be greater than zero and less than 65535");
            
            if (proxyUserId == null)
                throw new ArgumentNullException("proxyUserId");
            
            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
            _proxyUserId = proxyUserId;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.  The default proxy port 1080 is used.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        public Socks4ProxyClient(string proxyHost)
        {
            if (String.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException("proxyHost");
            
            _proxyHost = proxyHost;
            _proxyPort = SOCKS_PROXY_DEFAULT_PORT;
        }

        /// <summary>
        /// Create a Socks4 proxy client object.
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        public Socks4ProxyClient(string proxyHost, int proxyPort)
        {
            if (String.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException("proxyHost");

            if (proxyPort <= 0 || proxyPort > 65535)
                throw new ArgumentOutOfRangeException("proxyPort", "port must be greater than zero and less than 65535");

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
        }

        /// <summary>
        /// Gets or sets host name or IP address of the proxy server.
        /// </summary>
        public string ProxyHost
        {
            get { return _proxyHost; }
            set { _proxyHost = value; }
        }

        /// <summary>
        /// Gets or sets port used to connect to proxy server.
        /// </summary>
        public int ProxyPort
        {
            get { return _proxyPort; }
            set { _proxyPort = value; }
        }

        /// <summary>
        /// Gets String representing the name of the proxy. 
        /// </summary>
        /// <remarks>This property will always return the value 'SOCKS4'</remarks>
        virtual public string ProxyName
        {
            get { return PROXY_NAME; }
        }

        /// <summary>
        /// Gets or sets proxy user identification information.
        /// </summary>
        public string ProxyUserId
        {
            get { return _proxyUserId; }
            set { _proxyUserId = value; }
        }

        /// <summary>
        /// Gets or sets the TcpClient object. 
        /// This property can be set prior to executing CreateConnection to use an existing TcpClient connection.
        /// </summary>
        public TcpClient TcpClient
        {
            get { return _tcpClientCached; }
            set { _tcpClientCached = value; }
        }

        /// <summary>
        /// Creates a TCP connection to the destination host through the proxy server
        /// host.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address of the destination server.</param>
        /// <param name="destinationPort">Port number to connect to on the destination server.</param>
        /// <returns>
        /// Returns an open TcpClient object that can be used normally to communicate
        /// with the destination server
        /// </returns>
        /// <remarks>
        /// This method creates a connection to the proxy server and instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port.  
        /// </remarks>
        public TcpClient CreateConnection(string destinationHost, int destinationPort)
        {
            if (String.IsNullOrEmpty(destinationHost))
                throw new ArgumentNullException("destinationHost");

            if (destinationPort <= 0 || destinationPort > 65535)
                throw new ArgumentOutOfRangeException("destinationPort", "port must be greater than zero and less than 65535");

            try
            {
                // if we have no cached tcpip connection then create one
                if (_tcpClientCached == null)
                {
                    if (String.IsNullOrEmpty(_proxyHost))
                        throw new ProxyException("ProxyHost property must contain a value.");

                    if (_proxyPort <= 0 || _proxyPort > 65535)
                        throw new ProxyException("ProxyPort value must be greater than zero and less than 65535");

                    //  create new tcp client object to the proxy server
                    _tcpClient = new TcpClient();

                    // attempt to open the connection
                    _tcpClient.Connect(_proxyHost, _proxyPort);
                }
                else
                {
                    _tcpClient = _tcpClientCached;
                }

                //  send connection command to proxy host for the specified destination host and port
                SendCommand(_tcpClient.GetStream(), SOCKS4_CMD_CONNECT, destinationHost, destinationPort, _proxyUserId);

                // remove the private reference to the tcp client so the proxy object does not keep it
                // return the open proxied tcp client object to the caller for normal use
                TcpClient rtn = _tcpClient;
                _tcpClient = null;
                return rtn;
            }
            catch (Exception ex)
            {
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)), ex);
            }
        }


        /// <summary>
        /// Sends a command to the proxy server.
        /// </summary>
        /// <param name="proxy">Proxy server data stream.</param>
        /// <param name="command">Proxy byte command to execute.</param>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Destination port number</param>
        /// <param name="userId">IDENTD user ID value.</param>
        internal virtual void SendCommand(NetworkStream proxy, byte command, string destinationHost, int destinationPort, string userId)
        {
            // PROXY SERVER REQUEST
            // The client connects to the SOCKS server and sends a CONNECT request when
            // it wants to establish a connection to an application server. The client
            // includes in the request packet the IP address and the port number of the
            // destination host, and userid, in the following format.
            //
            //        +----+----+----+----+----+----+----+----+----+----+....+----+
            //        | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            //        +----+----+----+----+----+----+----+----+----+----+....+----+
            // # of bytes:	   1    1      2              4           variable       1
            //
            // VN is the SOCKS protocol version number and should be 4. CD is the
            // SOCKS command code and should be 1 for CONNECT request. NULL is a byte
            // of all zero bits.         

            //  userId needs to be a zero length string so that the GetBytes method
            //  works properly
            if (userId == null)
                userId = "";

            byte[] destIp = GetIPAddressBytes(destinationHost);
            byte[] destPort = GetDestinationPortBytes(destinationPort);
            byte[] userIdBytes = ASCIIEncoding.ASCII.GetBytes(userId);   
            byte[] request = new byte[9 + userIdBytes.Length];

            //  set the bits on the request byte array
            request[0] = SOCKS4_VERSION_NUMBER;
            request[1] = command;
            destPort.CopyTo(request, 2);
            destIp.CopyTo(request, 4);
            userIdBytes.CopyTo(request, 8);
            request[8 + userIdBytes.Length] = 0x00;  // null (byte with all zeros) terminator for userId

            // send the connect request
            proxy.Write(request, 0, request.Length);

            // wait for the proxy server to respond
            WaitForData(proxy);

            // PROXY SERVER RESPONSE
            // The SOCKS server checks to see whether such a request should be granted
            // based on any combination of source IP address, destination IP address,
            // destination port number, the userid, and information it may obtain by
            // consulting IDENT, cf. RFC 1413.  If the request is granted, the SOCKS
            // server makes a connection to the specified port of the destination host.
            // A reply packet is sent to the client when this connection is established,
            // or when the request is rejected or the operation fails. 
            //
            //        +----+----+----+----+----+----+----+----+
            //        | VN | CD | DSTPORT |      DSTIP        |
            //        +----+----+----+----+----+----+----+----+
            // # of bytes:	   1    1      2              4
            //
            // VN is the version of the reply code and should be 0. CD is the result
            // code with one of the following values:
            //
            //    90: request granted
            //    91: request rejected or failed
            //    92: request rejected becuase SOCKS server cannot connect to
            //        identd on the client
            //    93: request rejected because the client program and identd
            //        report different user-ids
            //
            // The remaining fields are ignored.
            //
            // The SOCKS server closes its connection immediately after notifying
            // the client of a failed or rejected request. For a successful request,
            // the SOCKS server gets ready to relay traffic on both directions. This
            // enables the client to do I/O on its connection as if it were directly
            // connected to the application server.

            // create an 8 byte response array  
            byte[] response = new byte[8];
            
            // read the resonse from the network stream
            proxy.Read(response, 0, 8);

            //  evaluate the reply code for an error condition
            if (response[1] != SOCKS4_CMD_REPLY_REQUEST_GRANTED)
                HandleProxyCommandError(response, destinationHost, destinationPort);
        }

        /// <summary>
        /// Translate the host name or IP address to a byte array.
        /// </summary>
        /// <param name="destinationHost">Host name or IP address.</param>
        /// <returns>Byte array representing IP address in bytes.</returns>
        internal byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            //  if the address doesn't parse then try to resolve with dns
            if (!IPAddress.TryParse(destinationHost, out ipAddr))
            {
                try
                {
                    ipAddr = Dns.GetHostEntry(destinationHost).AddressList[0];
                }
                catch (Exception ex)
                {
                    throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "A error occurred while attempting to DNS resolve the host name {0}.", destinationHost), ex);
                }
            }
           
            // return address bytes
            return ipAddr.GetAddressBytes();            
        }

        /// <summary>
        /// Translate the destination port value to a byte array.
        /// </summary>
        /// <param name="value">Destination port.</param>
        /// <returns>Byte array representing an 16 bit port number as two bytes.</returns>
        internal byte[] GetDestinationPortBytes(int value)
        {
            byte[] array = new byte[2];
            array[0] = Convert.ToByte(value / 256);
            array[1] = Convert.ToByte(value % 256);
            return array;
        }

        /// <summary>
        /// Receive a byte array from the proxy server and determine and handle and errors that may have occurred.
        /// </summary>
        /// <param name="response">Proxy server command response as a byte array.</param>
        /// <param name="destinationHost">Destination host.</param>
        /// <param name="destinationPort">Destination port number.</param>
        internal void HandleProxyCommandError(byte[] response, string destinationHost, int destinationPort)
        {

            if (response == null)
                throw new ArgumentNullException("response"); 

            //  extract the reply code
            byte replyCode = response[1];
           
            //  extract the ip v4 address (4 bytes)
            byte[] ipBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                ipBytes[i] = response[i + 4];
            
            //  convert the ip address to an IPAddress object
            IPAddress ipAddr = new IPAddress(ipBytes);

            //  extract the port number big endian (2 bytes)
            byte[] portBytes = new byte[2];
            portBytes[0] = response[3];
            portBytes[1] = response[2];
            Int16 port = BitConverter.ToInt16(portBytes, 0);

            // translate the reply code error number to human readable text
            string proxyErrorText;
            switch (replyCode)
            {
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED:
                    proxyErrorText = "connection request was rejected or failed";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD:
                    proxyErrorText = "connection request was rejected because SOCKS destination cannot connect to identd on the client";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD: 
                    proxyErrorText = "connection request rejected because the client program and identd report different user-ids";
                    break;
                default:
                    proxyErrorText = String.Format(CultureInfo.InvariantCulture, "proxy client received an unknown reply with the code value '{0}' from the proxy destination", replyCode.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            //  build the exeception message string
            string exceptionMsg = String.Format(CultureInfo.InvariantCulture, "The {0} concerning destination host {1} port number {2}.  The destination reported the host as {3} port {4}.", proxyErrorText, destinationHost, destinationPort, ipAddr.ToString(), port.ToString(CultureInfo.InvariantCulture));

            //  throw a new application exception 
            throw new ProxyException(exceptionMsg);
        }

        internal void WaitForData(NetworkStream stream)
        {
            int sleepTime = 0;
            while (!stream.DataAvailable)
            {
                Thread.Sleep(WAIT_FOR_DATA_INTERVAL);
                sleepTime += WAIT_FOR_DATA_INTERVAL;
                if (sleepTime > WAIT_FOR_DATA_TIMEOUT)
                    throw new ProxyException("A timeout while waiting for the proxy destination to respond.");
            }
        }


#region "Async Methods"

        private BackgroundWorker _asyncWorker;
        private Exception _asyncException;
        bool _asyncCancelled;

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is running.
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is running; otherwise, false.
        /// </remarks>
        public bool IsBusy
        {
            get { return _asyncWorker == null ? false : _asyncWorker.IsBusy; }
        }

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is cancelled.
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is cancelled; otherwise, false.
        /// </remarks>
        public bool IsAsyncCancelled
        {
            get { return _asyncCancelled; }
        }

        /// <summary>
        /// Cancels any asychronous operation that is currently active.
        /// </summary>
        public void CancelAsync()
        {
            if (_asyncWorker != null && !_asyncWorker.CancellationPending && _asyncWorker.IsBusy)
            {
                _asyncCancelled = true;
                _asyncWorker.CancelAsync();
            }
        }

        private void CreateAsyncWorker()
        {
            if (_asyncWorker != null)
                _asyncWorker.Dispose();
            _asyncException = null;
            _asyncWorker = null;
            _asyncCancelled = false;
            _asyncWorker = new BackgroundWorker();
        }

        /// <summary>
        /// Event handler for CreateConnectionAsync method completed.
        /// </summary>
        public event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;

        /// <summary>
        /// Asynchronously creates a remote TCP connection through a proxy server to the destination host on the destination port
        /// using the supplied open TcpClient object with an open connection to proxy server.
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns TcpClient object that can be used normally to communicate
        /// with the destination server.  
        /// </returns>
        /// <remarks>
        /// This instructs the proxy server to make a pass through connection to the specified destination host on the specified
        /// port.  
        /// </remarks>
        public void CreateConnectionAsync(string destinationHost, int destinationPort)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
                throw new InvalidOperationException("The Socks4/4a object is already busy executing another asynchronous operation.  You can only execute one asychronous method at a time.");

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(CreateConnectionAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CreateConnectionAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = destinationHost;
            args[1] = destinationPort;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void CreateConnectionAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                e.Result = CreateConnection((string)args[0], (int)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void CreateConnectionAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (CreateConnectionAsyncCompleted != null)
                CreateConnectionAsyncCompleted(this, new CreateConnectionAsyncCompletedEventArgs(_asyncException, _asyncCancelled, (TcpClient)e.Result));
        }
        
#endregion

    }

}
