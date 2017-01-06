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
using System.Diagnostics;

namespace DnDns.Records
{
    /// <summary>
    /// Handles a basic Dns record
    /// 
    /// From RFC 1035:
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
    ///     /                     RDATA                     /
    ///     /                                               /
    ///     +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
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
    /// RDATA           a variable length string of octets that describes the
    ///                 resource.  The format of this information varies
    ///                 according to the TYPE and CLASS of the resource record.
    /// </summary>
    public abstract class DnsRecordBase : IDnsRecord
    {
        #region Fields
        // NAME            an owner name, i.e., the name of the node to which this
        //                 resource record pertains.
        //private string _name;
        // TYPE            two octets containing one of the RR TYPE codes.
        //protected NsType _nsType;
        // CLASS - two octets containing one of the RR CLASS codes.
        //private NsClass _nsClass;
        // TTL - a 32 bit signed integer that specifies the time interval
        //       that the resource record may be cached before the source
        //       of the information should again be consulted.  Zero
        //       values are interpreted to mean that the RR can only be
        //       used for the transaction in progress, and should not be
        //       cached.  For example, SOA records are always distributed
        //       with a zero TTL to prohibit caching.  Zero values can
        ///      also be used for extremely volatile data.
        //private int _timeToLive;
        // RDLENGTH - an unsigned 16 bit integer that specifies the length in
        //            octets of the RDATA field.
        //protected short _dataLength;
        protected RecordHeader _dnsHeader;
        protected string _answer;
        protected string _errorMsg;
        #endregion

        #region Properties
        /// <summary>
        /// NAME - an owner name, i.e., the name of the node to which this
        ///        resource record pertains.
        /// </summary>
        //public string Name
        //{
        //    get { return _name; }
        //}

        public RecordHeader DnsHeader
        {
            get { return _dnsHeader; }
            protected set { _dnsHeader = value; }
        }

        public string Answer
        {
            get { return _answer; }
        }

        
        /// <summary>
        /// TYPE    two octets containing one of the RR TYPE codes.
        /// </summary>
        //public NsType NsType
        //{
        //    get { return _nsType; }
        //}

        /// <summary>
        /// CLASS - two octets containing one of the RR CLASS codes.
        /// </summary>
        //public NsClass NsClass
        //{
        //    get { return _nsClass; }
        //}
        
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
        //public int TimeToLive
        //{
        //    get { return _timeToLive; }
        //}

        /// <summary>
        /// RDLENGTH - an unsigned 16 bit integer that specifies the length in
        ///            octets of the RDATA field.
        /// </summary>
        //public short DataLength
        //{
        //    get { return _dataLength; }
        //}

        public string ErrorMsg
        {
            get { return _errorMsg; }
        }
        #endregion

        internal DnsRecordBase()
        {
        }

        public virtual void ParseRecord(ref MemoryStream ms)
        {
            // Default implementation - the most common.
            _answer = DnsRecordBase.ParseName(ref ms);
        }

        internal DnsRecordBase(RecordHeader dnsHeader)
        {
            _dnsHeader = dnsHeader;
        }
       
        // RFC 
        //        4.1.4. Message compression
        //
        // In order to reduce the size of messages, the domain system utilizes a
        // compression scheme which eliminates the repetition of domain names in a
        // message.  In this scheme, an entire domain name or a list of labels at
        // the end of a domain name is replaced with a pointer to a prior occurance
        // of the same name.
        //
        // The pointer takes the form of a two octet sequence:
        //
        //    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //    | 1  1|                OFFSET                   |
        //    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        //
        // The first two bits are ones.  This allows a pointer to be distinguished
        // from a label, since the label must begin with two zero bits because
        // labels are restricted to 63 octets or less.  (The 10 and 01 combinations
        // are reserved for future use.)  The OFFSET field specifies an offset from
        // the start of the message (i.e., the first octet of the ID field in the
        // domain header).  A zero offset specifies the first byte of the ID field,
        // etc.
        //
        // The compression scheme allows a domain name in a message to be
        // represented as either:
        //
        //   - a sequence of labels ending in a zero octet
        //   - a pointer
        //   - a sequence of labels ending with a pointer
        //

        internal static string ParseName(ref MemoryStream ms)
        {
            Trace.WriteLine("Reading Name...");
            StringBuilder sb = new StringBuilder();

            uint next = (uint)ms.ReadByte();
            Trace.WriteLine("Next is 0x" + next.ToString("x2"));
            int bPointer;

            while ((next != 0x00))
            {
                // Isolate 2 most significat bits -> e.g. 11xx xxxx
                // if it's 0xc0 (11000000b} then pointer
                switch (0xc0 & next)
                {
                    // 0xc0 -> Name is a pointer.
                    case 0xc0:
                    {
                        // Isolate Offset
                        int offsetMASK = ~0xc0;
                        
                        // Example on how to calculate the offset
                        // e.g. 
                        // 
                        // So if given the following 2 bytes - 0xc1 0x1c (11000001 00011100)
                        //
                        //  The pointer takes the form of a two octet sequence:
                        //    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                        //    | 1  1|                OFFSET                   |
                        //    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                        //    | 1  1| 0  0  0  0  0  1  0  0  0  1  1  1  0  0|
                        //    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                        //
                        // A pointer is indicated by the a 1 in the two most significant bits
                        // The Offset is the remaining bits.
                        //
                        // The Pointer = 0xc0 (11000000 00000000)
                        // The offset = 0x11c (00000001 00011100)

                        // Move offset into the proper position
                        int offset = (int)(offsetMASK & next) << 8;
                        
                        // extract the pointer to the data in the stream
                        bPointer = ms.ReadByte() + offset;
                        // store the position so we can resume later
                        long oldPtr = ms.Position;
                        // Move to the specified position in the stream and 
                        // parse the name (recursive call)
                        ms.Position = bPointer;
                        sb.Append(DnsRecordBase.ParseName(ref ms));
                        Trace.WriteLine(sb.ToString());
                        // Move back to original position, and continue
                        ms.Position = oldPtr;
                        next = 0x00;
                        break;
                    }
                    case 0x00:
                    {
                        Debug.Assert(next < 0xc0, "Offset cannot be greater then 0xc0.");
                        byte[] buffer = new byte[next];
                        ms.Read(buffer, 0, (int)next);
                        sb.Append(Encoding.ASCII.GetString(buffer) + ".");
                        next = (uint)ms.ReadByte();
                        Trace.WriteLine("0x" + next.ToString("x2"));
                        break;
                    }
                    default:
                        throw new InvalidOperationException("There was a problem decompressing the DNS Message.");
                }
            }
            return sb.ToString();
        }

        internal string ParseText(ref MemoryStream ms)
        {
            StringBuilder sb = new StringBuilder();

            int len = ms.ReadByte();
            byte[] buffer = new byte[len];
            ms.Read(buffer, 0, len);
            sb.Append(Encoding.ASCII.GetString(buffer));
            return sb.ToString();
        }

        public override string ToString()
        {
            return _answer;
        }

        #region IDnsRecord Members

        public virtual byte[] GetMessageBytes()
        {
            return new byte[]{};
        }

        #endregion
    }
}
