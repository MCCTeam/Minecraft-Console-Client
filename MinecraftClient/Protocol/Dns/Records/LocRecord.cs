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
	public sealed class LocRecord : DnsRecordBase 
	{
        // For LOC
        #region Fields
        private byte _version;
        private byte _size;
        private byte _horPrecision;
        private byte _vertPrecision;
        private uint _latitude;
        private uint _longitude;
        private uint _altitude;
        #endregion

        #region Properties
        public byte Version
        {
            get { return _version; }
        }

        public byte Size
        {
            get { return _size; }
        }

        public byte HorPrecision
        {
            get { return _horPrecision; }
        }
        
        public byte VertPrecision
        {
            get { return _vertPrecision; }
        }
        
        public uint Latitude
        {
            get { return _latitude; }
        }
        
        public uint Longitude
        {
            get { return _longitude; }
        }
        
        public uint Altitude
        {
            get { return _altitude; }
        }
        #endregion 

        private char[] _latDirection = new char[2] {'N', 'S'};
		private char[] _longDirection = new char[2] {'E', 'W'};

		internal LocRecord(RecordHeader dnsHeader) : base(dnsHeader) {}

		public override void ParseRecord(ref MemoryStream ms) 
		{
			byte[] latitude = new Byte[4];
			byte[] longitude = new Byte[4];
			byte[] altitude = new Byte[4];

			_version = (byte)ms.ReadByte();
			_size = (byte)ms.ReadByte();
			_horPrecision = (byte)ms.ReadByte();
			_vertPrecision = (byte)ms.ReadByte();
					
			ms.Read(latitude,0,latitude.Length);
			// _latitude = Tools.ByteToUInt(latitude);
            _latitude = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(latitude, 0));
					
			ms.Read(longitude,0,longitude.Length);
			// _longitude = Tools.ByteToUInt(longitude);
            _longitude = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(longitude, 0));

					
			ms.Read(altitude,0,altitude.Length);
			// _altitude = Tools.ByteToUInt(altitude);
            _altitude = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(altitude, 0));
			
			StringBuilder sb = new StringBuilder();
            sb.Append("Version: ");
            sb.Append(_version);
            sb.Append("\r\n");

			sb.Append("Size: ");
            sb.Append(CalcSize(_size));
            sb.Append(" m\r\n");

			sb.Append("Horizontal Precision: ");
            sb.Append(CalcSize(_horPrecision));
            sb.Append(" m\r\n");

			sb.Append("Vertical Precision: ");
            sb.Append(CalcSize(_vertPrecision));
            sb.Append(" m\r\n");
			
            sb.Append("Latitude: ");
            sb.Append(CalcLoc(_latitude, _latDirection));
            sb.Append("\r\n");
			
            sb.Append("Longitude: ");
            sb.Append(CalcLoc(_longitude, _longDirection));
            sb.Append("\r\n");
			
            sb.Append("Altitude: ");
            sb.Append((_altitude - 10000000) / 100.0);
            sb.Append(" m\r\n");

            _answer = sb.ToString();
		}

        private string CalcLoc(uint angle, char[] nsew) 
		{
			char direction;
			if (angle < 0x80000000) 
			{
				angle = 0x80000000 - angle;
				direction = nsew[1];
			} 
			else 
			{
				angle = angle - 0x80000000;
				direction = nsew[0];
			}
			
			uint tsecs = angle % 1000;
			angle = angle / 1000;
			uint secs = angle % 60;
			angle = angle / 60;
			uint minutes = angle % 60;
			uint degrees = angle / 60;
			
			return degrees + " deg, " + minutes + " min " + secs+ "." + tsecs + " sec " + direction;
		}

        // return size in meters
        private double CalcSize(byte val)
        {
            double size;
            int exponent;

            size = (val & 0xF0) >> 4;
            exponent = (val & 0x0F);
            while (exponent != 0)
            {
                size *= 10;
                exponent--;
            }
            return size / 100;
        }
    }
}
