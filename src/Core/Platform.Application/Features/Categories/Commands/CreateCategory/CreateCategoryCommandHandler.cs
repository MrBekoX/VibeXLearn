using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Categories.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IWriteRepository<Category> writeRepo,
    ICategoryBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow) : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var result = await ruleEngine.RunAsync(ct,
            rules.SlugMustBeUnique(request.Slug),
            rules.ParentMustExist(request.ParentId));

        if (result.IsFailure)
            return Result.Fail<Guid>(result.Error);

        var category = request.ParentId.HasValue
            ? Category.CreateChild(request.Name, request.Slug, request.ParentId.Value, request.Description)
            : Category.Create(request.Name, request.Slug, request.Description);
        await writeRepo.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(category.Id);
    }
}
