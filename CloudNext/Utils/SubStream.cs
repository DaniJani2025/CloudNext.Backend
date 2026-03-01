namespace CloudNext.Utils
{
    public class SubStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _length;
        private long _position;

        public SubStream(Stream baseStream, long length)
        {
            _baseStream = baseStream;
            _length = length;
            _position = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            var remaining = _length - _position;
            var toRead = (int)Math.Min(count, remaining);

            var read = _baseStream.Read(buffer, offset, toRead);
            _position += read;

            return read;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
