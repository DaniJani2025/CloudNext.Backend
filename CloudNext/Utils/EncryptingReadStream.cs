using System.Security.Cryptography;

namespace CloudNext.Utils
{
    public class EncryptingReadStream : Stream
    {
        private readonly CryptoStream _cryptoStream;
        private readonly MemoryStream _ivStream;
        private readonly Stream _inner;

        public EncryptingReadStream(Stream input, string hexKey)
        {
            var aes = Aes.Create();
            aes.Key = Convert.FromHexString(hexKey);
            aes.GenerateIV();

            _ivStream = new MemoryStream(aes.IV);

            _inner = new CryptoStream(
                input,
                aes.CreateEncryptor(),
                CryptoStreamMode.Read);

            _cryptoStream = null!;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_ivStream.Position < _ivStream.Length)
                return await _ivStream.ReadAsync(buffer, cancellationToken);

            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).Result;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}