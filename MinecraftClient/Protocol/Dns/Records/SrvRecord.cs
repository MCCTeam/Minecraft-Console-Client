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

namespace DnDns.Records
{
    public sealed class SrvRecord : DnsRecordBase
    {
        // For SRV
        private ushort _priority;
        private ushort _weight;
        private ushort _port;
        private string _hostName;

        public ushort Priority
        {
            get { return _priority; }
        }
        public ushort Weight
        {
            get { return _weight; }
        }
        
        public ushort Port
        {
            get { return _port; }
        }

        public string HostName
        {
            get { return _hostName; }
        }

        internal SrvRecord(RecordHeader dnsHeader) : base(dnsHeader) { }

        public override void ParseRecord(ref MemoryStream ms)
        {
            byte[] priority = new byte[2];
            ms.Read(priority, 0, 2);
            //_priority = (ushort)Tools.ByteToUInt(Priority);
            _priority = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(priority, 0));

            byte[] weight = new byte[2];
            ms.Read(weight, 0, 2);
            // _weight = (ushort)Tools.ByteToUInt(Weight);
            _weight = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(weight, 0));

            byte[] port = new byte[2];
            ms.Read(port, 0, 2);
            //_port = (ushort)Tools.ByteToUInt(port);
            _port = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(port, 0));

            _hostName = DnsRecordBase.ParseName(ref ms);

            _answer = "Service Location: \r\nPriority: " + _priority + "\r\nWeight: " +
                _weight + "\r\nPort: " + _port + "\r\nHostName: " + _hostName + "\r\n";
        }
    }
}
