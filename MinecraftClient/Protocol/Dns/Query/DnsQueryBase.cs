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

using DnDns.Enums;
using DnDns.Records;

namespace DnDns.Query
{
    /// <summary>
    /// DnsQueryBase maintains the common state of DNS Queries (both responses and requests)
    /// </summary>
    public abstract class DnsQueryBase
    {
        #region Fields
        // RFC 1034
        //
        // 4.1.1. Header section format
        // 
        //                                 1  1  1  1  1  1
        //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                      ID                       |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                    QDCOUNT                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                    ANCOUNT                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                    NSCOUNT                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                    ARCOUNT                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

        /// <summary>
        /// ID - A 16 bit identifier. This identifier is copied
        /// the corresponding reply and can be used by the requester
        /// to match up replies to outstanding queries.
        /// </summary>
        protected ushort _transactionId;
        /// <summary>
        /// _flags will store a combination of the enums that make up the 16 bits after the 
        ///  TransactionID in the DNS protocol header
        /// </summary>
        protected ushort _flags;
        /// <summary>
        /// A one bit field that specifies whether this message is a
        /// query (0), or a response (1).
        /// </summary>
        protected QueryResponse _queryResponse;
        /// <summary>
        /// OPCODE - A four bit field that specifies kind of query in this
        /// message.  This value is set by the originator of a query
        /// and copied into the response.  
        /// </summary>
        protected OpCode _opCode;
        /// <summary>
        ///  - A combination of flag fields in the DNS header (|AA|TC|RD|RA|)
        /// </summary>
        protected NsFlags _nsFlags;
        /// <summary>
        /// Response code - this 4 bit field is set as part of
        /// responses only. 
        /// </summary>
        protected RCode _rCode;
        /// <summary>
        /// QDCOUNT - an unsigned 16 bit integer specifying the number of
        /// entries in the question section.
        /// </summary>
        protected ushort _questions;
        /// <summary>
        /// ANCOUNT - an unsigned 16 bit integer specifying the number of
        /// resource records in the answer section.
        /// </summary>
        protected ushort _answerRRs;
        /// <summary>
        /// NSCOUNT - an unsigned 16 bit integer specifying the number of name
        /// server resource records in the authority records
        /// section.
        /// </summary>
        protected ushort _authorityRRs;

        // RFC 1034
        // 4.1.2. Question section format
        //                                 1  1  1  1  1  1
        //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                                               |
        // /                     QNAME                     /
        // /                                               /
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                     QTYPE                     |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                     QCLASS                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

        /// <summary>
        /// QNAME - a domain name represented as a sequence of labels, where
        /// each label consists of a length octet followed by that
        /// number of octets.  The domain name terminates with the
        /// zero length octet for the null label of the root.  Note
        /// that this field may be an odd number of octets; no
        /// padding is used
        /// </summary>
        protected string _name;
        /// <summary>
        /// QTYPE - a two octet code which specifies the type of the query.
        /// The values for this field include all codes valid for a
        /// TYPE field, together with some more general codes which
        /// can match more than one type of RR.
        /// </summary>
        protected NsType _nsType;
        /// <summary>
        /// QCLASS - a two octet code that specifies the class of the query.
        /// For example, the QCLASS field is IN for the Internet.
        /// </summary>
        protected NsClass _nsClass;

        /// <summary>
        /// The additional records for the DNS Query
        /// </summary>
        protected List<IDnsRecord> _additionalRecords = new List<IDnsRecord>();
        
        #endregion Fields

        #region Properties
        
        /// ID - A 16 bit identifier. This identifier is copied
        /// the corresponding reply and can be used by the requester
        /// to match up replies to outstanding queries.
        /// </summary>
        public ushort TransactionID
        {
            get { return _transactionId; }
        }

        /// <summary>
        /// A one bit field that specifies whether this message is a
        /// query (0), or a response (1).
        /// </summary>
        public QueryResponse QueryResponse
        {
            get { return _queryResponse; }
        }

        /// <summary>
        /// OPCODE - A four bit field that specifies kind of query in this
        /// message.  This value is set by the originator of a query
        /// and copied into the response.  
        /// </summary>
        public OpCode OpCode
        {
            get { return _opCode; }
        }

        /// <summary>
        /// NsFlags - A combination of flag fields in the DNS header (|AA|TC|RD|RA|)
        /// </summary>
        public NsFlags NsFlags
        {
            get { return _nsFlags; }
        }

        /// <summary>
        /// Response code - this 4 bit field is set as part of
        /// responses only. 
        /// </summary>
        public RCode RCode
        {
            get { return _rCode; }
        }

        /// <summary>
        /// QDCOUNT - an unsigned 16 bit integer specifying the number of
        /// entries in the question section.
        /// </summary>
        public ushort Questions
        {
            get { return _questions; }
        }

        /// <summary>
        /// ANCOUNT - an unsigned 16 bit integer specifying the number of
        /// resource records in the answer section.
        /// </summary>
        public ushort AnswerRRs
        {
            get { return _answerRRs; }
        }

        /// <summary>
        /// NSCOUNT - an unsigned 16 bit integer specifying the number of name
        /// server resource records in the authority records
        /// section.
        /// </summary>
        public ushort AuthorityRRs
        {
            get { return _authorityRRs; }
        }

        /// <summary>
        /// ARCOUNT - an unsigned 16 bit integer specifying the number of
        /// resource records in the additional records section.
        /// </summary>
        public ushort AdditionalRRs
        {
            get { return (ushort) _additionalRecords.Count; }
        }

        /// <summary>
        /// QNAME - a domain name represented as a sequence of labels, where
        /// each label consists of a length octet followed by that
        /// number of octets.  The domain name terminates with the
        /// zero length octet for the null label of the root.  Note
        /// that this field may be an odd number of octets; no
        /// padding is used
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// QTYPE - a two octet code which specifies the type of the query.
        /// The values for this field include all codes valid for a
        /// TYPE field, together with some more general codes which
        /// can match more than one type of RR.
        /// </summary>
        public NsType NsType
        {
            get { return _nsType; }
            set { _nsType = value; }
        }

        /// <summary>
        /// QCLASS - a two octet code that specifies the class of the query.
        /// For example, the QCLASS field is IN for the Internet.
        /// </summary>
        public NsClass NsClass
        {
            get { return _nsClass; }
            set { _nsClass = value; }
        }

        public List<IDnsRecord> AdditionalRRecords
        {
            get { return _additionalRecords; }
        }
        #endregion
    }
}
