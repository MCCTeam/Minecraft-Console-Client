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

using DnDns.Enums;

namespace DnDns.Records
{
    /// <summary>
    /// The DnsRecordHeader class contains fields, properties and 
    /// parsing cababilities within the DNS Record except the the 
    /// RDATA.  The Name, Type, Class, TTL, and RDLength.  
    ///
    /// This class is used in the DnsRecordFactory to instantiate 
    /// concrete DnsRecord Classes.
    /// 
    /// RFC 1035
    /// 
    /// 3.2.1. Format
    /// 
    /// All RRs have the same top level format shown below:
    /// 
    ///                                     1  1  1  1  1  1
    ///       0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    ///     |                                               |
    ///     /                                               /
    ///     /                      NAME                     /
    ///     |                                               |
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    ///     |                      TYPE                     |
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    ///     |                     CLASS                     |
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    ///     |                      TTL                      |
    ///     |                                               |
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    ///     |                   RDLENGTH                    |
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
    ///  
    /// where:
    /// 
    /// NAME            an owner name, i.e., the name of the node to which this
    ///                 resource record pertains.
    /// 
    /// TYPE            two octets containing one of the RR TYPE codes.
    /// 
    /// CLASS           two octets containing one of the RR CLASS codes.
    /// 
    /// TTL             a 32 bit signed integer that specifies the time interval
    ///                 that the resource record may be cached before the source
    ///                 of the information should again be consulted.  Zero
    ///                 values are interpreted to mean that the RR can only be
    ///                 used for the transaction in progress, and should not be
    ///                 cached.  For example, SOA records are always distributed
    ///                 with a zero TTL to prohibit caching.  Zero values can
    ///                 also be used for extremely volatile data.
    /// 
    /// RDLENGTH        an unsigned 16 bit integer that specifies the length in
    ///                 octets of the RDATA field.
    /// 
    /// </summary>
    public class RecordHeader
    {
        #region Fields
        // NAME            an owner name, i.e., the name of the node to which this
        //                 resource record pertains.
        private string _name;
        // TYPE            two octets containing one of the RR TYPE codes.
        private NsType _nsType;
        // CLASS - two octets containing one of the RR CLASS codes.
        private NsClass _nsClass;
        // TTL - a 32 bit signed integer that specifies the time interval
        //       that the resource record may be cached before the source
        //       of the information should again be consulted.  Zero
        //       values are interpreted to mean that the RR can only be
        //       used for the transaction in progress, and should not be
        //       cached.  For example, SOA records are always distributed
        //       with a zero TTL to prohibit caching.  Zero values can
        ///      also be used for extremely volatile data.
        private int _timeToLive;
        // RDLENGTH - an unsigned 16 bit integer that specifies the length in
        //            octets of the RDATA field.
        private short _dataLength;

        /// <summary>
        /// Initalise the <see cref="RecordHeader"/>
        /// </summary>
        /// <param name="name">The header name</param>
        /// <param name="nsType">The resource type</param>
        /// <param name="nsClass">The class type</param>
        /// <param name="timeToLive">The time to live</param>
        public RecordHeader(string name, NsType nsType, NsClass nsClass, int timeToLive)
        {
            _name = name;
            _nsType = nsType;
            _nsClass = nsClass;
            _timeToLive = timeToLive;
        }

        public RecordHeader()
        {
        }

        #endregion

        #region Properties
        /// <summary>
        /// NAME - an owner name, i.e., the name of the node to which this
        ///        resource record pertains.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// TYPE    two octets containing one of the RR TYPE codes.
        /// </summary>
        public NsType NsType
        {
            get { return _nsType; }
        }

        /// <summary>
        /// CLASS - two octets containing one of the RR CLASS codes.
        /// </summary>
        public NsClass NsClass
        {
            get { return _nsClass; }
        }

        /// <summary>
        /// TTL - a 32 bit signed integer that specifies the time interval
        ///       that the resource record may be cached before the source
        ///       of the information should again be consulted.  Zero
        ///       values are interpreted to mean that the RR can only be
        ///       used for the transaction in progress, and should not be
        ///       cached.  For example, SOA records are always distributed
        ///       with a zero TTL to prohibit caching.  Zero values can
        ///       also be used for extremely volatile data.
        /// </summary>
        public int TimeToLive
        {
            get { return _timeToLive; }
        }

        /// <summary>
        /// RDLENGTH - an unsigned 16 bit integer that specifies the length in
        ///            octets of the RDATA field.
        /// </summary>
        public short DataLength
        {
            get { return _dataLength; }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ms"></param>
        public void ParseRecordHeader(ref MemoryStream ms)
        {
            byte[] nsType = new byte[2];
            byte[] nsClass = new byte[2];
            byte[] nsTTL = new byte[4];
            byte[] nsDataLength = new byte[2];

            // Read the name
            _name = DnsRecordBase.ParseName(ref ms);

            // Read the data header
            ms.Read(nsType, 0, 2);
            ms.Read(nsClass, 0, 2);
            ms.Read(nsTTL, 0, 4);
            ms.Read(nsDataLength, 0, 2);
            _nsType = (NsType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(nsType, 0));
            _nsClass = (NsClass)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(nsClass, 0));

            _timeToLive = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nsTTL, 0));
            _dataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(nsDataLength, 0));
        }

        internal byte[] GetMessageBytes()
        {
            MemoryStream memoryStream = new MemoryStream();

            byte[] data = DnsHelpers.CanonicaliseDnsName(_name, false);
            memoryStream.Write(data,0,data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_nsType) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_nsClass) >> 16));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((uint)(IPAddress.HostToNetworkOrder((ushort)_timeToLive) >> 32));
            memoryStream.Write(data, 0, data.Length);

            data = BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((ushort)_dataLength) >> 16));
            memoryStream.Write(data, 0, data.Length);

            return memoryStream.ToArray();
        }
    }
}
