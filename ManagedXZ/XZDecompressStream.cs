using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    public class XZDecompressStream : Stream
    {
        public XZDecompressStream(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            _stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Init();
        }

        public XZDecompressStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("stream is not readable");

            _stream = stream;
            Init();
        }

        private readonly Stream _stream;
        private readonly lzma_stream _lzma_stream = new lzma_stream();
        private IntPtr _inbuf;
        private IntPtr _outbuf;
        private int read_pos;
        private lzma_action action = lzma_action.LZMA_RUN;
        private lzma_ret ret = lzma_ret.LZMA_OK;
        private const int BUFSIZE = 1024*512; // for decompress and read operation, a relatively large buffer is reasonable

        private void Init()
        {
            var ret = Native.lzma_auto_decoder(_lzma_stream, ulong.MaxValue, Native.LZMA_CONCATENATED);
            if (ret != lzma_ret.LZMA_OK)
                throw new Exception($"Can not create lzma stream: {ret}");

            _inbuf = Marshal.AllocHGlobal(BUFSIZE);
            _outbuf = Marshal.AllocHGlobal(BUFSIZE);

            // init lzma_stream
            _lzma_stream.next_in = _inbuf;
            _lzma_stream.next_out = _outbuf;
            _lzma_stream.avail_in = UIntPtr.Zero;
            _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        /// <summary>
        /// learn from 02_decompress.c
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count + offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count), "offset+count>buffer.length");
            if (count == 0) return 0;

            int cTotalRead = 0;
            while (true)
            {
                // read from underlying stream
                if (_lzma_stream.avail_in == UIntPtr.Zero && action == lzma_action.LZMA_RUN)
                {
                    // read more data from underlying stream
                    var data = new byte[BUFSIZE];
                    var bytesRead = _stream.Read(data, 0, BUFSIZE);
                    if (bytesRead < BUFSIZE) action = lzma_action.LZMA_FINISH; // source stream has no more data
                    _lzma_stream.next_in = _inbuf;
                    _lzma_stream.avail_in = (UIntPtr)bytesRead;
                    Marshal.Copy(data, 0, _inbuf, bytesRead);
                }

                // try to read from existing outbuf
                int cReadable = BUFSIZE - (int)(uint)_lzma_stream.avail_out - read_pos;
                if (cReadable > 0)
                {
                    var cCopy = Math.Min(cReadable, count - cTotalRead);
                    Marshal.Copy(IntPtr.Add(_outbuf, read_pos), buffer, offset + cTotalRead, cCopy);
                    cTotalRead += cCopy;
                    read_pos += cCopy;
                    Trace.Assert(cTotalRead <= count);
                    if (cTotalRead == count)
                        return cTotalRead;
                }

                // need to read more data from outbuf
                // if previous decode returns LZMA_STREAM_END, there will be no more data
                if (ret == lzma_ret.LZMA_STREAM_END)
                    return cTotalRead;

                // otherwise, reset outbuf to recv more decompressed data from liblzma, or decompress is finished
                Trace.Assert(read_pos + (uint)_lzma_stream.avail_out <= BUFSIZE);
                if (_lzma_stream.avail_out == UIntPtr.Zero && read_pos + (uint)_lzma_stream.avail_out == BUFSIZE)
                {
                    _lzma_stream.next_out = _outbuf;
                    _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
                    read_pos = 0;
                }
                
                // do decompress
                ret = Native.lzma_code(_lzma_stream, action);
                if (ret != lzma_ret.LZMA_OK && ret != lzma_ret.LZMA_STREAM_END)
                    throw new Exception($"lzma_code returns {ret}");
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            Native.lzma_end(_lzma_stream);
            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);
            _stream.Close();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}