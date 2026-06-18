using System.Security.Claims;
using EntraSsoSample.Api.Authorization;
using Microsoft.Identity.Web;

namespace EntraSsoSample.Api.Models;

// The unit a resource-based check runs against. OwnerObjectId is an Entra oid.
public sealed record Document(int Id, string Title, string OwnerObjectId, string OwnerName);

// Stand-in data store.
public static class DocumentStore
{
    private static readonly List<Document> _docs =
    [
        new(1, "Q1 board pack",   "11111111-1111-1111-1111-111111111111", "Alice Example"),
        new(2, "Risk register",   "22222222-2222-2222-2222-222222222222", "Bob Example"),
    ];

    public static Document? Find(int id) => _docs.FirstOrDefault(d => d.Id == id);
}

// Echoed back so you can SEE how the caller was authorized (delegated vs app-only).
public sealed record CallerInfo(string Kind, IEnumerable<string> Scopes, IEnumerable<string> AppRoles, string? ObjectId)
{
    public static CallerInfo Describe(ClaimsPrincipal user) => new(
        Kind: user.IsAppOnly() ? "app-only (daemon)" : "delegated (user)",
        Scopes: user.Scopes(),
        AppRoles: user.AppRoles(),
        ObjectId: user.GetObjectId());
}
