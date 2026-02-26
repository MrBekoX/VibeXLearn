using Asp.Versioning;
using MediatR;
using Platform.Application.Features.Certificates.Commands.CreatePendingCertificate;
using Platform.Application.Features.Certificates.Commands.MarkCertificateAsIssued;
using Platform.Application.Features.Certificates.Commands.RevokeCertificate;
using Platform.Application.Features.Certificates.Queries.GetByCourseCertificate;
using Platform.Application.Features.Certificates.Queries.GetByIdCertificate;
using Platform.Application.Features.Certificates.Queries.GetByUserCertificate;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Certificate endpoints.
/// </summary>
public static class CertificateEndpoints
{
    public static IEndpointRouteBuilder RegisterCertificateEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Certificates");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/certificates")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Certificates")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdCertificateQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCertificateById");

        group.MapGet("/by-user/{userId:guid}", async (Guid userId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByUserCertificateQuery(userId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetCertificatesByUser");

        group.MapGet("/by-course/{courseId:guid}", async (Guid courseId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByCourseCertificateQuery(courseId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetCertificatesByCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPost("/", async (CreatePendingCertificateRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreatePendingCertificateCommand(dto.UserId, dto.CourseId), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/certificates/{result.Value}", new { CertificateId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreatePendingCertificate")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}/issue", async (Guid id, IssueCertificateRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MarkCertificateAsIssuedCommand(
                id, dto.SertifierCertId, dto.PublicUrl), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("IssueCertificate")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}/revoke", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RevokeCertificateCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("RevokeCertificate")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}

file record CreatePendingCertificateRequest(Guid UserId, Guid CourseId);
file record IssueCertificateRequest(string SertifierCertId, string PublicUrl);
