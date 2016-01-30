using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    public class XZCompressStream : Stream
    {
        public XZCompressStream(string filename) : this(filename, 1, 6)
        {
        }

        public XZCompressStream(string filename, int threads) : this(filename, threads, 6)
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="threads">number of threads for parallel compress</param>
        /// <param name="level">0-9, the bigger, the slower, and higher compression ratio</param>
        public XZCompressStream(string filename, int threads, int level)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (threads <= 0) throw new ArgumentOutOfRangeException(nameof(threads));
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level));

            _stream = new FileStream(filename, FileMode.Append, FileAccess.Write);
            _threads = threads;
            _preset = (uint)level;
            Init();
        }

        public XZCompressStream(Stream stream) : this(stream, 1, 6)
        {
        }

        public XZCompressStream(Stream stream, int threads) : this(stream, threads, 6)
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="threads">number of threads for parallel compress</param>
        /// <param name="level">0-9, the bigger, the slower, and higher compression ratio</param>
        public XZCompressStream(Stream stream, int threads, int level)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite) throw new ArgumentException("stream is not writable");
            if (threads <= 0) throw new ArgumentOutOfRangeException(nameof(threads));
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level));

            _stream = stream;
            _threads = threads;
            _preset = (uint)level;
            Init();
        }

        private readonly Stream _stream;
        private readonly int _threads;
        private readonly uint _preset;
        private readonly lzma_stream _lzma_stream = new lzma_stream();
        private IntPtr _inbuf;
        private IntPtr _outbuf;
        private const int BUFSIZE = 1024*32; // we do not need a big outbuf

        private void Init()
        {
            lzma_ret ret = XZUtils.CreateEncoder(_lzma_stream, _threads, _preset);
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

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// learn from 01_compress_easy.c
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count + offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count), "offset+count > buffer.length");
            if (count == 0) return;

            int bytesProcessed = 0;
            while (true)
            {
                // Fill the input buffer if it is empty.
                if (_lzma_stream.avail_in == UIntPtr.Zero)
                {
                    int bytesToProcess = Math.Min(count - bytesProcessed, BUFSIZE);
                    if (bytesToProcess == 0) break; // no more data to compress
                    _lzma_stream.next_in = _inbuf;
                    _lzma_stream.avail_in = (UIntPtr)bytesToProcess;
                    Marshal.Copy(buffer, offset + bytesProcessed, _inbuf, bytesToProcess);
                    bytesProcessed += bytesToProcess;
                }

                // do compress, RUN action should return LZMA_OK on success
                var ret = Native.lzma_code(_lzma_stream, lzma_action.LZMA_RUN);
                if (ret != lzma_ret.LZMA_OK)
                    throw new Exception($"lzma_code returns {ret}");

                // check output buffer
                if (_lzma_stream.avail_out == UIntPtr.Zero)
                {
                    byte[] data = new byte[BUFSIZE];
                    Marshal.Copy(_outbuf, data, 0, data.Length);
                    _stream.Write(data, 0, data.Length);

                    // Reset next_out and avail_out.
                    _lzma_stream.next_out = _outbuf;
                    _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // compress all remaining data
                while (true)
                {
                    // do compress, LZMA_FINISH action should return LZMA_OK or LZMA_STREAM_END on success
                    var ret = Native.lzma_code(_lzma_stream, lzma_action.LZMA_FINISH);
                    if (ret != lzma_ret.LZMA_STREAM_END && ret != lzma_ret.LZMA_OK)
                        throw new Exception($"lzma_code returns {ret}");

                    // write output buffer to underlying stream
                    if (_lzma_stream.avail_out == UIntPtr.Zero || ret == lzma_ret.LZMA_STREAM_END)
                    {
                        byte[] data = new byte[BUFSIZE - (uint)_lzma_stream.avail_out];
                        Marshal.Copy(_outbuf, data, 0, data.Length);
                        _stream.Write(data, 0, data.Length);

                        // Reset next_out and avail_out.
                        _lzma_stream.next_out = _outbuf;
                        _lzma_stream.avail_out = (UIntPtr)BUFSIZE;
                    }

                    if (ret == lzma_ret.LZMA_STREAM_END)
                        break;
                }
            }
            finally
            {
                Native.lzma_end(_lzma_stream);
                Marshal.FreeHGlobal(_inbuf);
                Marshal.FreeHGlobal(_outbuf);
                _stream.Close();
            }
        }

        public override void Flush()
        {
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