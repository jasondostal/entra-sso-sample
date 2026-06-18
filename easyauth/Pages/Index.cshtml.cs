using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntraSsoSample.Pages;

// Reads the identity that App Service EasyAuth injects as request headers.
// No authentication code, no NuGet packages — the platform did the sign-in.
public class IndexModel : PageModel
{
    public string? Name { get; private set; }
    public string? Idp { get; private set; }
    public string? PrincipalId { get; private set; }
    public List<ClaimRow> Claims { get; } = new();

    public bool HasPrincipal => !string.IsNullOrEmpty(Name) || Claims.Count > 0;

    public record ClaimRow(string Type, string Value);

    public void OnGet()
    {
        // Convenience headers EasyAuth sets directly.
        Name = Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault();
        Idp = Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].FirstOrDefault();
        PrincipalId = Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault();

        // The full principal is a base64-encoded JSON blob of all claims.
        var encoded = Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
        if (string.IsNullOrEmpty(encoded)) return;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("claims", out var claims))
            {
                foreach (var c in claims.EnumerateArray())
                {
                    Claims.Add(new ClaimRow(
                        c.GetProperty("typ").GetString() ?? "",
                        c.GetProperty("val").GetString() ?? ""));
                }
            }
        }
        catch
        {
            // Header absent/malformed (e.g. running locally without EasyAuth) — ignore.
        }
    }
}
