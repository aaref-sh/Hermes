using System.Security.Cryptography;
using System.Text;
using HStore.API.Attributes;
using HStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace HStore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IFileStorageHelper fileStorageHelper) : ControllerBaseEx
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// <summary>
    /// Uploads a file to local storage.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="folder">The folder name to store the file in (default: "uploads").</param>
    /// <returns>The relative path of the uploaded file.</returns>
    [HttpPost("upload")]
    [AuthorizeMiddleware(["Admin", "Seller"])]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var url = await fileStorageHelper.UploadFileAsync(file.OpenReadStream(), file.FileName);
        return Ok(new { Url = url, FileName = file.FileName });
    }

    /// <summary>
    /// Serves a file from local storage with optimal performance for media streaming.
    /// Supports range requests, ETags, Last-Modified, and caching headers.
    /// </summary>
    /// <param name="filePath">The relative file path (e.g., "products-images/guid_filename.jpg").</param>
    /// <returns>The file content with appropriate headers for caching and streaming.</returns>
    [HttpGet("{*filePath}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult GetFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path is required.");

        var physicalPath = fileStorageHelper.GetPhysicalPath(filePath);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var fileInfo = new FileInfo(physicalPath);
        var lastModified = fileInfo.LastWriteTimeUtc;
        var etag = GenerateETag(filePath, lastModified, fileInfo.Length);

        // Check If-None-Match (ETag)
        var requestETag = Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(requestETag) && requestETag == etag.ToString())
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // Check If-Modified-Since
        if (Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out var ifModifiedSinceValue) &&
            DateTimeOffset.TryParse(ifModifiedSinceValue, out var ifModifiedSince) &&
            ifModifiedSince >= lastModified.AddSeconds(-1))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // Determine content type
        if (!ContentTypeProvider.TryGetContentType(physicalPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var entityTag = new EntityTagHeaderValue(etag, false);

        // For media files, enable range processing for optimal streaming
        var enableRangeProcessing = IsMediaFile(contentType);

        var result = new PhysicalFileResult(physicalPath, contentType)
        {
            EnableRangeProcessing = enableRangeProcessing,
            FileDownloadName = Path.GetFileName(physicalPath),
            LastModified = lastModified,
            EntityTag = entityTag
        };

        Response.Headers.Append(HeaderNames.CacheControl, "public, max-age=31536000, immutable");
        Response.Headers.Append(HeaderNames.Vary, "Accept-Encoding");

        return result;
    }

    /// <summary>
    /// Deletes a file from local storage.
    /// </summary>
    /// <param name="filePath">The relative file path to delete.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{*filePath}")]
    [AuthorizeMiddleware(["Admin"])]
    public IActionResult DeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path is required.");

        var deleted = fileStorageHelper.DeleteFile(filePath);
        return deleted ? NoContent() : NotFound();
    }

    private static string GenerateETag(string filePath, DateTime lastModified, long fileLength)
    {
        var input = $"{filePath}:{lastModified:o}:{fileLength}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }

    private static bool IsMediaFile(string contentType)
    {
        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }
}

