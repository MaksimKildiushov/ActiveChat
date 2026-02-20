using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Interfaces;

public interface IStepHandler
{
    StepKind StepKind { get; }
    Task<ReplyIntent> HandleAsync(StepContext context, CancellationToken ct = default);
}
