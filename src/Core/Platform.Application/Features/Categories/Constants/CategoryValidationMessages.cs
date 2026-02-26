namespace Platform.Application.Features.Categories.Constants;

public static class CategoryValidationMessages
{
    public const string CategoryIdRequired = "Category ID is required.";
    public const string NameRequired = "Category name is required.";
    public const string NameMaxLength = "Category name must not exceed 100 characters.";
    public const string SlugRequired = "Category slug is required.";
    public const string SlugInvalidFormat = "Slug must contain only lowercase letters, numbers, and hyphens.";
    public const string SlugMaxLength = "Slug cannot exceed 150 characters.";
    public const string DescriptionMaxLength = "Description cannot exceed 500 characters.";
}
