namespace OpsFlow.Application.Constants;

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string TeamLead = nameof(TeamLead);
    public const string Engineer = nameof(Engineer);
    public const string Viewer = nameof(Viewer);

    public const string AdminOrTeamLead = $"{Admin},{TeamLead}";
    public const string AdminTeamLeadOrEngineer = $"{Admin},{TeamLead},{Engineer}";
}
