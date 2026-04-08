using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Payments.Commands.ConfirmPayment;

public record ConfirmPaymentCommand(string IntentId) : IRequest<Envelope>;