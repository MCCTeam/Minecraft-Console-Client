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
    public sealed class AfsdbRecord : DnsRecordBase
    {
        private ushort _port;
        private string _name;
        private short _type;

        public ushort Port
        {
            get { return _port; }
        }

        public string AfsdbName
        {
            get { return _name; }
        }

        public short Type
        {
            get { return _type; }
        }

        internal AfsdbRecord(RecordHeader dnsHeader) : base(dnsHeader) { }

        public override void ParseRecord(ref MemoryStream ms)
        {
            byte[] type = new byte[2];
            ms.Read(type, 0, 2);
            // _port = (ushort)Tools.ByteToUInt(type);
            _port = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(type, 0));
            _name = DnsRecordBase.ParseName(ref ms);
            //_type = (short)Tools.ByteToUInt(type);
            _type = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(type, 0));
            _answer = "Name: " + _name + ", Port: " + _port + ", Type: " + _type;
        }
    }
}
