using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using DnDns.Enums;
using DnDns.Query;
using DnDns.Records;

namespace DnDns.Security
{
    /// <summary>
    /// Implements TSIG signing of DNS messages as per RFC2845
    /// </summary>
    /// <remarks>This class only supports the one hashing algorithim, hmac-sha256.
    /// It would be trivial to add more.</remarks>
    public class TsigMessageSecurityProvider : IMessageSecurityProvider
    {
        private const string Hmacsha1String = "hmac-sha256.";
        
        private readonly string _name;
        private readonly string _algorithimName;
        private readonly ushort _fudge;
        private readonly byte[] _sharedKey;
        private readonly HMACSHA256 _hmac;

        /// <summary>
        /// Initalise the <see cref="TsigMessageSecurityProvider"/>
        /// </summary>
        /// <param name="name">The name of the shared key</param>
        /// <param name="sharedKey">The shared key in base64 string format</param>
        /// <param name="fudge">The signing time fudge value</param>
        public TsigMessageSecurityProvider(string name, string sharedKey, ushort fudge)
        {
            _name = name;
            _fudge = fudge;
            _sharedKey = Convert.FromBase64String(sharedKey);

            if (_sharedKey == null)
            {
                throw new ArgumentException("Argument is not a valid base64 string", "sharedKey");
            }

            _hmac = new HMACSHA256(_sharedKey);

            _algorithimName = Hmacsha1String;
        }

        #region IMessageSecurityProvider Members
        /// <summary>
        /// Apply a TSIG record to the request message.
        /// </summary>
        /// <param name="dnsQueryRequest">The <see cref="DnsQueryRequest"/> to add the security headers too.</param>
        /// <returns>A <see cref="DnsQueryRequest"/> instance with additional security attributes assigned.</returns>
        public DnsQueryRequest SecureMessage(DnsQueryRequest dnsQueryRequest)
        {
            DateTime signDateTime = DateTime.Now;
            int timeHigh;
            long timeLow;

            byte[] messageBytes = dnsQueryRequest.GetMessageBytes();
            Trace.WriteLine(String.Format("Message Header Bytes: {0}", DnsHelpers.DumpArrayToString(messageBytes)));

            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(messageBytes, 0, messageBytes.Length);

            // the shared key name
            byte[] data = DnsHelpers.CanonicaliseDnsName(_name, false);
            memoryStream.Write(data, 0, data.Length);
            data = BitConverter.GetBytes((ushort) (IPAddress.HostToNetworkOrder((ushort) NsClass.ANY) >> 16));
            memoryStream.Write(data, 0, data.Length);
            // the TTL value
            data = BitConverter.GetBytes((uint) (IPAddress.HostToNetworkOrder((uint) 0) >> 32));
            memoryStream.Write(data, 0, data.Length);
            // the algorithim name
            data = DnsHelpers.CanonicaliseDnsName(_algorithimName, true);
            memoryStream.Write(data, 0, data.Length);

            DnsHelpers.ConvertToDnsTime(signDateTime.ToUniversalTime(), out timeHigh, out timeLow);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)timeHigh) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((uint) (IPAddress.HostToNetworkOrder((uint)timeLow) >> 32));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort) (IPAddress.HostToNetworkOrder(_fudge) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort) (IPAddress.HostToNetworkOrder((ushort) RCode.NoError) >> 16));
            memoryStream.Write(data, 0, data.Length);

            // no other data
            data = BitConverter.GetBytes((ushort) (IPAddress.HostToNetworkOrder((ushort) 0) >> 16));
            memoryStream.Write(data, 0, data.Length);

            byte[] dataToHash = memoryStream.ToArray();
            Trace.WriteLine(String.Format("Data to hash: {0}", DnsHelpers.DumpArrayToString(dataToHash)));
            byte[] mac = _hmac.ComputeHash(dataToHash);
            Trace.WriteLine(String.Format("hash: {0}", DnsHelpers.DumpArrayToString(mac)));

            dnsQueryRequest.AdditionalRRecords.Add(new TSigRecord(_name, _algorithimName, RCode.NoError, _fudge, dnsQueryRequest.TransactionID, new byte[] { }, mac, signDateTime));

            return dnsQueryRequest;
        }

        #endregion
    }
}