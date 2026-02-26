namespace Platform.Application.Features.Categories.Constants;

public static class CategoryBusinessMessages
{
    public const string NotFound = "Category not found.";
    public const string NotFoundById = "Category not found with the specified ID.";
    public const string NotFoundBySlug = "Category not found with the specified slug.";
    public const string SlugExists = "A category with this slug already exists.";
    public const string ParentNotFound = "Parent category not found.";
    public const string CannotDeleteWithChildren = "Cannot delete category with child categories.";
    public const string CannotDeleteWithCourses = "Cannot delete category with associated courses.";
}
