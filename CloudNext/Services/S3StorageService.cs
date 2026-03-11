using Amazon.S3;
using Amazon.S3.Model;
using CloudNext.Interfaces;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3StorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["AWS:Bucket"]!;
    }

    public async Task SaveAsync(Stream stream, string key)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3.PutObjectAsync(request);
    }

    public async Task<Stream> GetAsync(string key)
    {
        var response = await _s3.GetObjectAsync(_bucket, key);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucket, key);
    }
}