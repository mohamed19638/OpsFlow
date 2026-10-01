using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Helpers;

public static class IncidentStateMachine
{
    private static readonly IReadOnlyDictionary<IncidentStatus, HashSet<IncidentStatus>> Transitions =
        new Dictionary<IncidentStatus, HashSet<IncidentStatus>>
        {
            [IncidentStatus.Detected] = [IncidentStatus.Investigating],
            [IncidentStatus.Investigating] = [IncidentStatus.Identified, IncidentStatus.Mitigating],
            [IncidentStatus.Identified] = [IncidentStatus.Mitigating],
            // Resolution goes through the dedicated Resolve action so that ResolvedAt,
            // resolution notes, timeline, and audit data are recorded together.
            [IncidentStatus.Mitigating] = [],
            [IncidentStatus.Resolved] = [IncidentStatus.Postmortem, IncidentStatus.Closed],
            [IncidentStatus.Postmortem] = [IncidentStatus.Closed],
            [IncidentStatus.Closed] = []
        };

    public static bool CanTransition(IncidentStatus from, IncidentStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static HashSet<IncidentStatus> GetValidTransitions(IncidentStatus from) =>
        Transitions.TryGetValue(from, out var allowed)
            ? [.. allowed]
            : [];
}
