using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Payment;
using MediatR;

namespace Application.Payments.Commands.CreatePaymentIntent;

public record CreatePaymentIntentCommand(string OrderNumber) : IRequest<Envelope<PaymentConfirmationResult>>;