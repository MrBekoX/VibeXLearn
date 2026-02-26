using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Categories.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Rules;

public interface ICategoryBusinessRules
{
    IBusinessRule CategoryMustExist(Guid categoryId);
    IBusinessRule SlugMustBeUnique(string slug);
    IBusinessRule ParentMustExist(Guid? parentId);
    IBusinessRule MustNotHaveChildren(Guid categoryId);
    IBusinessRule MustNotHaveCourses(Guid categoryId);
}

public sealed class CategoryBusinessRules(IReadRepository<Category> repo) : ICategoryBusinessRules
{
    public IBusinessRule CategoryMustExist(Guid categoryId)
        => new BusinessRule("CATEGORY_NOT_FOUND", CategoryBusinessMessages.NotFoundById,
            async ct => await repo.AnyAsync(c => c.Id == categoryId, ct)
                ? Result.Success() : Result.Fail(CategoryBusinessMessages.NotFoundById));

    public IBusinessRule SlugMustBeUnique(string slug)
        => new BusinessRule("CATEGORY_SLUG_EXISTS", CategoryBusinessMessages.SlugExists,
            async ct => !await repo.AnyAsync(c => c.Slug == slug.ToLowerInvariant(), ct)
                ? Result.Success() : Result.Fail(CategoryBusinessMessages.SlugExists));

    public IBusinessRule ParentMustExist(Guid? parentId)
        => new BusinessRule("CATEGORY_PARENT_NOT_FOUND", CategoryBusinessMessages.ParentNotFound,
            async ct => !parentId.HasValue || await repo.AnyAsync(c => c.Id == parentId.Value, ct)
                ? Result.Success() : Result.Fail(CategoryBusinessMessages.ParentNotFound));

    public IBusinessRule MustNotHaveChildren(Guid categoryId)
        => new BusinessRule("CATEGORY_HAS_CHILDREN", CategoryBusinessMessages.CannotDeleteWithChildren,
            async ct => !await repo.AnyAsync(c => c.ParentId == categoryId, ct)
                ? Result.Success() : Result.Fail(CategoryBusinessMessages.CannotDeleteWithChildren));

    public IBusinessRule MustNotHaveCourses(Guid categoryId)
        => new BusinessRule("CATEGORY_HAS_COURSES", CategoryBusinessMessages.CannotDeleteWithCourses,
            async ct => !await repo.AnyAsync(c => c.Id == categoryId && c.Courses!.Any(), ct)
                ? Result.Success() : Result.Fail(CategoryBusinessMessages.CannotDeleteWithCourses));
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
