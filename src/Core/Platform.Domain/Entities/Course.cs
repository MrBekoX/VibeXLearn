using Platform.Domain.Common;
using Platform.Domain.Enums;
using Platform.Domain.Events;
using Platform.Domain.ValueObjects;

namespace Platform.Domain.Entities;

/// <summary>
/// Course aggregate root with rich domain behavior.
/// </summary>
public class Course : AuditableEntity, IAggregateRoot
{
    // Private setters for encapsulation
    public string       Title           { get; private set; } = default!;
    public string       Slug            { get; private set; } = default!;
    public string?      Description     { get; private set; }
    public string?      ThumbnailUrl    { get; private set; }
    public decimal      Price           { get; private set; }
    public CourseLevel  Level           { get; private set; }
    public CourseStatus Status          { get; private set; } = CourseStatus.Draft;
    public Guid         InstructorId    { get; private set; }
    public Guid         CategoryId      { get; private set; }
    public int          EnrollmentCount { get; private set; } = 0;

    // Navigation properties
    public AppUser                 Instructor  { get; private set; } = default!;
    public Category                Category    { get; private set; } = default!;
    public ICollection<Lesson>     Lessons     { get; private set; } = [];
    public ICollection<Enrollment> Enrollments { get; private set; } = [];
    public ICollection<Order>      Orders      { get; private set; } = [];

    // Private constructor for EF Core
    private Course() { }

    /// <summary>
    /// Factory method to create a new course.
    /// </summary>
    public static Course Create(
        string title,
        string slug,
        decimal price,
        CourseLevel level,
        Guid instructorId,
        Guid categoryId,
        string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(slug, nameof(slug));
        Guard.Against.NegativeOrZero(price, nameof(price));
        Guard.Against.EmptyGuid(instructorId, nameof(instructorId));
        Guard.Against.EmptyGuid(categoryId, nameof(categoryId));

        return new Course
        {
            Title = title.Trim(),
            Slug = Platform.Domain.ValueObjects.Slug.From(slug).Value,
            Price = price,
            Level = level,
            InstructorId = instructorId,
            CategoryId = categoryId,
            Description = description?.Trim(),
            Status = CourseStatus.Draft
        };
    }

    /// <summary>
    /// Update course title.
    /// </summary>
    public void UpdateTitle(string title)
    {
        Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)).Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update course description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update course price. Raises domain event if published.
    /// </summary>
    public void UpdatePrice(decimal newPrice)
    {
        Guard.Against.NegativeOrZero(newPrice, nameof(newPrice));

        if (newPrice == Price)
            return;

        var oldPrice = Price;
        Price = newPrice;
        MarkAsUpdated();

        if (Status == CourseStatus.Published)
        {
            AddDomainEvent(new CoursePriceChangedEvent(Id, oldPrice, newPrice));
        }
    }

    /// <summary>
    /// Update course thumbnail.
    /// </summary>
    public void UpdateThumbnail(string? thumbnailUrl)
    {
        ThumbnailUrl = thumbnailUrl;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update course category.
    /// </summary>
    public void UpdateCategory(Guid categoryId)
    {
        Guard.Against.EmptyGuid(categoryId, nameof(categoryId));
        CategoryId = categoryId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update course level.
    /// </summary>
    public void UpdateLevel(CourseLevel level)
    {
        Level = level;
        MarkAsUpdated();
    }

    /// <summary>
    /// Publish the course. Only draft courses can be published.
    /// </summary>
    public void Publish()
    {
        if (Status != CourseStatus.Draft)
            throw new DomainException("COURSE_PUBLISH_INVALID_STATUS",
                "Only draft courses can be published.");

        Status = CourseStatus.Published;
        MarkAsUpdated();
        AddDomainEvent(new CoursePublishedEvent(Id));
    }

    /// <summary>
    /// Archive the course. Only published courses can be archived.
    /// </summary>
    public void Archive()
    {
        if (Status != CourseStatus.Published)
            throw new DomainException("COURSE_ARCHIVE_INVALID_STATUS",
                "Only published courses can be archived.");

        Status = CourseStatus.Archived;
        MarkAsUpdated();
        AddDomainEvent(new CourseArchivedEvent(Id));
    }

    /// <summary>
    /// Restore archived course to draft.
    /// </summary>
    public void RestoreToDraft()
    {
        if (Status != CourseStatus.Archived)
            throw new DomainException("COURSE_RESTORE_INVALID_STATUS",
                "Only archived courses can be restored to draft.");

        Status = CourseStatus.Draft;
        MarkAsUpdated();
    }

    /// <summary>
    /// Increment enrollment count (called when student enrolls).
    /// </summary>
    public void IncrementEnrollment()
    {
        EnrollmentCount++;
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if course can be purchased.
    /// </summary>
    public bool CanBePurchased => Status == CourseStatus.Published && !IsDeleted;

    /// <summary>
    /// Check if course is published.
    /// </summary>
    public bool IsPublished => Status == CourseStatus.Published;
}
