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
    //  DNS HEADER: http://www.faqs.org/rfcs/rfc1035.html
    //                                 1  1  1  1  1  1
    //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                 Query Identifier              |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |QR|   Opcode  |AA|TC|RD|RA| Z|AD|CD|   RCODE   |  <-- The Enums below are combined to create this 16 bit (2 byte) field
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
    /// FlagMasks are used as a bitmask to isolate bits 16 through 31 of the DNS header to convert
    /// them to their appropriate Enum types
    /// </summary>
    internal enum FlagMasks : ushort
    {
        QueryResponseMask = 0x8000,
        OpCodeMask = 0x7800,
        NsFlagMask = 0x07F0,
        RCodeMask = 0x000F
    }

    /// <summary>
    /// |QR| - Starts at bit 16 of DNS Header, size: 1 bit
    /// 
    /// RFC 1035:
    /// A one bit field that specifies whether this message is a
    /// query (0), or a response (1).
    /// 
    /// </summary>
    [Flags()]
    public enum QueryResponse : ushort
    {
        /// <summary>
        /// // QR   Query or Response   [RFC1035]   ( 0 = Query )
        /// </summary>
        Query    = 0x0,
        /// <summary>
        /// // QR   Query or Response   [RFC1035]   ( 1 = Response )
        /// </summary>
        Response = 0x8000    
    }

    /// <summary>
    /// |  OpCode  | - 4 bits of Dns header, Bit 17 - 20, see RFC 1035
    ///     
    /// RFC 1035:
    /// 
    /// A four bit field that specifies kind of query in this
    /// message.  This value is set by the originator of a query
    /// and copied into the response.  
    /// 
    /// The values are:
    /// 
    ///     0               a standard query (QUERY)
    /// 
    ///     1               an inverse query (IQUERY)
    /// 
    ///     2               a server status request (STATUS)
    /// 
    ///     3-15            reserved for future 
    /// </summary>
    [Flags()]
    public enum OpCode : ushort
    {
        /// <summary>
        /// Standard query
        /// [RFC1035] (QUERY)
        /// </summary>
        QUERY = 0x0000,     
        /// <summary>
        /// Inverse query
        /// [RFC1035] (IQUERY)
        /// </summary>
        IQUERY = 0x0800,    
        /// <summary>
        /// Server status request
        /// [RFC1035] (STATUS)
        /// </summary>
        STATUS = 0x1000,    
    }

    /// <summary>
    /// |AA|TC|RD|RA| Z|AD|CD|  - 8 bits (1 byte) flag fields 
    /// 
    /// reference: http://www.networksorcery.com/enp/protocol/dns.htm
    /// </summary>
    [Flags()]
    public enum NsFlags : ushort
    {
        /// <summary>
        /// AA - Authorative Answer 	[RFC1035]   ( 0 = Not authoritative, 1 = Is authoritative )
        /// Authoritative Answer - this bit is valid in responses,
        /// and specifies that the responding name server is an
        /// authority for the domain name in question section.
        /// 
        /// Note that the contents of the answer section may have
        /// multiple owner names because of aliases.  The AA bit
        /// corresponds to the name which matches the query name, or
        /// the first owner name in the answer section.
        /// </summary>
        AA = 0x0400,    
        /// <summary>
        /// TC - Truncated Response 	[RFC1035]   ( 0 = Not truncated, 1 = Message truncated )
        /// 
        /// TrunCation - specifies that this message was truncated
        ///     due to length greater than that permitted on the
        ///     transmission channel.
        /// </summary>
        TC = 0x0200,    
        /// <summary>
        /// RD - Recursion Desired	[RFC1035]   ( 0 = Recursion not desired, 1 = Recursion desired )
        /// 
        /// Recursion Desired - this bit may be set in a query and
        ///     is copied into the response.  If RD is set, it directs
        ///     the name server to pursue the query recursively.
        ///     Recursive query support is optional.
        /// </summary>
        RD = 0x0100,    
        /// <summary>
        /// RA - Recursion Allowed	[RFC1035]   ( 0 = Recursive query support not available, 1 = Recursive query support available )
        /// 
        /// Recursion Available - this be is set or cleared in a
        ///     response, and denotes whether recursive query support is
        ///     available in the name server.
        /// </summary>
        RA = 0x0080,    
        /// <summary>
        /// AD - Authentic Data   	[RFC4035]   ( Authenticated data. 1 bit ) [NOT IMPLEMENTED]
        /// 
        /// Indicates in a response that all data included in the answer and authority 
        /// sections of the response have been authenticated by the server according to 
        /// the policies of that server. It should be set only if all data in the response 
        /// has been cryptographically verified or otherwise meets the server's local security 
        /// policy.
        /// </summary>
        AD = 0x0020,
        /// <summary>
        /// CD - Checking Disabled 	[RFC4035]   ( Checking Disabled. 1 bit ) [NOT IMPLEMENTED]
        /// </summary>
        CD = 0x0010
    }

    /// <summary>
    /// |   RCODE   | - 4 bits error codes
    /// 
    /// Response code - this 4 bit field is set as part of
    ///     responses.  The values have the following interpretation:
    /// 
    /// Fields 6-15            Reserved for future use.
    /// 
    /// reference: http://www.networksorcery.com/enp/protocol/dns.htm
    /// </summary>
    [Flags()]
    public enum RCode : ushort
    {
        /// <summary>
        /// No error condition
        /// </summary>
        NoError = 0,
        /// <summary>
        /// Format error - The name server was unable to 
        /// interpret the query.
        /// </summary>
        FormatError = 1,
        /// <summary>
        /// Server failure - The name server was unable to process 
        /// this query due to a problem with the name server.
        /// </summary>
        ServerFailure = 2,
        /// <summary>
        /// Name Error - Meaningful only for responses from an 
        /// authoritative name server, this code signifies that 
        /// the domain name referenced in the query does not 
        /// exist.
        /// </summary>
        NameError = 3,
        /// <summary>
        /// Not Implemented - The name server does not support 
        /// the requested kind of query.
        /// </summary>
        NotImplemented = 4,
        /// <summary>
        /// Refused - The name server refuses to perform the 
        /// specified operation for policy reasons.  For example,
        /// a name server may not wish to provide the information 
        /// to the particular requester, or a name server may not 
        /// wish to perform a particular operation (e.g., zone  
        /// transfer) for particular data.
        /// </summary>
        Refused = 5,
        /// <summary>
        /// RFC 2136
        /// Name Exists when it should not.
        /// </summary>
        YXDomain = 6,
        /// <summary>
        /// RFC 2136
        /// RR Set Exists when it should not.
        /// </summary>
        YXRRSet = 7,
        /// <summary>
        /// RFC 2136
        /// RR Set that should exist does not.
        /// </summary>
        NXRRSet = 8,
        /// <summary>
        /// RFC 2136
        /// Server Not Authoritative for zone.
        /// </summary>
        NotAuth = 9,
        /// <summary>
        /// RFC 2136
        /// Name not contained in zone.
        /// </summary>
        NotZone = 10,
        /// <summary>
        /// RFC 2671
        /// RFC 2845
        /// 
        /// BADVERS Bad OPT Version.
        /// BADSIG  TSIG Signature Failure.
        /// </summary>
        BADVERS_BADSIG = 16,
        /// <summary>
        /// RFC 2845
        /// Key not recognized.
        /// </summary>
        BADKEY = 17,
        /// <summary>
        /// RFC 2845
        /// Signature out of time window.
        /// </summary>
        BADTIME = 18,
        /// <summary>
        /// RFC 2930
        /// Bad TKEY Mode.
        /// </summary>
        BADMODE = 19,
        /// <summary>
        /// RFC 2930
        /// Duplicate key name.
        /// </summary>
        BADNAME = 20,
        /// <summary>
        /// RFC 2930
        /// Algorithm not supported.
        /// </summary>
        BADALG = 21
    }
}