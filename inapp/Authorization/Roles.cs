namespace EntraSsoSample.InApp.Authorization;

// The app's role vocabulary — the ONE place to customize roles. To add or rename a
// role: change it here, mirror it in the app registration's appRoles
// (infra/create-app-registration.sh), then assign users or AD groups to it. The string
// values must match the appRole "value" in Entra exactly (that's what lands in the
// "roles" claim).
public static class Roles
{
    public const string Reader = "Reader";            // baseline read (the default signed-in user)
    public const string Contributor = "Contributor";  // create / edit your own items
    public const string Approver = "Approver";         // approve pending items
    public const string Auditor = "Auditor";           // read everything, including others' items
    public const string Admin = "Admin";               // full administrative access
}
