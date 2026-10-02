namespace OpsFlow.Application.Constants;

public static class CacheKeys
{
    public static string ServicesByOrganization(Guid organizationId) =>
        $"services:org:{organizationId}";
}
