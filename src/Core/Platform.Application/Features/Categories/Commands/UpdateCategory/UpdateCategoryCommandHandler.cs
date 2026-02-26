using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Categories.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    IReadRepository<Category> readRepo,
    IWriteRepository<Category> writeRepo,
    IUnitOfWork uow) : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await readRepo.GetByIdAsync(request.CategoryId, ct, tracking: true);

        if (category is null)
            return Result.Fail("CATEGORY_NOT_FOUND", CategoryBusinessMessages.NotFoundById);

        if (request.Name is not null)
            category.UpdateName(request.Name);

        if (request.Description is not null)
            category.UpdateDescription(request.Description);

        await writeRepo.UpdateAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}
