using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderProcessing.Data
{
    public sealed class JsonFileStore
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        FileLocks = new(StringComparer.OrdinalIgnoreCase);

        private readonly string _dataDirectory;

        public JsonFileStore()
            : this(Path.Combine(AppContext.BaseDirectory, "Data"))
        {

        }
        public JsonFileStore(string dataDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

            _dataDirectory = Path.GetFullPath(dataDirectory);
            Directory.CreateDirectory(_dataDirectory);
        }

        private string GetFilePath(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            if (Path.GetFileName(fileName) != fileName)
            {
                throw new ArgumentException("Only filenames are allowed.", nameof(fileName));
            }

            return Path.Combine(_dataDirectory, fileName);
        }

        private static SemaphoreSlim GetFileLock(string path)
        {
            return FileLocks.GetOrAdd(
                Path.GetFullPath(path),
                static _ => new SemaphoreSlim(1, 1));
        }

        // ---------------- Async operations ----------------

        public async Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct = default)
        {
            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            await fileLock.WaitAsync(ct);

            try
            {
                return await ReadUnlockedAsync<T>(path, fileName, ct);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public async Task WriteAllAsync<T>(string fileName, IEnumerable<T> records,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(records);

            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            await fileLock.WaitAsync(ct);

            try
            {
                await WriteUnlockedAsync(path, records, ct);
            }
            finally
            {
                fileLock.Release();
            }
        }

        // Performs the complete read-modify-write sequence under one lock.
        public async Task<TResult> UpdateAsync<T, TResult>(string fileName, Func<List<T>, TResult> update,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(update);

            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            await fileLock.WaitAsync(ct);

            try
            {
                List<T> records = await ReadUnlockedAsync<T>(path, fileName, ct);

                ct.ThrowIfCancellationRequested();

                TResult result = update(records);

                ct.ThrowIfCancellationRequested();

                await WriteUnlockedAsync(path, records, ct);

                return result;
            }
            finally
            {
                fileLock.Release();
            }
        }

        // ---------------- Sync operations ----------------

        public List<T> ReadAll<T>(string fileName)
        {
            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            fileLock.Wait();

            try
            {
                return ReadUnlocked<T>(path, fileName);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public void WriteAll<T>(string fileName, IEnumerable<T> records)
        {
            ArgumentNullException.ThrowIfNull(records);

            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            fileLock.Wait();

            try
            {
                WriteUnlocked(path, records);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public TResult Update<T, TResult>(string fileName, Func<List<T>, TResult> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            string path = GetFilePath(fileName);
            SemaphoreSlim fileLock = GetFileLock(path);

            fileLock.Wait();

            try
            {
                List<T> records = ReadUnlocked<T>(path, fileName);
                TResult result = update(records);

                WriteUnlocked(path, records);

                return result;
            }
            finally
            {
                fileLock.Release();
            }
        }

        // ---------------- Read Helpers ----------------

        private static async Task<List<T>> ReadUnlockedAsync<T>(string path, string fileName,
            CancellationToken ct = default)
        {
            if (!File.Exists(path))
            {
                return [];
            }

            try
            {
                byte[] bytes = await File.ReadAllBytesAsync(path, ct);

                return JsonSerializer.Deserialize<List<T>>(bytes, JsonOptions) ?? [];
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }

        private static List<T> ReadUnlocked<T>(string path, string fileName)
        {
            if (!File.Exists(path))
            {
                return [];
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);

                return JsonSerializer.Deserialize<List<T>>(bytes, JsonOptions) ?? [];
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }

        // ---------------- Write Helpers ----------------

        private static async Task WriteUnlockedAsync<T>(string path, IEnumerable<T> records,
            CancellationToken ct = default)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(records, JsonOptions);

            await WriteAtomicallyAsync(path, json, ct);
        }

        private static void WriteUnlocked<T>(string path, IEnumerable<T> records)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(records, JsonOptions);

            WriteAtomically(path, json);
        }

        private static async Task WriteAtomicallyAsync(string path, byte[] content,
            CancellationToken ct = default)
        {
            string directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);

            string tempPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(content, ct);
                    await stream.FlushAsync(ct);

                    // Request a flush through the OS to the storage device.
                    stream.Flush(flushToDisk: true);
                }

                ct.ThrowIfCancellationRequested();

                // Same-directory rename/replacement avoids exposing a
                // partially written destination file.
                File.Move(tempPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static void WriteAtomically(string path, byte[] content)
        {
            string directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);

            string tempPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    stream.Write(content);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(tempPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
