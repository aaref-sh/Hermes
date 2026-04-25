using HStore.Application.Interfaces;
using HStore.Domain.Settings;
using Microsoft.Extensions.Options;

namespace HStore.Infrastructure.Utilities;

public class LocalStorageHelper(IOptions<LocalStorageSettings> localStorageSettings) : IFileStorageHelper
{
    private readonly LocalStorageSettings _settings = localStorageSettings.Value;

    private static readonly Dictionary<string, string> PrefixToFolderMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["img"] = "Images",
        ["vid"] = "Videos",
        ["doc"] = "Docs",
        ["aud"] = "Audio",
        ["oth"] = "Other"
    };

    /// <summary>
    /// Uploads a file to local storage. Automatically categorizes into
    /// Images, Videos, Audio, Docs, or Other based on file extension.
    /// </summary>
    /// <returns>The full public URL of the uploaded file.</returns>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
        var prefix = GetPrefixFromExtension(extension);
        var folder = PrefixToFolderMap[prefix];

        var uniqueFileName = $"{prefix}_{Guid.NewGuid()}_{nameWithoutExt}";
        var folderPath = Path.Combine(_settings.BasePath, folder);
        var filePath = Path.Combine(folderPath, uniqueFileName);

        Directory.CreateDirectory(folderPath);

        await using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await fileStream.CopyToAsync(fileStreamOutput);

        return $"{_settings.PublicUrl.TrimEnd('/')}/{uniqueFileName}";
    }

    /// <summary>
    /// Deletes a file from local storage.
    /// </summary>
    /// <param name="fileName">The file name (e.g., img_guid_name.jpg).</param>
    public bool DeleteFile(string fileName)
    {
        var physicalPath = GetPhysicalPath(fileName);
        if (!File.Exists(physicalPath))
            return false;

        File.Delete(physicalPath);
        return true;
    }

    /// <summary>
    /// Gets the absolute physical path for a file name by resolving
    /// the type prefix to the correct subfolder.
    /// </summary>
    /// <param name="fileName">The file name (e.g., img_guid_name.jpg).</param>
    public string GetPhysicalPath(string fileName)
    {
        var sanitizedName = fileName.Replace("..", "").Replace("\\", "/").TrimStart('/');
        var prefix = sanitizedName.Split('_')[0];
        var folder = PrefixToFolderMap.TryGetValue(prefix, out var f) ? f : "Other";
        return Path.Combine(_settings.BasePath, folder, sanitizedName);
    }

    /// <summary>
    /// Checks if a file exists in local storage.
    /// </summary>
    /// <param name="fileName">The file name (e.g., img_guid_name.jpg).</param>
    public bool FileExists(string fileName)
    {
        var physicalPath = GetPhysicalPath(fileName);
        return File.Exists(physicalPath);
    }

    private static string GetPrefixFromExtension(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();

        return ext switch
        {
            "jpg" or "jpeg" or "png" or "gif" or "webp" or "bmp" or "svg" or "ico" or "tiff" or "tif" or "raw" or "heic" => "img",
            "mp4" or "avi" or "mkv" or "mov" or "wmv" or "flv" or "webm" or "m4v" or "mpg" or "mpeg" or "3gp" => "vid",
            "mp3" or "wav" or "ogg" or "flac" or "aac" or "wma" or "m4a" or "opus" or "aiff" => "aud",
            "pdf" or "doc" or "docx" or "xls" or "xlsx" or "ppt" or "pptx" or "txt" or "csv" or "json" or "xml" or "html" or "htm" or "md" or "zip" or "rar" or "7z" => "doc",
            _ => "oth"
        };
    }
}

