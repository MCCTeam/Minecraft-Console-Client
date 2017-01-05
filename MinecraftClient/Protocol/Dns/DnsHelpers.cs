using System;
using System.Diagnostics;
using System.Text;

namespace DnDns
{
    internal static class DnsHelpers
    {
        private const long Epoch = 621355968000000000;

        internal static byte[] CanonicaliseDnsName(string name, bool lowerCase)
        {
            if (!name.EndsWith("."))
            {
                name += ".";
            }

            if (name == ".")
            {
                return new byte[1];
            }

            StringBuilder sb = new StringBuilder();

            sb.Append('\0');

            for (int i = 0, j = 0; i < name.Length; i++, j++)
            {
                if (lowerCase)
                {
                    sb.Append(char.ToLower(name[i]));
                }
                else
                {
                    sb.Append(name[i]);
                }

                if (name[i] == '.')
                {
                    sb[i - j] = (char) (j & 0xff);
                    j = -1;
                }
            }

            sb[sb.Length - 1] = '\0';

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        internal static String DumpArrayToString(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder();
            
            builder.Append("[");
            
            foreach (byte b in bytes)
            {
                builder.Append(" ");
                builder.Append((sbyte)b);
                builder.Append(" ");
            }

            builder.Append("]");

            return builder.ToString();
        }

        /// <summary>
        /// Converts a instance of a <see cref="DateTime"/> class to a 48 bit format time since epoch.
        /// Epoch is defined as 1-Jan-70 UTC.
        /// </summary>
        /// <param name="dateTimeToConvert">The <see cref="DateTime"/> instance to convert to DNS format.</param>
        /// <param name="timeHigh">The upper 16 bits of time.</param>
        /// <param name="timeLow">The lower 32 bits of the time object.</param>
        internal static void ConvertToDnsTime(DateTime dateTimeToConvert, out int timeHigh, out long timeLow)
        {
            long secondsFromEpoch = (dateTimeToConvert.ToUniversalTime().Ticks - Epoch) / 10000000;
            timeHigh = (int)(secondsFromEpoch >> 32);
            timeLow = (secondsFromEpoch & 0xFFFFFFFFL);

            Trace.WriteLine(String.Format("Date: {0}", dateTimeToConvert));
            Trace.WriteLine(String.Format("secondsFromEpoch: {0}", secondsFromEpoch));
            Trace.WriteLine(String.Format("timeHigh: {0}", timeHigh));
            Trace.WriteLine(String.Format("timeLow: {0}", timeLow));
        }

        /// <summary>
        /// Convert from DNS 48 but time format to a <see cref="DateTime"/> instance. 
        /// </summary>
        /// <param name="timeHigh">The upper 16 bits of time.</param>
        /// <param name="timeLow">The lower 32 bits of the time object.</param>
        /// <returns>The converted date time</returns>
        internal static DateTime ConvertFromDnsTime(long timeLow, long timeHigh)
        {
            long time = (timeHigh << 32) + timeLow;
            time = time*10000000;
            time += Epoch;

            return new DateTime(time);
        }

        /// <summary>
        /// Convert from DNS 48 but time format to a <see cref="DateTime"/> instance. 
        /// </summary>
        /// <param name="dnsTime">The upper 48 bits of time.</param>
        /// <returns>The converted date time</returns>
        internal static DateTime ConvertFromDnsTime(long dnsTime)
        {
            dnsTime = dnsTime * 10000000;
            dnsTime += Epoch;

            return new DateTime(dnsTime);
        }
    }
}
