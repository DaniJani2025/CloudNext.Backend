using CloudNext.Interfaces;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService()
    {
        _basePath = Path.Combine(AppContext.BaseDirectory, "Documents");
    }

    public async Task SaveAsync(Stream stream, string key)
    {
        var fullPath = Path.Combine(_basePath, key);
        var directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await stream.CopyToAsync(fileStream);
    }

    public Task<Stream> GetAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}