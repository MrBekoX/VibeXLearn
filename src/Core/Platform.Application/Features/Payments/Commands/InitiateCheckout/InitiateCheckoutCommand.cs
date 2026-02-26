using MediatR;
using Platform.Application.Common.Results;
using Platform.Application.Features.Payments.DTOs;

namespace Platform.Application.Features.Payments.Commands.InitiateCheckout;

/// <summary>
/// Command to initiate checkout for a course.
/// </summary>
public sealed record InitiateCheckoutCommand(
    Guid UserId,
    Guid CourseId,
    string? CouponCode = null) : IRequest<Result<CheckoutResponseDto>>;
