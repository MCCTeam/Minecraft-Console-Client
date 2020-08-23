// ZipStorer, by Jaime Olivares
// Website: http://github.com/jaime-olivares/zipstorer

// Copyright(c) 2016 Jaime Olivares
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software 
// without restriction, including without limitation the rights to use, copy, modify, 
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to the following 
// conditions:
//
// The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
// OR OTHER DEALINGS IN THE SOFTWARE.
#define NOASYNC
using System;
using System.Collections.Generic;
using System.Text;

#if !NOASYNC
    using System.Threading.Tasks;
#endif

namespace System.IO.Compression
{
    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// </summary>
    public class ZipStorer : IDisposable
    {
        /// <summary>
        /// Compression method enumeration
        /// </summary>
        public enum Compression : ushort 
        { 
            /// <summary>Uncompressed storage</summary> 
            Store = 0, 
            /// <summary>Deflate compression method</summary>
            Deflate = 8 
        }

        /// <summary>
        /// Represents an entry in Zip file directory
        /// </summary>
        public class ZipFileEntry
        {
            /// <summary>Compression method</summary>
            public Compression Method; 
            /// <summary>Full path and filename as stored in Zip</summary>
            public string FilenameInZip;
            /// <summary>Original file size</summary>
            public long FileSize;
            /// <summary>Compressed file size</summary>
            public long CompressedSize;
            /// <summary>Offset of header information inside Zip storage</summary>
            public long HeaderOffset;
            /// <summary>Offset of file inside Zip storage</summary>
            public long FileOffset;
            /// <summary>Size of header information</summary>
            public uint HeaderSize;
            /// <summary>32-bit checksum of entire file</summary>
            public uint Crc32;
            /// <summary>Last modification time of file</summary>
            public DateTime ModifyTime;
            /// <summary>Creation time of file</summary>
            public DateTime CreationTime;
            /// <summary>Last access time of file</summary>
            public DateTime AccessTime;
            /// <summary>User comment for file</summary>
            public string Comment;
            /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
            public bool EncodeUTF8;

            /// <summary>Overriden method</summary>
            /// <returns>Filename in Zip</returns>
            public override string ToString()
            {
                return this.FilenameInZip;
            }
        }

#region Public fields
        /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
        public bool EncodeUTF8 = false;
        /// <summary>Force deflate algotithm even if it inflates the stored file. Off by default.</summary>
        public bool ForceDeflating = false;
#endregion

#region Private fields
        // List of files to store
        private List<ZipFileEntry> Files = new List<ZipFileEntry>();
        // Filename of storage file
        private string FileName;
        // Stream object of storage file
        private Stream ZipFileStream;
        // General comment
        private string Comment = string.Empty;
        // Central dir image
        private byte[] CentralDirImage = null;
        // Existing files in zip
        private long ExistingFiles = 0;
        // File access for Open method
        private FileAccess Access;
        // leave the stream open after the ZipStorer object is disposed
        private bool leaveOpen;
        // Static CRC32 Table
        private static UInt32[] CrcTable = null;
        // Default filename encoder
        private static Encoding DefaultEncoding = Encoding.GetEncoding(437);
#endregion

#region Public methods
        // Static constructor. Just invoked once in order to create the CRC32 lookup table.
        static ZipStorer()
        {
            // Generate CRC32 table
            CrcTable = new UInt32[256];
            for (int i = 0; i < CrcTable.Length; i++)
            {
                UInt32 c = (UInt32)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 3988292384 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }

        /// <summary>
        /// Method to create a new storage file
        /// </summary>
        /// <param name="_filename">Full path of Zip file to create</param>
        /// <param name="_comment">General comment for Zip file</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(string _filename, string _comment = null)
        {
            Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.ReadWrite);

            ZipStorer zip = Create(stream, _comment);
            zip.Comment = _comment ?? string.Empty;
            zip.FileName = _filename;

            return zip;
        }

        /// <summary>
        /// Method to create a new zip storage in a stream
        /// </summary>
        /// <param name="_stream"></param>
        /// <param name="_comment"></param>
        /// <param name="_leaveOpen">true to leave the stream open after the ZipStorer object is disposed; otherwise, false (default).</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(Stream _stream, string _comment = null, bool _leaveOpen = false)
        {
            ZipStorer zip = new ZipStorer()
            {
                Comment = _comment ?? string.Empty,
                ZipFileStream = _stream,
                Access = FileAccess.Write,
                leaveOpen = _leaveOpen
            };

            return zip;
        }

        /// <summary>
        /// Method to open an existing storage file
        /// </summary>
        /// <param name="_filename">Full path of Zip file to open</param>
        /// <param name="_access">File access mode as used in FileStream constructor</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open(string _filename, FileAccess _access)
        {
            Stream stream = (Stream)new FileStream(_filename, FileMode.Open, _access == FileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite);

            ZipStorer zip = Open(stream, _access);
            zip.FileName = _filename;

            return zip;
        }

        /// <summary>
        /// Method to open an existing storage from stream
        /// </summary>
        /// <param name="_stream">Already opened stream with zip contents</param>
        /// <param name="_access">File access mode for stream operations</param>
        /// <param name="_leaveOpen">true to leave the stream open after the ZipStorer object is disposed; otherwise, false (default).</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open(Stream _stream, FileAccess _access, bool _leaveOpen = false)
        {
            if (!_stream.CanSeek && _access != FileAccess.Read)
                throw new InvalidOperationException("Stream cannot seek");

            ZipStorer zip = new ZipStorer()
            {
                ZipFileStream = _stream,
                Access = _access,
                leaveOpen = _leaveOpen
            };

            if (zip.ReadFileInfo())
                return zip;
            
            if (!_leaveOpen)
                zip.Close();
            
            throw new System.IO.InvalidDataException();
        }

        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_pathname">Full path of file to add to Zip storage</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_comment">Comment for stored file</param>        
        public ZipFileEntry AddFile(Compression _method, string _pathname, string _filenameInZip, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not alowed");

            using (var stream = new FileStream(_pathname, FileMode.Open, FileAccess.Read))
            {
                return this.AddStream(_method, _filenameInZip, stream, File.GetLastWriteTime(_pathname), _comment);
            }
        }

        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <remarks>Same parameters and return value as AddStreamAsync()</remarks>
        public ZipFileEntry AddStream(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
        {
#if NOASYNC
            return this.AddStreamAsync(_method, _filenameInZip, _source, _modTime, _comment);
#else            
            return Task.Run(() => this.AddStreamAsync(_method, _filenameInZip, _source, _modTime, _comment)).Result;
#endif
        }

        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_source">Stream object containing the data to store in Zip</param>
        /// <param name="_modTime">Modification time of the data to store</param>
        /// <param name="_comment">Comment for stored file</param>
#if NOASYNC
        private ZipFileEntry
#else                
        public async Task<ZipFileEntry> 
#endif
        AddStreamAsync(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not alowed");

            // Prepare the fileinfo
            ZipFileEntry zfe = new ZipFileEntry()
            {
                Method = _method,
                EncodeUTF8 = this.EncodeUTF8,
                FilenameInZip = NormalizedFilename(_filenameInZip),
                Comment = _comment ?? string.Empty,
                Crc32 = 0,  // to be updated later
                HeaderOffset = (uint)this.ZipFileStream.Position,  // offset within file of the start of this local record
                CreationTime = _modTime,
                ModifyTime = _modTime,
                AccessTime = _modTime
            };

            // Write local header
            this.WriteLocalHeader(zfe);
            zfe.FileOffset = (uint)this.ZipFileStream.Position;

            // Write file to zip (store)
            #if NOASYNC
                Store(zfe, _source);
            #else
                await Store(zfe, _source);
            #endif

            _source.Close();
            this.UpdateCrcAndSizes(zfe);
            Files.Add(zfe);

            return zfe;
        }

        /// <summary>
        /// Add full contents of a directory into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_pathname">Full path of directory to add to Zip storage</param>
        /// <param name="_pathnameInZip">Path name as desired in Zip directory</param>
        /// <param name="_comment">Comment for stored directory</param>
        public void AddDirectory(Compression _method, string _pathname, string _pathnameInZip, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not allowed");

            string foldername;
            int pos = _pathname.LastIndexOf(Path.DirectorySeparatorChar);
            string separator = Path.DirectorySeparatorChar.ToString();

            if (pos >= 0)
                foldername = _pathname.Remove(0, pos + 1);
            else
                foldername = _pathname;

            if (!string.IsNullOrEmpty(_pathnameInZip))
                foldername = _pathnameInZip + foldername;

            if (!foldername.EndsWith(separator, StringComparison.CurrentCulture))
                foldername = foldername + separator;

            // this.AddStream(_method, foldername, null, File.GetLastWriteTime(_pathname), _comment);

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(_pathname);

            foreach (string fileName in fileEntries)
                this.AddFile(_method, fileName, foldername + Path.GetFileName(fileName), "");

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(_pathname);

            foreach (string subdirectory in subdirectoryEntries)
                this.AddDirectory(_method, subdirectory, foldername, "");
        }

        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// </summary>
        /// <remarks>This is a required step, unless automatic dispose is used</remarks>
        public void Close()
        {
            if (this.Access != FileAccess.Read)
            {
                uint centralOffset = (uint)this.ZipFileStream.Position;
                uint centralSize = 0;

                if (this.CentralDirImage != null)
                    this.ZipFileStream.Write(CentralDirImage, 0, CentralDirImage.Length);

                for (int i = 0; i < Files.Count; i++)
                {
                    long pos = this.ZipFileStream.Position;
                    this.WriteCentralDirRecord(Files[i]);
                    centralSize += (uint)(this.ZipFileStream.Position - pos);
                }

                if (this.CentralDirImage != null)
                    this.WriteEndRecord(centralSize + (uint)CentralDirImage.Length, centralOffset);
                else
                    this.WriteEndRecord(centralSize, centralOffset);
            }

            if (this.ZipFileStream != null && !this.leaveOpen)
            {
                this.ZipFileStream.Flush();
                this.ZipFileStream.Dispose();
                this.ZipFileStream = null;
            }
        }

        /// <summary>
        /// Read all the file records in the central directory 
        /// </summary>
        /// <returns>List of all entries in directory</returns>
        public List<ZipFileEntry> ReadCentralDir()
        {
            if (this.CentralDirImage == null)
                throw new InvalidOperationException("Central directory currently does not exist");

            List<ZipFileEntry> result = new List<ZipFileEntry>();

            for (int pointer = 0; pointer < this.CentralDirImage.Length; )
            {
                uint signature = BitConverter.ToUInt32(CentralDirImage, pointer);
                if (signature != 0x02014b50)
                    break;

                bool encodeUTF8 = (BitConverter.ToUInt16(CentralDirImage, pointer + 8) & 0x0800) != 0;
                ushort method = BitConverter.ToUInt16(CentralDirImage, pointer + 10);
                uint modifyTime = BitConverter.ToUInt32(CentralDirImage, pointer + 12);
                uint crc32 = BitConverter.ToUInt32(CentralDirImage, pointer + 16);
                long comprSize = BitConverter.ToUInt32(CentralDirImage, pointer + 20);
                long fileSize = BitConverter.ToUInt32(CentralDirImage, pointer + 24);
                ushort filenameSize = BitConverter.ToUInt16(CentralDirImage, pointer + 28);
                ushort extraSize = BitConverter.ToUInt16(CentralDirImage, pointer + 30);
                ushort commentSize = BitConverter.ToUInt16(CentralDirImage, pointer + 32);
                uint headerOffset = BitConverter.ToUInt32(CentralDirImage, pointer + 42);
                uint headerSize = (uint)( 46 + filenameSize + extraSize + commentSize);
                DateTime modifyTimeDT = DosTimeToDateTime(modifyTime) ?? DateTime.Now;

                Encoding encoder = encodeUTF8 ? Encoding.UTF8 : DefaultEncoding;

                ZipFileEntry zfe = new ZipFileEntry()
                {
                    Method = (Compression)method,
                    FilenameInZip = encoder.GetString(CentralDirImage, pointer + 46, filenameSize),
                    FileOffset = GetFileOffset(headerOffset),
                    FileSize = fileSize,
                    CompressedSize = comprSize,
                    HeaderOffset = headerOffset,
                    HeaderSize = headerSize,
                    Crc32 = crc32,
                    ModifyTime = modifyTimeDT,
                    CreationTime = modifyTimeDT,
                    AccessTime = DateTime.Now,
                };

                if (commentSize > 0)
                    zfe.Comment = encoder.GetString(CentralDirImage, pointer + 46 + filenameSize + extraSize, commentSize);

                if (extraSize > 0)
                {
                    this.ReadExtraInfo(CentralDirImage, pointer + 46 + filenameSize, zfe);
                }

                result.Add(zfe);
                pointer += (46 + filenameSize + extraSize + commentSize);
            }

            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into a physical file
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract</param>
        /// <param name="_filename">Name of file to store uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile(ZipFileEntry _zfe, string _filename)
        {
            // Make sure the parent directory exist
            string path = Path.GetDirectoryName(_filename);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // Check if it is a directory. If so, do nothing.
            if (Directory.Exists(_filename))
                return true;

            bool result;
            using(var output = new FileStream(_filename, FileMode.Create, FileAccess.Write))
            {
                result = this.ExtractFile(_zfe, output);
            }
                
            if (result)
            {
                File.SetCreationTime(_filename, _zfe.CreationTime);
                File.SetLastWriteTime(_filename, _zfe.ModifyTime);
                File.SetLastAccessTime(_filename, _zfe.AccessTime);
            }
            
            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// </summary>
        /// <remarks>Same parameters and return value as ExtractFileAsync</remarks>
        public bool ExtractFile(ZipFileEntry _zfe, Stream _stream)
        {
#if NOASYNC
            return this.ExtractFileAsync(_zfe, _stream);
#else                
            return Task.Run(() => ExtractFileAsync(_zfe, _stream)).Result;
#endif
        }

        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract</param>
        /// <param name="_stream">Stream to store the uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
#if NOASYNC
        private bool
#else                
        public async Task<bool> 
#endif
        ExtractFileAsync(ZipFileEntry _zfe, Stream _stream)
        {
            if (!_stream.CanWrite)
                throw new InvalidOperationException("Stream cannot be written");

            // check signature
            byte[] signature = new byte[4];
            this.ZipFileStream.Seek(_zfe.HeaderOffset, SeekOrigin.Begin);
            
            #if NOASYNC
                this.ZipFileStream.Read(signature, 0, 4);
            #else
                await this.ZipFileStream.ReadAsync(signature, 0, 4);
            #endif

            if (BitConverter.ToUInt32(signature, 0) != 0x04034b50)
                return false;

            // Select input stream for inflating or just reading
            Stream inStream;

            if (_zfe.Method == Compression.Store)
                inStream = this.ZipFileStream;
            else if (_zfe.Method == Compression.Deflate)
                inStream = new DeflateStream(this.ZipFileStream, CompressionMode.Decompress, true);
            else
                return false;

            // Buffered copy
            byte[] buffer = new byte[65535];
            this.ZipFileStream.Seek(_zfe.FileOffset, SeekOrigin.Begin);
            long bytesPending = _zfe.FileSize;

            while (bytesPending > 0)
            {
                #if NOASYNC
                    int bytesRead = inStream.Read(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                    _stream.Write(buffer, 0, bytesRead);
                #else
                    int bytesRead = await inStream.ReadAsync(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                    await _stream.WriteAsync(buffer, 0, bytesRead);
                #endif
                                        
                bytesPending -= (uint)bytesRead;
            }
            _stream.Flush();

            if (_zfe.Method == Compression.Deflate)
                inStream.Dispose();

            return true;
        }

        /// <summary>
        /// Copy the contents of a stored file into a byte array
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract</param>
        /// <param name="_file">Byte array with uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile(ZipFileEntry _zfe, out byte[] _file)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (ExtractFile(_zfe, ms))
                {
                    _file = ms.ToArray();
                    return true;
                }
                else
                {
                    _file = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes one of many files in storage. It creates a new Zip file.
        /// </summary>
        /// <param name="_zip">Reference to the current Zip object</param>
        /// <param name="_zfes">List of Entries to remove from storage</param>
        /// <returns>True if success, false if not</returns>
        /// <remarks>This method only works for storage of type FileStream</remarks>
        public static bool RemoveEntries(ref ZipStorer _zip, List<ZipFileEntry> _zfes)
        {
            if (!(_zip.ZipFileStream is FileStream))
                throw new InvalidOperationException("RemoveEntries is allowed just over streams of type FileStream");

            //Get full list of entries
            var fullList = _zip.ReadCentralDir();

            //In order to delete we need to create a copy of the zip file excluding the selected items
            var tempZipName = Path.GetTempFileName();
            var tempEntryName = Path.GetTempFileName();

            try
            {
                var tempZip = ZipStorer.Create(tempZipName, string.Empty);

                foreach (ZipFileEntry zfe in fullList)
                {
                    if (!_zfes.Contains(zfe))
                    {
                        if (_zip.ExtractFile(zfe, tempEntryName))
                        {
                            tempZip.AddFile(zfe.Method, tempEntryName, zfe.FilenameInZip, zfe.Comment);
                        }
                    }
                }

                _zip.Close();
                tempZip.Close();

                File.Delete(_zip.FileName);
                File.Move(tempZipName, _zip.FileName);

                _zip = ZipStorer.Open(_zip.FileName, _zip.Access);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(tempZipName))
                    File.Delete(tempZipName);
                if (File.Exists(tempEntryName))
                    File.Delete(tempEntryName);
            }
            return true;
        }
#endregion

#region Private methods
        // Calculate the file offset by reading the corresponding local header
        private uint GetFileOffset(uint _headerOffset)
        {
            byte[] buffer = new byte[2];

            this.ZipFileStream.Seek(_headerOffset + 26, SeekOrigin.Begin);
            this.ZipFileStream.Read(buffer, 0, 2);
            ushort filenameSize = BitConverter.ToUInt16(buffer, 0);
            this.ZipFileStream.Read(buffer, 0, 2);
            ushort extraSize = BitConverter.ToUInt16(buffer, 0);

            return (uint)(30 + filenameSize + extraSize + _headerOffset);
        }

        /* Local file header:
            local file header signature     4 bytes  (0x04034b50)
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes

            filename (variable size)
            extra field (variable size)
        */
        private void WriteLocalHeader(ZipFileEntry _zfe)
        {
            long pos = this.ZipFileStream.Position;
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] extraInfo = this.CreateExtraInfo(_zfe);

            this.ZipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0}, 0, 6); // No extra header
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            this.ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4); // zipping date and time
            this.ZipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12); // unused CRC, un/compressed size, updated later
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // filename length
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2); // extra length

            this.ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            this.ZipFileStream.Write(extraInfo, 0, extraInfo.Length);
            _zfe.HeaderSize = (uint)(this.ZipFileStream.Position - pos);
        }

        /* Central directory's File header:
            central file header signature   4 bytes  (0x02014b50)
            version made by                 2 bytes
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            file comment length             2 bytes
            disk number start               2 bytes
            internal file attributes        2 bytes
            external file attributes        4 bytes
            relative offset of local header 4 bytes

            filename (variable size)
            extra field (variable size)
            file comment (variable size)
        */
        private void WriteCentralDirRecord(ZipFileEntry _zfe)
        {
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] encodedComment = encoder.GetBytes(_zfe.Comment);
            byte[] extraInfo = this.CreateExtraInfo(_zfe);

            this.ZipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            this.ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);  // zipping date and time
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // file CRC
            this.ZipFileStream.Write(BitConverter.GetBytes(get32bitSize(_zfe.CompressedSize)), 0, 4); // compressed file size
            this.ZipFileStream.Write(BitConverter.GetBytes(get32bitSize(_zfe.FileSize)), 0, 4); // uncompressed file size
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2); // extra length
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);

            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // disk=0
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // file type: binary
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // Internal file attributes
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2); // External file attributes (normal/readable)
            this.ZipFileStream.Write(BitConverter.GetBytes(get32bitSize(_zfe.HeaderOffset)), 0, 4);  // Offset of header

            this.ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            this.ZipFileStream.Write(extraInfo, 0, extraInfo.Length);
            this.ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        private uint get32bitSize(long size)
        {
            return size >= 0xFFFFFFFF ? 0xFFFFFFFF : (uint)size;
        }

        /* 
        Zip64 end of central directory record
            zip64 end of central dir 
            signature                       4 bytes  (0x06064b50)
            size of zip64 end of central
            directory record                8 bytes
            version made by                 2 bytes
            version needed to extract       2 bytes
            number of this disk             4 bytes
            number of the disk with the 
            start of the central directory  4 bytes
            total number of entries in the
            central directory on this disk  8 bytes
            total number of entries in the
            central directory               8 bytes
            size of the central directory   8 bytes
            offset of start of central
            directory with respect to
            the starting disk number        8 bytes
            zip64 extensible data sector    (variable size)        
        
        Zip64 end of central directory locator

            zip64 end of central dir locator 
            signature                       4 bytes  (0x07064b50)
            number of the disk with the
            start of the zip64 end of 
            central directory               4 bytes
            relative offset of the zip64
            end of central directory record 8 bytes
            total number of disks           4 bytes

        End of central dir record:
            end of central dir signature    4 bytes  (0x06054b50)
            number of this disk             2 bytes
            number of the disk with the
            start of the central directory  2 bytes
            total number of entries in
            the central dir on this disk    2 bytes
            total number of entries in
            the central dir                 2 bytes
            size of the central directory   4 bytes
            offset of start of central
            directory with respect to
            the starting disk number        4 bytes
            zipfile comment length          2 bytes
            zipfile comment (variable size)
        */
        private void WriteEndRecord(long _size, long _offset)
        {
            long dirOffset = ZipFileStream.Length;

            // Zip64 end of central directory record
            this.ZipFileStream.Position = dirOffset;
            this.ZipFileStream.Write(new byte[] { 80, 75, 6, 6 }, 0, 4);
            this.ZipFileStream.Write(BitConverter.GetBytes((Int64)44), 0, 8); // size of zip64 end of central directory
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt16)45), 0, 2); // version made by
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt16)45), 0, 2); // version needed to extract 
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt32)0), 0, 4); // current disk
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt32)0), 0, 4); // start of central directory 
            this.ZipFileStream.Write(BitConverter.GetBytes((Int64)Files.Count+ExistingFiles), 0, 8); // total number of entries in the central directory in disk
            this.ZipFileStream.Write(BitConverter.GetBytes((Int64)Files.Count+ExistingFiles), 0, 8); // total number of entries in the central directory
            this.ZipFileStream.Write(BitConverter.GetBytes(_size), 0, 8); // size of the central directory
            this.ZipFileStream.Write(BitConverter.GetBytes(_offset), 0, 8); // offset of start of central directory with respect to the starting disk number

            // Zip64 end of central directory locator
            this.ZipFileStream.Write(new byte[] { 80, 75, 6, 7 }, 0, 4);
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt32)0), 0, 4); // number of the disk 
            this.ZipFileStream.Write(BitConverter.GetBytes(dirOffset), 0, 8); // relative offset of the zip64 end of central directory record
            this.ZipFileStream.Write(BitConverter.GetBytes((UInt32)1), 0, 4); // total number of disks 

            Encoding encoder = this.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedComment = encoder.GetBytes(this.Comment);

            this.ZipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            this.ZipFileStream.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, 12);
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            this.ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        // Copies all the source file into the zip storage
#if NOASYNC
        private Compression
#else                
        private async Task<Compression> 
#endif
        Store(ZipFileEntry _zfe, Stream _source)
        {
            byte[] buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;
            Stream outStream;

            long posStart = this.ZipFileStream.Position;
            long sourceStart = _source.CanSeek ? _source.Position : 0;

            if (_zfe.Method == Compression.Store)
                outStream = this.ZipFileStream;
            else
                outStream = new DeflateStream(this.ZipFileStream, CompressionMode.Compress, true);

            _zfe.Crc32 = 0 ^ 0xffffffff;
            
            do
            {
#if NOASYNC
                bytesRead = _source.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                    outStream.Write(buffer, 0, bytesRead);
#else
                bytesRead = await _source.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                    await outStream.WriteAsync(buffer, 0, bytesRead);
#endif 

                for (uint i = 0; i < bytesRead; i++)
                {
                    _zfe.Crc32 = ZipStorer.CrcTable[(_zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (_zfe.Crc32 >> 8);
                }

                totalRead += (uint)bytesRead;
            } while (bytesRead > 0);

            outStream.Flush();

            if (_zfe.Method == Compression.Deflate)
                outStream.Dispose();

            _zfe.Crc32 ^= 0xFFFFFFFF;
            _zfe.FileSize = totalRead;
            _zfe.CompressedSize = (uint)(this.ZipFileStream.Position - posStart);

            // Verify for real compression
            if (_zfe.Method == Compression.Deflate && !this.ForceDeflating && _source.CanSeek && _zfe.CompressedSize > _zfe.FileSize)
            {
                // Start operation again with Store algorithm
                _zfe.Method = Compression.Store;
                this.ZipFileStream.Position = posStart;
                this.ZipFileStream.SetLength(posStart);
                _source.Position = sourceStart;

                #if NOASYNC                
                    return this.Store(_zfe, _source);
                #else
                    return await this.Store(_zfe, _source);
                #endif
            }

            return _zfe.Method;
        }

        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description 
                0-4 Day of the month (131) 
                5-8 Month (1 = January, 2 = February, and so on) 
                9-15 Year offset from 1980 (add 1980 to get actual year) 
            MS-DOS time. The time is a packed value with the following format. Bits Description 
                0-4 Second divided by 2 
                5-10 Minute (059) 
                11-15 Hour (023 on a 24-hour clock) 
        */
        private uint DateTimeToDosTime(DateTime _dt)
        {
            return (uint)(
                (_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) | 
                (_dt.Day<<16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));
        }

        private DateTime? DosTimeToDateTime(uint _dt)
        {
            int year = (int)(_dt >> 25) + 1980;
            int month = (int)(_dt >> 21) & 15;
            int day = (int)(_dt >> 16) & 31;
            int hours = (int)(_dt >> 11) & 31;
            int minutes = (int)(_dt >> 5) & 63;
            int seconds = (int)(_dt & 31) * 2;

            if (month == 0 || day == 0 || year >= 2107)
                return DateTime.Now;

            return new DateTime(year, month, day, hours, minutes, seconds);
        }

        private byte[] CreateExtraInfo(ZipFileEntry _zfe)
        {
            byte[] buffer = new byte[36+36];
            BitConverter.GetBytes((ushort)0x0001).CopyTo(buffer, 0); // ZIP64 Information
            BitConverter.GetBytes((ushort)32).CopyTo(buffer, 2); // Length
            BitConverter.GetBytes((ushort)1).CopyTo(buffer, 8); // Tag 1
            BitConverter.GetBytes((ushort)24).CopyTo(buffer, 10); // Size 1
            BitConverter.GetBytes(_zfe.FileSize).CopyTo(buffer, 12); // MTime
            BitConverter.GetBytes(_zfe.CompressedSize).CopyTo(buffer, 20); // ATime
            BitConverter.GetBytes(_zfe.HeaderOffset).CopyTo(buffer, 28); // CTime

            BitConverter.GetBytes((ushort)0x000A).CopyTo(buffer, 36); // NTFS FileTime
            BitConverter.GetBytes((ushort)32).CopyTo(buffer, 38); // Length
            BitConverter.GetBytes((ushort)1).CopyTo(buffer, 44); // Tag 1
            BitConverter.GetBytes((ushort)24).CopyTo(buffer, 46); // Size 1
            BitConverter.GetBytes(_zfe.ModifyTime.ToFileTime()).CopyTo(buffer, 48); // MTime
            BitConverter.GetBytes(_zfe.AccessTime.ToFileTime()).CopyTo(buffer, 56); // ATime
            BitConverter.GetBytes(_zfe.CreationTime.ToFileTime()).CopyTo(buffer, 64); // CTime

            return buffer;
        }

        private void ReadExtraInfo(byte[] buffer, int offset, ZipFileEntry _zfe)
        {
            if (buffer.Length < 4)
                return;

            int pos = offset;
            uint tag, size;

            while (pos < buffer.Length - 4)
            {
                uint extraId = BitConverter.ToUInt16(buffer, pos);
                uint length = BitConverter.ToUInt16(buffer, pos+2);

                if (extraId == 0x0001) // ZIP64 Information
                {
                    tag = BitConverter.ToUInt16(buffer, pos + 8);
                    size = BitConverter.ToUInt16(buffer, pos + 10);

                    if (tag == 1 && size >= 24)
                    {
                        if (_zfe.FileSize == 0xFFFFFFFF)
                            _zfe.FileSize = BitConverter.ToInt64(buffer, pos+12);
                        if (_zfe.CompressedSize == 0xFFFFFFFF)
                            _zfe.CompressedSize = BitConverter.ToInt64(buffer, pos+20);
                        if (_zfe.HeaderOffset == 0xFFFFFFFF)
                            _zfe.HeaderOffset = BitConverter.ToInt64(buffer, pos+28);
                    }
                }

                if (extraId == 0x000A) // NTFS FileTime
                {
                    tag = BitConverter.ToUInt16(buffer, pos + 8);
                    size = BitConverter.ToUInt16(buffer, pos + 10);

                    if (tag == 1 && size == 24)
                    {
                        _zfe.ModifyTime = DateTime.FromFileTime(BitConverter.ToInt64(buffer, pos+12));
                        _zfe.AccessTime = DateTime.FromFileTime(BitConverter.ToInt64(buffer, pos+20));
                        _zfe.CreationTime = DateTime.FromFileTime(BitConverter.ToInt64(buffer, pos+28));
                    }
                }

                pos += (int)length + 4;
            }        
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central directory.
        */
        private void UpdateCrcAndSizes(ZipFileEntry _zfe)
        {
            long lastPos = this.ZipFileStream.Position;  // remember position

            this.ZipFileStream.Position = _zfe.HeaderOffset + 8;
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method

            this.ZipFileStream.Position = _zfe.HeaderOffset + 14;
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);  // Update CRC
            this.ZipFileStream.Write(BitConverter.GetBytes(get32bitSize(_zfe.CompressedSize)), 0, 4);  // Compressed size
            this.ZipFileStream.Write(BitConverter.GetBytes(get32bitSize(_zfe.FileSize)), 0, 4);  // Uncompressed size

            this.ZipFileStream.Position = lastPos;  // restore position
        }

        // Replaces backslashes with slashes to store in zip header
        private string NormalizedFilename(string _filename)
        {
            string filename = _filename.Replace('\\', '/');

            int pos = filename.IndexOf(':');
            if (pos >= 0)
                filename = filename.Remove(0, pos + 1);

            return filename.Trim('/');
        }

        // Reads the end-of-central-directory record
        private bool ReadFileInfo()
        {
            if (this.ZipFileStream.Length < 22)
                return false;

            try
            {
                this.ZipFileStream.Seek(-17, SeekOrigin.End);
                BinaryReader br = new BinaryReader(this.ZipFileStream);
                do
                {
                    this.ZipFileStream.Seek(-5, SeekOrigin.Current);
                    UInt32 sig = br.ReadUInt32();

                    if (sig == 0x06054b50) // It is central dir
                    {
                        long dirPosition = ZipFileStream.Position - 4;

                        this.ZipFileStream.Seek(6, SeekOrigin.Current);

                        long entries = br.ReadUInt16();
                        long centralSize = br.ReadInt32();
                        long centralDirOffset = br.ReadUInt32();
                        UInt16 commentSize = br.ReadUInt16();

                        var commentPosition = ZipFileStream.Position;

                        if (centralDirOffset == 0xffffffff) // It is a Zip64 file
                        {                            
                            this.ZipFileStream.Position = dirPosition - 20;

                            sig = br.ReadUInt32();

                            if (sig != 0x07064b50) // Not a Zip64 central dir locator
                                return false;

                            this.ZipFileStream.Seek(4, SeekOrigin.Current);

                            long dir64Position = br.ReadInt64();
                            this.ZipFileStream.Position = dir64Position;

                            sig = br.ReadUInt32();

                            if (sig != 0x06064b50) // Not a Zip64 central dir record
                                return false;

                            this.ZipFileStream.Seek(28, SeekOrigin.Current);
                            entries = br.ReadInt64();
                            centralSize = br.ReadInt64();
                            centralDirOffset = br.ReadInt64();
                        }

                        // check if comment field is the very last data in file
                        if (commentPosition + commentSize != this.ZipFileStream.Length)
                            return false;

                        // Copy entire central directory to a memory buffer
                        this.ExistingFiles = entries;
                        this.CentralDirImage = new byte[centralSize];
                        this.ZipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        this.ZipFileStream.Read(this.CentralDirImage, 0, (int)centralSize);

                        // Leave the pointer at the begining of central dir, to append new files
                        this.ZipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        return true;
                    }
                } while (this.ZipFileStream.Position > 0);
            }
            catch { }

            return false;
        }
#endregion

#region IDisposable Members
        /// <summary>
        /// Closes the Zip file stream
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }
#endregion
    }
}
