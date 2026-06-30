using HRDocs.Client;
using HRDocs.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app"); builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000/") });
builder.Services.AddAuthorizationCore(); builder.Services.AddScoped<TokenStore>(); builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>(); builder.Services.AddScoped<ApiClient>(); builder.Services.AddScoped<NotificationService>(); builder.Services.AddScoped<DialogService>(); builder.Services.AddScoped<TooltipService>(); builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<NotificationConnection>();
await builder.Build().RunAsync();
