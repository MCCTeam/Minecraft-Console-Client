using System;
using System.IO;
using System.Net;
using System.Text;
using DnDns.Enums;

namespace DnDns.Records
{
    /// <summary>
    /// Implementation of a TSIG record structure as per RFC 2845
    /// </summary>
    public sealed class TSigRecord : DnsRecordBase
    {
        private string _algorithmName;
        private RCode _error;
        private ushort _fudge;
        private ushort _originalId;
        private byte[] _otherData;
        private byte[] _mac;
        private DateTime _timeSigned;

        public string AlgorithmName
        {
            get { return _algorithmName; }
        }

        public RCode Error
        {
            get { return _error; }
        }

        public ushort Fudge
        {
            get { return _fudge; }
        }

        public ushort OriginalID
        {
            get { return _originalId; }
        }

        public byte[] OtherData
        {
            get { return _otherData; }
        }

        public byte[] Mac
        {
            get { return _mac; }
        }

        public DateTime TimeSigned
        {
            get { return _timeSigned; }
        }

        public TSigRecord(RecordHeader dnsHeader) : base(dnsHeader)
        {
        }

        public TSigRecord(string name, string algorithmName, RCode error, ushort fudge, ushort originalId, byte[] otherData, byte[] mac, DateTime timeSigned)
        {
            DnsHeader = new RecordHeader(name, NsType.TSIG, NsClass.ANY, 0);

            _algorithmName = algorithmName;
            _error = error;
            _fudge = fudge;
            _originalId = originalId;
            _otherData = otherData;
            _mac = mac;
            _timeSigned = timeSigned;
            
            if(otherData == null)
            {
                _otherData = new byte[]{};
            }
        }

        public override void ParseRecord(ref MemoryStream memoryStream)
        {
            Byte[] dataUInt16 = new byte[2];
            Byte[] dataUInt32 = new byte[4];
            
            _algorithmName = ParseName(ref memoryStream);

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            long timeHigh = (ushort) IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));
            memoryStream.Read(dataUInt32, 0, dataUInt32.Length);
            long timeLow = (uint) IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(dataUInt32, 0));
	        _timeSigned = DnsHelpers.ConvertFromDnsTime(timeLow, timeHigh);

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            _fudge = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            Int32 macLen = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));
            _mac = new byte[macLen];
            memoryStream.Read(_mac, 0, macLen);

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            _originalId = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            _error = (RCode)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));

            memoryStream.Read(dataUInt16, 0, dataUInt16.Length);
            Int32 otherLen = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(dataUInt16, 0));
            
            if(otherLen > 0)
            {
                _otherData = new byte[otherLen];
                memoryStream.Read(_otherData, 0, otherLen);
            }
            else
            {
                _otherData = null;
            }

            _answer = ToString();

        }

        public override byte[] GetMessageBytes()
        {
            MemoryStream memoryStream = new MemoryStream();

            byte[] data = DnsHeader.GetMessageBytes();
            memoryStream.Write(data,0,data.Length);
            
            long rLengthPosition = memoryStream.Position;

            data = DnsHelpers.CanonicaliseDnsName(_algorithmName, false);
            memoryStream.Write(data, 0, data.Length);

            int timeHigh;
            long timeLow;
            DnsHelpers.ConvertToDnsTime(_timeSigned.ToUniversalTime(), out timeHigh, out timeLow);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)timeHigh) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((uint)(IPAddress.HostToNetworkOrder((uint)timeLow) >> 32));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_fudge) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_mac.Length) >> 16));
            memoryStream.Write(data, 0, data.Length);
            
            memoryStream.Write(_mac, 0, _mac.Length);
            
            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder(_originalId) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_error) >> 16));
            memoryStream.Write(data, 0, data.Length);


            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_otherData.Length) >> 16));
            memoryStream.Write(data, 0, data.Length);

            if(_otherData.Length != 0)
            {
                memoryStream.Write(_otherData, 0, _otherData.Length);
            }
        
            // Add the rdata lenght
            long rlength = memoryStream.Position - rLengthPosition;

            memoryStream.Seek(rLengthPosition - 2, SeekOrigin.Begin);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)rlength) >> 16));
            memoryStream.Write(data, 0, data.Length);

            return memoryStream.ToArray();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_algorithmName);
            sb.Append(" ");
            sb.Append(_timeSigned);
            sb.Append(" ");
            sb.Append(_fudge);
            sb.Append(" ");
            sb.Append(_mac.Length);
            sb.Append(" ");
            sb.Append(Convert.ToBase64String(Mac));
            sb.Append(" ");
            sb.Append(_error);
            sb.Append(" ");
        
            if (_otherData == null)
            {
                sb.Append(0);
            }
            else
            {
                sb.Append(_otherData.Length);
                sb.Append(" ");
                
                if (_error == RCode.BADTIME)
                {
                    if (_otherData.Length != 6)
                    {
                        sb.Append("<invalid BADTIME other data>");
                    }
                    else
                    {
                        long time = ((long)(_otherData[0] & 0xFF) << 40) +
                                ((long)(_otherData[1] & 0xFF) << 32) +
                                ((_otherData[2] & 0xFF) << 24) +
                                ((_otherData[3] & 0xFF) << 16) +
                                ((_otherData[4] & 0xFF) << 8) +
                                ((_otherData[5] & 0xFF));
                        
                        sb.Append("<server time: ");
                        sb.Append(DnsHelpers.ConvertFromDnsTime(time));
                        sb.Append(">");
                    }
                }
                else
                {
                    sb.Append("<");
                    sb.Append(Convert.ToBase64String(_otherData));
                    sb.Append(">");
                }
            }
            
            return sb.ToString();
        }
    }
}