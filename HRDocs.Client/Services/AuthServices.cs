using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using HRDocs.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace HRDocs.Client.Services;
public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "hrdocs_token";
    public ValueTask<string?> GetAsync() => js.InvokeAsync<string?>("localStorage.getItem", Key);
    public ValueTask SetAsync(string token) => js.InvokeVoidAsync("localStorage.setItem", Key, token);
    public ValueTask ClearAsync() => js.InvokeVoidAsync("localStorage.removeItem", Key);
}
public sealed class JwtAuthenticationStateProvider(TokenStore store) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await store.GetAsync(); if (string.IsNullOrWhiteSpace(token)) return Anonymous();
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/'); payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Convert.FromBase64String(payload))!;
            if (values.TryGetValue("exp", out var exp) && DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow) { await store.ClearAsync(); return Anonymous(); }
            var claims = new List<Claim>();
            foreach (var (key, value) in values)
            {
                var type = key switch { "unique_name" or "name" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => ClaimTypes.Name, "role" or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role, "sub" or "nameid" => ClaimTypes.NameIdentifier, "email" => ClaimTypes.Email, _ => key };
                if (value.ValueKind == JsonValueKind.Array) claims.AddRange(value.EnumerateArray().Select(x => new Claim(type, x.ToString()))); else claims.Add(new Claim(type, value.ToString()));
            }
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
        }
        catch { return Anonymous(); }
    }
    public void Changed() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    private static AuthenticationState Anonymous() => new(new ClaimsPrincipal(new ClaimsIdentity()));
}
public sealed class ApiClient(HttpClient http, TokenStore store)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public string BaseUrl => http.BaseAddress!.ToString();
    public string AbsoluteUrl(string path) => new Uri(http.BaseAddress!, path).ToString();
    public async Task<T?> GetAsync<T>(string url) { using var r = await SendAsync(HttpMethod.Get, url); await Ensure(r); return await r.Content.ReadFromJsonAsync<T>(Json); }
    public async Task<T?> PostAsync<T>(string url, object value) { using var r = await SendAsync(HttpMethod.Post, url, JsonContent.Create(value)); await Ensure(r); return await r.Content.ReadFromJsonAsync<T>(Json); }
    public async Task PutAsync(string url, object value) { using var r = await SendAsync(HttpMethod.Put, url, JsonContent.Create(value)); await Ensure(r); }
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null) { var req = new HttpRequestMessage(method, url) { Content = content }; var token = await store.GetAsync(); if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); return await http.SendAsync(req); }
    public async Task<string?> TokenAsync() => await store.GetAsync();
    private static async Task Ensure(HttpResponseMessage r) { if (r.IsSuccessStatusCode) return; var message = await r.Content.ReadAsStringAsync(); throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"Request failed ({(int)r.StatusCode})." : message.Trim('"')); }
}
