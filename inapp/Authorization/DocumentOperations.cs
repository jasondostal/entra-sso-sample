using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace EntraSsoSample.InApp.Authorization;

// Resource operations checked against a specific Document. Using the framework's
// OperationAuthorizationRequirement lets ONE handler cover several operations, so
// "who can Read" and "who can Edit" live side by side instead of in two requirements.
public static class DocumentOperations
{
    public static readonly OperationAuthorizationRequirement Read = new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Edit = new() { Name = nameof(Edit) };
}
