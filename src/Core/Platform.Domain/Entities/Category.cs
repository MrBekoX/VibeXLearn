using Platform.Domain.Common;
using Platform.Domain.ValueObjects;

namespace Platform.Domain.Entities;

/// <summary>
/// Category with hierarchy management.
/// </summary>
public class Category : AuditableEntity
{
    // Private setters for encapsulation
    public string    Name        { get; private set; } = default!;
    public string    Slug        { get; private set; } = default!;
    public string?   Description { get; private set; }
    public Guid?     ParentId    { get; private set; }

    // Computed properties
    public bool      IsRoot      => !ParentId.HasValue;
    public int       Depth       => ParentId.HasValue ? 1 : 0; // Simplified, could be calculated recursively

    // Navigation properties
    public Category?              Parent   { get; private set; }
    public ICollection<Category>  Children { get; private set; } = [];
    public ICollection<Course>    Courses  { get; private set; } = [];

    // Private constructor for EF Core
    private Category() { }

    /// <summary>
    /// Factory method to create a new root category.
    /// </summary>
    public static Category Create(string name, string slug, string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(slug, nameof(slug));

        return new Category
        {
            Name = name.Trim(),
            Slug = ValueObjects.Slug.From(slug).Value,
            Description = description?.Trim()
        };
    }

    /// <summary>
    /// Factory method to create a subcategory.
    /// </summary>
    public static Category CreateChild(string name, string slug, Guid parentId, string? description = null)
    {
        Guard.Against.EmptyGuid(parentId, nameof(parentId));

        var category = Create(name, slug, description);
        category.ParentId = parentId;
        return category;
    }

    /// <summary>
    /// Update category name.
    /// </summary>
    public void UpdateName(string name)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)).Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Update category slug.
    /// </summary>
    public void UpdateSlug(string slug)
    {
        Slug = ValueObjects.Slug.From(slug).Value;
        MarkAsUpdated();
    }

    /// <summary>
    /// Update category description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Move to a new parent category.
    /// </summary>
    public void MoveToParent(Guid? newParentId)
    {
        if (newParentId == Id)
            throw new DomainException("CATEGORY_SELF_PARENT",
                "Category cannot be its own parent.");

        // TODO: Add cycle detection for multi-level hierarchies

        ParentId = newParentId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Make this a root category.
    /// </summary>
    public void MakeRoot()
    {
        ParentId = null;
        MarkAsUpdated();
    }

    /// <summary>
    /// Check if category can be deleted (no courses, no children).
    /// </summary>
    public bool CanBeDeleted => (Courses == null || !Courses.Any()) &&
                                 (Children == null || !Children.Any());
}
