using System.IO;
using System.Reflection;
using System.Text.Json;

namespace IndustrialAutomationStudio.Modules.Motion.Repositories.Json;

internal sealed class JsonFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                   .ConfigureAwait(false)
               ?? throw new JsonException($"配置文件 '{path}' 内容为空。");
    }

    public async Task<T> ReadEmbeddedAsync<T>(
        string resourceName,
        CancellationToken cancellationToken)
    {
        await using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"找不到嵌入配置资源 '{resourceName}'。");
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                   .ConfigureAwait(false)
               ?? throw new JsonException($"嵌入配置资源 '{resourceName}' 内容为空。");
    }

    public async Task WriteAtomicAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"无法确定配置文件 '{path}' 的目录。");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                        stream,
                        value,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            _ = await ReadAsync<T>(temporaryPath, cancellationToken).ConfigureAwait(false);

            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
