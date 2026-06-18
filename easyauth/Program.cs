// EasyAuth sample — this app has NO authentication code.
//
// Sign-in is handled entirely by App Service "Authentication" (EasyAuth) running
// in front of the app. The platform does the Entra OpenID Connect dance, then
// forwards the request with the signed-in user injected as X-MS-CLIENT-PRINCIPAL-*
// headers. The app just reads them (see Pages/Index.cshtml.cs).
//
// The auth configuration lives in infra/main.bicep (authsettingsV2), not here.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
