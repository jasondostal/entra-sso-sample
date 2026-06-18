using Microsoft.AspNetCore.Authorization;

namespace EntraSsoSample.InApp.Authorization;

// A resource-based requirement carries no data of its own — the decision depends on
// the *resource* (a Document) handed in at check time. This is what lets you answer
// "can this user edit THIS specific row", which a role/scope claim cannot express.
public sealed class SameOwnerOrAdminRequirement : IAuthorizationRequirement;
