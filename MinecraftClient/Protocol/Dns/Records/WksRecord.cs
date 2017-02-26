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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DnDns.Records
{
    public sealed class WksRecord : DnsRecordBase
    {
        private ProtocolType _protocolType;
        private IPAddress _ipAddress;
        private short[] _ports;

        public ProtocolType ProtocolType
        {
            get { return _protocolType; }
            set { _protocolType = value; }
        }
        
        public IPAddress IpAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; }
        }

        public short[] Ports
        {
            get { return _ports; }
            set { _ports = value; }
        }

        internal WksRecord(RecordHeader dnsHeader) : base(dnsHeader) { }

        public override void ParseRecord(ref MemoryStream ms)
        {
            // Bit map is the data length minus the IpAddress (4 bytes) and the Protocol (1 byte), 5 bytes total.
            int bitMapLen = this.DnsHeader.DataLength - 4 - 1;
            byte[] ipAddr = new byte[4];
            byte[] BitMap = new byte[bitMapLen];

            ms.Read(ipAddr, 0, 4);
            // _ipAddress = new IPAddress(Tools.ToUInt32(ipAddr, 0));
            _ipAddress = new IPAddress((uint)IPAddress.NetworkToHostOrder(BitConverter.ToUInt32(ipAddr, 0)));
            _protocolType = (ProtocolType)ms.ReadByte();
            ms.Read(BitMap, 0, BitMap.Length);
            _ports = GetKnownServices(BitMap);
            _answer = _protocolType + ": " + Tools.GetServByPort(_ports, _protocolType);
        }

        private short[] GetKnownServices(byte[] BitMap)
        {
            short[] tempPortArr = new short[1024];
            int portCount = 0;
            // mask to isolate left most bit
            const byte mask = 0x80;
            // Iterate through each byte
            for (int i = 0; i < BitMap.Length; i++)
            {
                byte currentByte = BitMap[i];
                int count = 0;
                // iterate through each bit
                for (byte j = 0x07; j != 0xFF; j--)
                {
                    int port = (((i * 8) + count++) + 1);
                    currentByte = (byte)(currentByte << 1);
                    // is the flag set?
                    if ((mask & currentByte) == 0x80)
                    {
                        tempPortArr[portCount] = (short)port;
                        portCount++;
                    }
                }
            }
            short[] portArr = new short[portCount];
            Array.Copy(tempPortArr, 0, portArr, 0, portCount);
            return portArr;
        }
    }
}
