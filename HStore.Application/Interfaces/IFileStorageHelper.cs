namespace HStore.Application.Interfaces;

/// <summary>
/// A helper for uploading files to storage.
/// </summary>
public interface IFileStorageHelper
{
    /// <summary>
    /// Uploads a file to storage. The file is automatically placed in the appropriate
    /// type folder (Images, Videos, Audio, Docs, or Other) based on its extension.
    /// </summary>
    /// <param name="fileStream">The stream containing the file data.</param>
    /// <param name="fileName">The original name of the file.</param>
    /// <returns>
    /// The full public URL of the uploaded file.
    /// </returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the file to delete.</param>
    /// <returns>True if the file was deleted; false otherwise.</returns>
    bool DeleteFile(string relativePath);

    /// <summary>
    /// Gets the absolute physical path for a relative file path.
    /// </summary>
    /// <param name="relativePath">The relative path of the file.</param>
    /// <returns>The absolute physical path.</returns>
    string GetPhysicalPath(string relativePath);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the file.</param>
    /// <returns>True if the file exists; false otherwise.</returns>
    bool FileExists(string relativePath);
}

