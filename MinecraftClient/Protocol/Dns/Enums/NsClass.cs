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
    /// RFC 1035:
    /// 
    /// 3.2.4. CLASS values
    /// 
    /// CLASS fields appear in resource records.  The following CLASS mnemonics
    /// and values are defined:
    /// 
    /// IN              1 the Internet
    /// 
    /// CS              2 the CSNET class (Obsolete - used only for examples in
    ///                 some obsolete RFCs)
    /// 
    /// CH              3 the CHAOS class
    /// 
    /// HS              4 Hesiod [Dyer 87]
    /// 
    /// 3.2.5. QCLASS values
    /// 
    /// QCLASS fields appear in the question section of a query.  QCLASS values
    /// are a superset of CLASS values; every CLASS is a valid QCLASS.  In
    /// addition to CLASS values, the following QCLASSes are defined:
    /// 
    /// *               255 any class
    /// </summary>
    public enum NsClass : byte
    {
        /// <summary>
        /// Cookie??                - NOT IMPLEMENTED
        /// </summary>
        INVALID = 0,
        /// <summary>
        /// // Internet (inet), RFC 1035
        /// </summary>
        INET = 1,
		/// <summary>
        /// MIT Chaos-net, RFC 1035 - NOT IMPLEMENTED
		/// </summary>
        CHAOS = 3,
        /// <summary>
        /// MIT Hesiod, RFC 1035    - NOT IMPLEMENTED
        /// </summary>
        HS = 4,
        /// <summary>
        /// RFC 2136 - None
        /// prereq sections in update requests -- NOT IMPLEMENTED
        /// </summary>
        NONE = 254,
        /// <summary>
        /// Any QCLASS only, Wildcard match, RFC 1035   - IMPLEMENTED for INET only
        /// </summary>
        ANY = 255
    }
}
