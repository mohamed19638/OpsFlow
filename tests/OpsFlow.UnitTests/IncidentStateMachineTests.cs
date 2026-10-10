using FluentAssertions;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Helpers;

namespace OpsFlow.UnitTests;

public class IncidentStateMachineTests
{
    [Theory]
    [InlineData(IncidentStatus.Detected, IncidentStatus.Investigating)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Identified)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Mitigating)]
    [InlineData(IncidentStatus.Identified, IncidentStatus.Mitigating)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Postmortem)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Closed)]
    [InlineData(IncidentStatus.Postmortem, IncidentStatus.Closed)]
    public void CanTransition_returns_true_for_allowed_transitions(IncidentStatus from, IncidentStatus to)
    {
        IncidentStateMachine.CanTransition(from, to).Should().BeTrue();
        IncidentStateMachine.GetValidTransitions(from).Should().Contain(to);
    }

    [Theory]
    [InlineData(IncidentStatus.Detected, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Detected, IncidentStatus.Closed)]
    [InlineData(IncidentStatus.Investigating, IncidentStatus.Detected)]
    [InlineData(IncidentStatus.Identified, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Mitigating, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Resolved, IncidentStatus.Investigating)]
    [InlineData(IncidentStatus.Postmortem, IncidentStatus.Resolved)]
    [InlineData(IncidentStatus.Closed, IncidentStatus.Investigating)]
    public void CanTransition_returns_false_for_forbidden_transitions(IncidentStatus from, IncidentStatus to)
    {
        IncidentStateMachine.CanTransition(from, to).Should().BeFalse();
        IncidentStateMachine.GetValidTransitions(from).Should().NotContain(to);
    }

    [Fact]
    public void GetValidTransitions_returns_a_copy_that_cannot_change_the_rules()
    {
        var transitions = IncidentStateMachine.GetValidTransitions(IncidentStatus.Detected);
        transitions.Clear();

        IncidentStateMachine.CanTransition(IncidentStatus.Detected, IncidentStatus.Investigating).Should().BeTrue();
    }
}
