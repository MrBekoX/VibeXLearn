using MediatR;
using Platform.Application.Common.Results;

namespace Platform.Application.Features.Payments.Commands.ProcessCallback;

/// <summary>
/// Command to process payment callback from Iyzico.
/// </summary>
public sealed record ProcessCallbackCommand(
    string Token,
    string ConversationId,
    string RawBody) : IRequest<Result>;
