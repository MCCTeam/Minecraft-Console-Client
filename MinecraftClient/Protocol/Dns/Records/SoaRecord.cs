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
    public sealed class SoaRecord : DnsRecordBase
    {
        // For SOA
        #region fields
        private string _primaryNameServer;
        private string _responsiblePerson;
        private uint _serial;
        private uint _refreshInterval;
        private uint _retryInterval;
        private uint _expirationLimit;
        // RFC 1034: TTL - only positive values of a signed 32 bit number.
        private int _minimumTimeToLive;
        #endregion

        #region properties
        public string PrimaryNameServer
        {
            get { return _primaryNameServer; }
        }

        public string ResponsiblePerson
        {
            get { return _responsiblePerson; }
        }

        public uint Serial
        {
            get { return _serial; }
        }

        public uint RefreshInterval
        {
            get { return _refreshInterval; }
        }

        public uint RetryInterval
        {
            get { return _retryInterval; }
        }

        public uint ExpirationLimit
        {
            get { return _expirationLimit; }
        }

        public int MinimumTimeToLive
        {
            get { return _minimumTimeToLive; }
        }
        #endregion

        internal SoaRecord(RecordHeader dnsHeader) : base(dnsHeader) { }

        public override void ParseRecord(ref MemoryStream ms)
        {
            StringBuilder sb = new StringBuilder();
            // Parse Name
            _primaryNameServer = DnsRecordBase.ParseName(ref ms);
            sb.Append("Primary NameServer: ");
            sb.Append(_primaryNameServer);
            sb.Append("\r\n");

            // Parse Responsible Persons Mailbox (Parse Name)
            _responsiblePerson = DnsRecordBase.ParseName(ref ms);
            sb.Append("Responsible Person: ");
            sb.Append(_responsiblePerson);
            sb.Append("\r\n");

            byte[] serial = new Byte[4];
            byte[] refreshInterval = new Byte[4];
            byte[] retryInterval = new Byte[4];
            byte[] expirationLimit = new Byte[4];
            byte[] minTTL = new Byte[4];

            // Parse Serial (4 bytes)
            ms.Read(serial, 0, 4);
            //_serial = Tools.ByteToUInt(serial);
            _serial = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(serial, 0));
            sb.Append("Serial: ");
            sb.Append(_serial);
            sb.Append("\r\n");

            // Parse Refresh Interval (4 bytes)
            ms.Read(refreshInterval, 0, 4);
            // _refreshInterval = Tools.ByteToUInt(refreshInterval);
            _refreshInterval = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(refreshInterval, 0));
            sb.Append("Refresh Interval: ");
            sb.Append(_refreshInterval);
            sb.Append("\r\n");

            // Parse Retry Interval (4 bytes)
            ms.Read(retryInterval, 0, 4);
            //_retryInterval = Tools.ByteToUInt(retryInterval);
            _retryInterval = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(retryInterval, 0));
            sb.Append("Retry Interval: ");
            sb.Append(_retryInterval);
            sb.Append("\r\n");

            // Parse Expiration limit (4 bytes)
            ms.Read(expirationLimit, 0, 4);
            // _expirationLimit = Tools.ByteToUInt(expirationLimit);
            _expirationLimit = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(expirationLimit, 0));
            sb.Append("Expire: ");
            sb.Append(_expirationLimit);
            sb.Append("\r\n");

            // Parse Min TTL (4 bytes)
            ms.Read(minTTL, 0, 4);
            // _minTTL = Tools.ByteToUInt(minTTL);
            _minimumTimeToLive = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(minTTL, 0));
            sb.Append("TTL: ");
            sb.Append(_minimumTimeToLive);
            sb.Append("\r\n");

            _answer = sb.ToString();
        }
    }
}
