using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Categories.Constants;
using Platform.Application.Features.Categories.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    IReadRepository<Category> readRepo,
    IWriteRepository<Category> writeRepo,
    ICategoryBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var result = await ruleEngine.RunAsync(ct,
            rules.CategoryMustExist(request.CategoryId),
            rules.MustNotHaveChildren(request.CategoryId),
            rules.MustNotHaveCourses(request.CategoryId));

        if (result.IsFailure)
            return result;

        var category = await readRepo.GetByIdAsync(request.CategoryId, ct, tracking: true);

        if (category is null)
            return Result.Fail("CATEGORY_NOT_FOUND", CategoryBusinessMessages.NotFoundById);

        await writeRepo.SoftDeleteAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
