using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// This class parses JSON data and returns an object describing that data.
    /// Really lightweight JSON handling by ORelio - (c) 2013 - 2014
    /// </summary>
    public static class Json
    {
        /// <summary>
        /// Parse some JSON and return the corresponding JSON object
        /// </summary>
        public static JSONData ParseJson(string json)
        {
            int cursorpos = 0;
            return String2Data(json, ref cursorpos);
        }

        /// <summary>
        /// The class storing unserialized JSON data
        /// The data can be an object, an array or a string
        /// </summary>
        public class JSONData
        {
            public enum DataType { Object, Array, String };
            private DataType type;
            public DataType Type { get { return type; } }
            public Dictionary<string, JSONData> Properties;
            public List<JSONData> DataArray;
            public string StringValue;
            public JSONData(DataType datatype)
            {
                type = datatype;
                Properties = new Dictionary<string, JSONData>();
                DataArray = new List<JSONData>();
                StringValue = String.Empty;
            }
        }

        /// <summary>
        /// Parse a JSON string to build a JSON object
        /// </summary>
        /// <param name="toparse">String to parse</param>
        /// <param name="cursorpos">Cursor start (set to 0 for function init)</param>
        private static JSONData String2Data(string toparse, ref int cursorpos)
        {
            try
            {
                JSONData data;
                switch (toparse[cursorpos])
                {
                    //Object
                    case '{':
                        data = new JSONData(JSONData.DataType.Object);
                        cursorpos++;
                        while (toparse[cursorpos] != '}')
                        {
                            if (toparse[cursorpos] == '"')
                            {
                                JSONData propertyname = String2Data(toparse, ref cursorpos);
                                if (toparse[cursorpos] == ':') { cursorpos++; } else { /* parse error ? */ }
                                JSONData propertyData = String2Data(toparse, ref cursorpos);
                                data.Properties[propertyname.StringValue] = propertyData;
                            }
                            else cursorpos++;
                        }
                        cursorpos++;
                        break;

                    //Array
                    case '[':
                        data = new JSONData(JSONData.DataType.Array);
                        cursorpos++;
                        while (toparse[cursorpos] != ']')
                        {
                            if (toparse[cursorpos] == ',') { cursorpos++; }
                            JSONData arrayItem = String2Data(toparse, ref cursorpos);
                            data.DataArray.Add(arrayItem);
                        }
                        cursorpos++;
                        break;

                    //String
                    case '"':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        while (toparse[cursorpos] != '"')
                        {
                            if (toparse[cursorpos] == '\\')
                            {
                                try //Unicode character \u0123
                                {
                                    if (toparse[cursorpos + 1] == 'u'
                                        && isHex(toparse[cursorpos + 2])
                                        && isHex(toparse[cursorpos + 3])
                                        && isHex(toparse[cursorpos + 4])
                                        && isHex(toparse[cursorpos + 5]))
                                    {
                                        //"abc\u0123abc" => "0123" => 0123 => Unicode char n°0123 => Add char to string
                                        data.StringValue += char.ConvertFromUtf32(int.Parse(toparse.Substring(cursorpos + 2, 4), System.Globalization.NumberStyles.HexNumber));
                                        cursorpos += 6; continue;
                                    }
                                    else if (toparse[cursorpos + 1] == 'n')
                                    {
                                        data.StringValue += '\n';
                                        cursorpos += 2;
                                        continue;
                                    }
                                    else if (toparse[cursorpos + 1] == 'r')
                                    {
                                        data.StringValue += '\r';
                                        cursorpos += 2;
                                        continue;
                                    }
                                    else if (toparse[cursorpos + 1] == 't')
                                    {
                                        data.StringValue += '\t';
                                        cursorpos += 2;
                                        continue;
                                    }
                                    else cursorpos++; //Normal character escapement \"
                                }
                                catch (IndexOutOfRangeException) { cursorpos++; } // \u01<end of string>
                                catch (ArgumentOutOfRangeException) { cursorpos++; } // Unicode index 0123 was invalid
                            }
                            data.StringValue += toparse[cursorpos];
                            cursorpos++;
                        }
                        cursorpos++;
                        break;

                    //Number
                    case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': case '.':
                        data = new JSONData(JSONData.DataType.String);
                        StringBuilder sb = new StringBuilder();
                        while ((toparse[cursorpos] >= '0' && toparse[cursorpos] <= '9') || toparse[cursorpos] == '.')
                        {
                            sb.Append(toparse[cursorpos]);
                            cursorpos++;
                        }
                        data.StringValue = sb.ToString();
                        break;

                    //Boolean : true
                    case 't':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'r') { cursorpos++; }
                        if (toparse[cursorpos] == 'u') { cursorpos++; }
                        if (toparse[cursorpos] == 'e') { cursorpos++; data.StringValue = "true"; }
                        break;

                    //Boolean : false
                    case 'f':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'a') { cursorpos++; }
                        if (toparse[cursorpos] == 'l') { cursorpos++; }
                        if (toparse[cursorpos] == 's') { cursorpos++; }
                        if (toparse[cursorpos] == 'e') { cursorpos++; data.StringValue = "false"; }
                        break;

                    //Unknown data
                    default:
                        cursorpos++;
                        return String2Data(toparse, ref cursorpos);
                }
                while (cursorpos < toparse.Length
                    && (char.IsWhiteSpace(toparse[cursorpos])
                    || toparse[cursorpos] == '\r'
                    || toparse[cursorpos] == '\n'))
                    cursorpos++;
                return data;
            }
            catch (IndexOutOfRangeException)
            {
                return new JSONData(JSONData.DataType.String);
            }
        }

        /// <summary>
        /// Small function for checking if a char is an hexadecimal char (0-9 A-F a-f)
        /// </summary>
        /// <param name="c">Char to test</param>
        /// <returns>True if hexadecimal</returns>
        private static bool isHex(char c) { return ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')); }
    }
}
