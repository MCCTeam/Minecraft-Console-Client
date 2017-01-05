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

using DnDns.Enums;

namespace DnDns.Records
{
    class RecordFactory
    {
        public static IDnsRecord Create(ref MemoryStream ms) 
        {
            IDnsRecord dnsRecord;
            // Have to start out with an unknown record, since we have to parse the entire
            // header before we can determine the type of DNS record it is.
            // TODO: Consider other options.
            
            // start as an unknown type, then create a known type, parse the response 
            // and return the object.	
            //DnsRecordBase dr = new DnsUnknownRecord();
            //dr.ParseRecordHeader(ref ms);

            RecordHeader dnsHeader = new RecordHeader();
            dnsHeader.ParseRecordHeader(ref ms);

            switch (dnsHeader.NsType)
            {
                case NsType.A:
                    {
                        dnsRecord = new ARecord(dnsHeader);
                        break;
                    }
                case NsType.AAAA:
                    {
                        dnsRecord = new AaaaRecord(dnsHeader);
                        break;
                    }
                case NsType.MX:
                    {
                        dnsRecord = new MxRecord(dnsHeader);
                        break;
                    }
                case NsType.RP:
                    {
                        dnsRecord = new RpRecord(dnsHeader);
                        break;
                    }
                case NsType.MR:
                    {
                        dnsRecord = new MrRecord(dnsHeader);
                        break;
                    }
                case NsType.MB:
                    {
                        dnsRecord = new MbRecord(dnsHeader);
                        break;
                    }
                case NsType.MG:
                    {
                        dnsRecord = new MgRecord(dnsHeader);
                        break;
                    }
                case NsType.NS:
                    {
                        dnsRecord = new NsRecord(dnsHeader);
                        break;
                    }
                case NsType.CNAME:
                    {
                        dnsRecord = new CNameRecord(dnsHeader);
                        break;
                    }
                case NsType.PTR:
                    {
                        dnsRecord = new PtrRecord(dnsHeader);
                        break;
                    }
                case NsType.HINFO:
                    {
                        dnsRecord = new HInfoRecord(dnsHeader);
                        break;
                    }
                case NsType.MINFO:
                    {
                        dnsRecord = new MInfoRecord(dnsHeader);
                        break;
                    }
                case NsType.X25:
                    {
                        dnsRecord = new X25Record(dnsHeader);
                        break;
                    }
                case NsType.TXT:
                    {
                        dnsRecord = new TxtRecord(dnsHeader);
                        break;
                    }
                case NsType.LOC:
                    {
                        dnsRecord = new LocRecord(dnsHeader);
                        break;
                    }
                case NsType.SOA:
                    {
                        dnsRecord = new SoaRecord(dnsHeader);
                        break;
                    }
                case NsType.SRV:
                    {
                        dnsRecord = new SrvRecord(dnsHeader);
                        break;
                    }
                case NsType.AFSDB:
                    {
                        dnsRecord = new AfsdbRecord(dnsHeader);
                        break;
                    }
                case NsType.ATMA:
                    {
                        dnsRecord = new AtmaRecord(dnsHeader);
                        break;
                    }
                case NsType.ISDN:
                    {
                        dnsRecord = new IsdnRecord(dnsHeader);
                        break;
                    }
                case NsType.RT:
                    {
                        dnsRecord = new RtRecord(dnsHeader);
                        break;
                    }
                case NsType.WKS:
                    {
                        dnsRecord = new WksRecord(dnsHeader);
                        break;
                    }
                case NsType.TSIG:
                    {
                        dnsRecord = new TSigRecord(dnsHeader);
                        break;
                    }
                default:
                    {
                        // Unknown type. parse and return the DnsUnknownRecord
                        dnsRecord = new UnknownRecord(dnsHeader);
                        break;
                    }
            }

            //dnsRecord.ParseRecordHeader(ref ms);
            dnsRecord.ParseRecord(ref ms);
            return dnsRecord;
        }
    }
}
