using Microsoft.AspNetCore.Authorization;

namespace EntraSsoSample.Api.Authorization;

// Resource-based: decided against a specific Document at runtime (owner, or elevated).
public sealed class SameOwnerOrElevatedRequirement : IAuthorizationRequirement;
