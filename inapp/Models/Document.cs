namespace EntraSsoSample.InApp.Models;

// A stand-in for "a thing that has an owner" — the unit a resource-based check runs
// against. OwnerObjectId is an Entra object id (oid), matched against the caller's.
public sealed record Document(int Id, string Title, string OwnerObjectId, string OwnerName);
