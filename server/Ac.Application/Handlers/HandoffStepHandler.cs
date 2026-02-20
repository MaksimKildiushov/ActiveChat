using Ac.Application.Interfaces;
using Ac.Application.Models;
using Ac.Domain.Enums;
using Ac.Domain.ValueObjects;

namespace Ac.Application.Handlers;

public class HandoffStepHandler : IStepHandler
{
    public StepKind StepKind => StepKind.Handoff;

    public Task<ReplyIntent> HandleAsync(StepContext context, CancellationToken ct = default)
        => Task.FromResult<ReplyIntent>(new HandoffIntent("Передаю оператору, подождите."));
}
