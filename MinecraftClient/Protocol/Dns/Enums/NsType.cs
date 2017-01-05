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

namespace DnDns.Enums
{
    /// <summary>
    /// Currently defined type values for resources and queries.
    /// 
    /// RFC 1034
    /// 
    /// 3.2.2. TYPE values
    /// 
    /// TYPE fields are used in resource records.  Note that these types are a
    /// subset of QTYPEs.
    /// 
    /// TYPE            value and meaning
    /// 
    /// A               1   a host address, Implemented
    /// 
    /// NS              2   an authoritative name server, Implemented
    /// 
    /// MD              3   a mail destination (Obsolete - use MX), NOT Implemented
    /// 
    /// MF              4   a mail forwarder (Obsolete - use MX), NOT Implemented
    /// 
    /// CNAME           5   the canonical name for an alias, Implemented
    /// 
    /// SOA             6   marks the start of a zone of authority, Implemented
    /// 
    /// MB              7   a mailbox domain name (EXPERIMENTAL), Implemented
    /// 
    /// MG              8   a mail group member (EXPERIMENTAL), Implemented
    /// 
    /// MR              9   a mail rename domain name (EXPERIMENTAL), Implemented
    /// 
    /// NULL            10  a null RR (EXPERIMENTAL), NOT IMPLEMENTED
    /// 
    /// WKS             11  a well known service description
    /// 
    /// PTR             12  a domain name pointer
    /// 
    /// HINFO           13  host information
    /// 
    /// MINFO           14  mailbox or mail list information
    /// 
    /// MX              15  mail exchange
    /// 
    /// TXT             16  text strings
    /// 
    /// 3.2.3. QTYPE values
    /// 
    /// QTYPE fields appear in the question part of a query.  QTYPES are a
    /// superset of TYPEs, hence all TYPEs are valid QTYPEs.  In addition, the
    /// following QTYPEs are defined:
    /// 
    /// AXFR            252 A request for a transfer of an entire zone
    /// 
    /// MAILB           253 A request for mailbox-related records (MB, MG or MR)
    /// 
    /// MAILA           254 A request for mail agent RRs (Obsolete - see MX)
    /// 
    /// *               255 A request for all records
    /// 
    /// </summary>
    public enum NsType : uint
    {
        /// <summary>
        /// Invalid
        /// </summary>
        INVALID = 0,
        /// <summary>
        /// Host address
        /// </summary>
        A = 1,
		/// <summary>
        /// Authoritative server 
		/// </summary>
        NS = 2,
        /// <summary>
        /// Mail destination - NOT IMPLEMENTED
        /// </summary>
        MD = 3,
        /// <summary>
        /// Mail forwarder, NOT IMPLEMENTED
        /// </summary>
        MF = 4,
        /// <summary>
        ///  Canonical name
        /// </summary>
        CNAME = 5,
        /// <summary>
        /// Start of authority zone
        /// </summary>
        SOA = 6,
        // Mailbox domain name
        MB = 7,
        /// <summary>
        /// Mail group member
        /// </summary>
        MG = 8,
        /// <summary>
        /// Mail rename name
        /// </summary>
        MR = 9,
        /// <summary>
        /// Null resource record
        /// </summary>
        NULL = 10,
        /// <summary>
        /// Well known service
        /// </summary>
        WKS = 11,
        /// <summary>
        /// Domain name pointer
        /// </summary>
        PTR = 12,
        /// <summary>
        /// Host information
        /// </summary>
        HINFO = 13,
        /// <summary>
        /// Mailbox information
        /// </summary>
        MINFO = 14,
        /// <summary>
        /// Mail routing information
        /// </summary>
        MX = 15,
        /// <summary>
        /// Text strings, RFC 1464 
        /// </summary>
        TXT = 16,								
        /// <summary>
        /// Responsible person, RFC 1183, Implemented
        /// </summary>
        RP = 17,
        /// <summary>
        /// AFS cell database, RFC 1183, Implemented
        /// </summary>
        AFSDB = 18,
        /// <summary>
        /// X_25 calling address, RFC 1183, Implemented
        /// </summary>
        X25 = 19,						
        /// <summary>
        /// ISDN calling address, RFC 1183, Implemented
        /// </summary>
        ISDN = 20,
        /// <summary>
        /// Router, RFC 1183, Implemented
        /// </summary>
        RT = 21,
        /// <summary>
        /// NSAP address, RFC 1706
        /// </summary>
        NSAP = 22,		
        /// <summary>
        /// Reverse NSAP lookup - deprecated by PTR	?
        /// </summary>
        NSAP_PTR = 23,
        /// <summary>
        /// Security signature, RFC 2535
        /// </summary>
        SIG = 24,
		/// <summary>
        /// Security key, RFC 2535
		/// </summary>
        KEY = 25,
        /// <summary>
        /// X.400 mail mapping, RFC ?
        /// </summary>
        PX = 26,		
        /// <summary>
        /// Geographical position - withdrawn, RFC 1712
        /// </summary>
        GPOS = 27,		
        /// <summary>
        /// Ip6 Address, RFC 1886 -- Implemented
        /// </summary>
        AAAA = 28,
        /// <summary>
        /// Location Information, RFC 1876, Implemented
        /// </summary>
        LOC = 29,
        /// <summary>
        /// Next domain (security), RFC 2065
        /// </summary>
        NXT = 30,
        /// <summary>
        /// Endpoint identifier,RFC ?
        /// </summary>
        EID = 31,
        /// <summary>
        /// Nimrod Locator, RFC ?
        /// </summary>
        NIMLOC = 32,	
        /// <summary>
        /// Server Record, RFC 2052, Implemented
        /// </summary>
        SRV = 33,
        /// <summary>
        /// ATM Address, RFC ?, Implemented
        /// </summary>
        ATMA = 34,
        /// <summary>
        /// Naming Authority PoinTeR, RFC 2915
        /// </summary>
        MAPTR = 35,
        /// <summary>
        /// Key Exchange, RFC 2230
        /// </summary>
        KX = 36,
        /// <summary>
        /// Certification record, RFC 2538
        /// </summary>
        CERT = 37,
        /// <summary>
        /// IPv6 address (deprecates AAAA), RFC 3226
        /// </summary>
        A6 = 38,
        /// <summary>
        /// Non-terminal DNAME (for IPv6), RFC 2874
        /// </summary>
        DNAME = 39,
        /// <summary>
        /// Kitchen sink (experimentatl), RFC ?
        /// </summary>
        SINK = 40,
        /// <summary>
        /// EDNS0 option (meta-RR), RFC 2671
        /// </summary>
        OPT = 41,
        /// <summary>
        /// Transaction key, RFC 2930
        /// </summary>
        TKEY = 249,
        /// <summary>
        /// Transaction signature, RFC 2845
        /// </summary>
        TSIG = 250,
        /// <summary>
        /// Incremental zone transfer, RFC 1995
        /// </summary>
        IXFR = 251,
        /// <summary>
        /// Transfer zone of authority, RFC 1035
        /// </summary>
        AXFR = 252,
        /// <summary>
        /// Transfer mailbox records, RFC 1035
        /// </summary>
        MAILB = 253,
        /// <summary>
        /// Transfer mail agent records, RFC 1035
        /// </summary>
        MAILA = 254,	
        /// <summary>
        /// All of the above, RFC 1035
        /// </summary>
        ANY = 255,
        /// <summary>
        /// DNSSEC Trust Authorities
        /// </summary>
        DNSSECTrustAuthorities = 32768,
        /// <summary>
        /// DNSSEC Lookaside Validation, RFC4431
        /// </summary>
        DNSSECLookasideValidation = 32769
    }
}