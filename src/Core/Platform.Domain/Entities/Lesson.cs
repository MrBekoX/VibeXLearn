using Platform.Domain.Common;
using Platform.Domain.Enums;

namespace Platform.Domain.Entities;

/// <summary>
/// Lesson with ordering and type-specific behavior.
/// </summary>
public class Lesson : AuditableEntity
{
    // Private setters for encapsulation
    public string     Title       { get; private set; } = default!;
    public string?    Description { get; private set; }
    public string?    VideoUrl    { get; private set; }
    public int        Order       { get; private set; }
    public LessonType Type        { get; private set; } = LessonType.Video;
    public bool       IsFree      { get; private set; } = false;
    public Guid       CourseId    { get; private set; }

    // Computed properties
    public bool       HasVideo    => !string.IsNullOrWhiteSpace(VideoUrl);
    public bool       RequiresLiveSession => Type == LessonType.Live;
    public bool       IsAssignment => Type == LessonType.Assignment;

    // Navigation properties
    public Course                  Course      { get; private set; } = default!;
    public ICollection<Submission> Submissions { get; private set; } = [];
    public LiveSession?            LiveSession { get; private set; }

    // Private constructor for EF Core
    private Lesson() { }

    /// <summary>
    /// Factory method to create a new lesson.
    /// </summary>
    public static Lesson Create(
        Guid courseId,
        string title,
        int order,
        LessonType type = LessonType.Video,
        string? description = null,
        bool isFree = false)
    {
        Guard.Against.EmptyGuid(courseId, nameof(courseId));
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.Negative(order, nameof(order));

        return new Lesson
        {
            CourseId = courseId,
            Title = title.Trim(),
            Order = order,
            Type = type,
            Description = description?.Trim(),
            IsFree = isFree
        };
    }

    /// <summary>
    /// Create a video lesson.
    /// </summary>
    public static Lesson CreateVideo(Guid courseId, string title, int order, string videoUrl, bool isFree = false)
    {
        var lesson = Create(courseId, title, order, LessonType.Video, isFree: isFree);
        lesson.VideoUrl = videoUrl?.Trim();
        return lesson;
    }

    /// <summary>
    /// Create a live lesson.
    /// </summary>
    public static Lesson CreateLive(Guid courseId, string title, int order)
        => Create(courseId, title, order, LessonType.Live);

    /// <summary>
    /// Create an assignment lesson.
    /// </summary>
    public static Lesson CreateAssignment(Guid courseId, string title, int order, string? description = null)
        => Create(courseId, title, order, LessonType.Assignment, description);

    /// <summary>
    /// Update lesson title.
    /// </summary>
    public void UpdateTitle(string title)
    {
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)).Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update lesson description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update video URL (only for video lessons).
    /// </summary>
    public void UpdateVideoUrl(string? videoUrl)
    {
        if (Type != LessonType.Video)
            throw new DomainException("LESSON_VIDEO_INVALID_TYPE",
                "Video URL can only be set for video lessons.");

        VideoUrl = videoUrl?.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update lesson order.
    /// </summary>
    public void UpdateOrder(int order)
    {
        Order = (int)Guard.Against.Negative(order, nameof(order));
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark as free/preview.
    /// </summary>
    public void MarkAsFree()
    {
        IsFree = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Mark as paid (not free).
    /// </summary>
    public void MarkAsPaid()
    {
        IsFree = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Change lesson type.
    /// </summary>
    public void ChangeType(LessonType newType)
    {
        if (Type == newType)
            return;

        // Clear type-specific data when changing type
        if (newType != LessonType.Video)
            VideoUrl = null;

        Type = newType;
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if content is accessible to non-enrolled users.
    /// </summary>
    public bool IsPreviewable => IsFree && HasVideo;
}
