using Microsoft.AspNetCore.Mvc;

namespace Platform.WebAPI.Attributes;

/// <summary>
/// Common request size limit constants.
/// SKILL: request-size-limits
/// </summary>
public static class RequestSizeLimits
{
    /// <summary>100KB - for simple JSON requests</summary>
    public const int Small = 100 * 1024;

    /// <summary>1MB - for standard requests</summary>
    public const int Medium = 1024 * 1024;

    /// <summary>10MB - for rich content</summary>
    public const int Large = 10 * 1024 * 1024;

    /// <summary>100MB - for file uploads</summary>
    public const int FileUpload = 100 * 1024 * 1024;
}

/// <summary>
/// Applies small request size limit (100KB).
/// </summary>
public class SmallRequestLimitAttribute : RequestSizeLimitAttribute
{
    public SmallRequestLimitAttribute() : base(RequestSizeLimits.Small) { }
}

/// <summary>
/// Applies medium request size limit (1MB).
/// </summary>
public class MediumRequestLimitAttribute : RequestSizeLimitAttribute
{
    public MediumRequestLimitAttribute() : base(RequestSizeLimits.Medium) { }
}

/// <summary>
/// Applies large request size limit (10MB).
/// </summary>
public class LargeRequestLimitAttribute : RequestSizeLimitAttribute
{
    public LargeRequestLimitAttribute() : base(RequestSizeLimits.Large) { }
}

/// <summary>
/// Applies file upload request size limit (100MB).
/// </summary>
public class FileUploadRequestLimitAttribute : RequestSizeLimitAttribute
{
    public FileUploadRequestLimitAttribute() : base(RequestSizeLimits.FileUpload) { }
}
