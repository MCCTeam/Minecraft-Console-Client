/**********************************************************************
 * Copyright (c) 2010, j. montgomery                                  *
 * All rights reserved.                                               *
 *                                                                    *
 * Redistribution and use in source and binary forms, with or without *
 * modification, are permitted provided that the following conditions *
 * are met:                                                           *
 *                                                                    *
 * + Redistributions of source code must retain the above copyright   *
 *   notice, this list of conditions and the following disclaimer.    *
 *                                                                    *
 * + Redistributions in binary form must reproduce the above copyright*
 *   notice, this list of conditions and the following disclaimer     *
 *   in the documentation and/or other materials provided with the    *
 *   distribution.                                                    *
 *                                                                    *
 * + Neither the name of j. montgomery's employer nor the names of    *
 *   its contributors may be used to endorse or promote products      *
 *   derived from this software without specific prior written        *
 *   permission.                                                      *
 *                                                                    *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS*
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT  *
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS  *
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE     *
 * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,*
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES           *
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR *
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) *
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,*
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)      *
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED*
 * OF THE POSSIBILITY OF SUCH DAMAGE.                                 *
 **********************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Security.Permissions;
using System.Text;

using DnDns.Enums;
using DnDns.Records;
using DnDns.Security;

namespace DnDns.Query
{
	/// <summary>
    /// Summary description for DnsQueryRequest.
	/// </summary>
	public class DnsQueryRequest  : DnsQueryBase
	{
        private static Random r = new Random();

        private DnsPermission _dnsPermissions;

        private int _bytesSent = 0;
        private int _socketTimeout = 5000;
        
        /// <summary>
        /// The number of bytes sent to query the DNS Server.
        /// </summary>
        public int BytesSent
        {
            get { return _bytesSent; } 
        }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds that a DnsQueryRequest will wait to receive data once a read operation is initiated.
        /// Defauts to 5 seconds (5000 ms)
        /// </summary>
        public int Timeout
        {
            get { return _socketTimeout; }
            set { _socketTimeout = value; } 
        }

        #region Constructors
        public DnsQueryRequest() 
		{
            _dnsPermissions = new DnsPermission(PermissionState.Unrestricted);

            // Construct the class with some defaults
            _transactionId = (ushort) r.Next();
            _flags = 0;
            _queryResponse = QueryResponse.Query;
            this._opCode = OpCode.QUERY;
            // Recursion Desired
            this._nsFlags = NsFlags.RD;
            this._questions = 1;
		}

		#endregion Constructors

		private byte[] BuildQuery(string host) 
		{
			string newHost;
			int newLocation = 0;
			int oldLocation = 0;

			MemoryStream ms = new MemoryStream();

			host = host.Trim();
			// decide how to build this query based on type
			switch (_nsType) 
			{
				case NsType.PTR:
					IPAddress queryIP = IPAddress.Parse(host);

					// pointer should be translated as follows
					// 209.115.22.3 -> 3.22.115.209.in-addr.arpa
					char[] ipDelim = new char[] {'.'};

					string[] s = host.Split(ipDelim,4);
					newHost = String.Format("{0}.{1}.{2}.{3}.in-addr.arpa", s[3], s[2], s[1], s[0]);
					break;
				default:
					newHost = host;
					break;
			}
			
			// Package up the host
			while(oldLocation < newHost.Length) 
			{
				newLocation = newHost.IndexOf(".", oldLocation);	
				
				if (newLocation == -1) newLocation = newHost.Length;

				byte subDomainLength = (byte)(newLocation - oldLocation);
				char[] sub = newHost.Substring(oldLocation, subDomainLength).ToCharArray();
				
				ms.WriteByte(subDomainLength);
				ms.Write(Encoding.ASCII.GetBytes(sub, 0, sub.Length), 0, sub.Length);

				oldLocation = newLocation + 1;
			}

			// Terminate the domain name w/ a 0x00. 
			ms.WriteByte(0x00);

			return ms.ToArray();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="queryType"></param>
        /// <param name="queryClass"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public DnsQueryResponse Resolve(string host, NsType queryType, NsClass queryClass, ProtocolType protocol)
        {
            return Resolve(host, queryType, queryClass, protocol, null);
        }

        public DnsQueryResponse Resolve(string host, NsType queryType, NsClass queryClass, ProtocolType protocol, TsigMessageSecurityProvider provider)
        {
            string dnsServer = string.Empty;

            // Test for Unix/Linux OS
            if (Tools.IsPlatformLinuxUnix())
            {
                dnsServer = Tools.DiscoverUnixDnsServerAddress();
            }
            else
            {
                IPAddressCollection dnsServerCollection = Tools.DiscoverDnsServerAddresses();
                if (dnsServerCollection.Count == 0)
                    throw new Exception("Couldn't detect local DNS Server.");

                dnsServer = dnsServerCollection[0].ToString();
            }

            if (String.IsNullOrEmpty(dnsServer))
                throw new Exception("Couldn't detect local DNS Server.");

            return Resolve(dnsServer, host, queryType, queryClass, protocol, provider);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dnsServer"></param>
        /// <param name="host"></param>
        /// <param name="queryType"></param>
        /// <param name="queryClass"></param>
        /// <param name="protocol"></param>
        /// <param name="messageSecurityProvider">The instance of the message security provider to use to secure the DNS request.</param>
        /// <returns>A <see cref="T:DnDns.Net.Dns.DnsQueryResponse"></see> instance that contains the Dns Answer for the request query.</returns>
        /// <PermissionSet>
        ///     <IPermission class="System.Net.DnsPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
        /// </PermissionSet>
        public DnsQueryResponse Resolve(string dnsServer, string host, NsType queryType, NsClass queryClass, ProtocolType protocol, IMessageSecurityProvider messageSecurityProvider) 
		{
            // Do stack walk and Demand all callers have DnsPermission.
            _dnsPermissions.Demand();

            byte[] bDnsQuery = this.BuildDnsRequest(host, queryType, queryClass, protocol, messageSecurityProvider);
			
			// Connect to DNS server and get the record for the current server.
			IPHostEntry ipe = System.Net.Dns.GetHostEntry(dnsServer);
			IPAddress ipa = ipe.AddressList[0];
			IPEndPoint ipep = new IPEndPoint(ipa, (int)UdpServices.Domain);

            byte[] recvBytes = null;

            switch (protocol)
            {
                case ProtocolType.Tcp:
                    {
                        recvBytes = ResolveTcp(bDnsQuery, ipep);
                        break;
                    }
                case ProtocolType.Udp:
                    {
                        recvBytes = ResolveUdp(bDnsQuery, ipep);
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException("Invalid Protocol: " + protocol);
                    }
            }

            Trace.Assert(recvBytes != null, "Failed to retrieve data from the remote DNS server.");

			DnsQueryResponse dnsQR = new DnsQueryResponse();
			
			dnsQR.ParseResponse(recvBytes, protocol);

			return dnsQR;
		}

        private byte[] ResolveUdp(byte[] bDnsQuery, IPEndPoint ipep)
        {
            // UDP messages, data size = 512 octets or less
            UdpClient udpClient = new UdpClient();
            byte[] recvBytes = null;

            try
            {
                udpClient.Client.ReceiveTimeout = _socketTimeout;
                udpClient.Connect(ipep);
                udpClient.Send(bDnsQuery, bDnsQuery.Length);
                recvBytes = udpClient.Receive(ref ipep);
            }
            finally
            {
                udpClient.Close();
            }
            return recvBytes;
        }

        private static byte[] ResolveTcp(byte[] bDnsQuery, IPEndPoint ipep)
        {
            TcpClient tcpClient = new TcpClient();
            byte[] recvBytes = null;

            try
            {
                tcpClient.Connect(ipep);

                NetworkStream netStream = tcpClient.GetStream();
                BinaryReader netReader = new System.IO.BinaryReader(netStream);

                netStream.Write(bDnsQuery, 0, bDnsQuery.Length);

                // wait until data is avail
                while (!netStream.DataAvailable) ;

                if (tcpClient.Connected && netStream.DataAvailable)
                {
                    // Read first two bytes to find out the length of the response
                    byte[] bLen = new byte[2];
                    
                    // NOTE: The order of the next two lines matter. Do not reorder
                    // Array indexes are also intentionally reversed
                    bLen[1] = (byte)netStream.ReadByte();
                    bLen[0] = (byte)netStream.ReadByte();

                    UInt16 length = BitConverter.ToUInt16(bLen, 0);

                    recvBytes = new byte[length];
                    netStream.Read(recvBytes, 0, length);
                }
            }
            finally
            {
                tcpClient.Close();
            }
            return recvBytes;
        }

        private byte[] BuildDnsRequest(string host, NsType queryType, NsClass queryClass, ProtocolType protocol, IMessageSecurityProvider messageSecurityProvider)
        {
            // Combind the NsFlags with our constant flags
            ushort flags = (ushort)((ushort)_queryResponse | (ushort)_opCode | (ushort)_nsFlags);
            this._flags = flags;

            //NOTE: This limits the librarys ablity to issue multiple queries per request.
            this._nsType = queryType;
            this._nsClass = queryClass;
            this._name = host;

            if(messageSecurityProvider != null)
            {
                messageSecurityProvider.SecureMessage(this);
            }

            byte[] bDnsQuery = GetMessageBytes();

            // Add two byte prefix that contains the packet length per RFC 1035 section 4.2.2
            if (protocol == ProtocolType.Tcp)
            {
                // 4.2.2. TCP usageMessages sent over TCP connections use server port 53 (decimal).  
                // The message is prefixed with a two byte length field which gives the message 
                // length, excluding the two byte length field.  This length field allows the 
                // low-level processing to assemble a complete message before beginning to parse 
                // it.
                int len = bDnsQuery.Length;
                Array.Resize<byte>(ref bDnsQuery, len + 2);
                Array.Copy(bDnsQuery, 0, bDnsQuery, 2, len);
                bDnsQuery[0] = (byte)((len >> 8) & 0xFF);
                bDnsQuery[1] = (byte)((len & 0xFF));
            }

            return bDnsQuery;
        }

        internal byte[] GetMessageBytes()
        {
            MemoryStream memoryStream = new MemoryStream();
            byte[] data = new byte[2];

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_transactionId) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_flags) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_questions) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_answerRRs) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_authorityRRs) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_additionalRecords.Count) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = DnsHelpers.CanonicaliseDnsName(_name, false);
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_nsType) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_nsClass) >> 16));
            memoryStream.Write(data, 0, data.Length);

            foreach (IDnsRecord dnsRecord in AdditionalRRecords)
            {
                data = dnsRecord.GetMessageBytes();
                memoryStream.Write(data, 0, data.Length);
            }

            Trace.WriteLine(String.Format("The message bytes: {0}", DnsHelpers.DumpArrayToString(memoryStream.ToArray())));

            return memoryStream.ToArray();
        }
	}
}
