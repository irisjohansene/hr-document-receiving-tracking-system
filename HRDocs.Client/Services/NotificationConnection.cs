using HRDocs.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Radzen;

namespace HRDocs.Client.Services;
public sealed class NotificationConnection(NavigationManager nav, ApiClient api, NotificationService toast) : IAsyncDisposable
{
    private HubConnection? connection;
    public async Task StartAsync()
    {
        if (connection is not null) return;
        connection = new HubConnectionBuilder().WithUrl(new Uri(new Uri(nav.BaseUri), "hubs/notifications"), o => o.AccessTokenProvider = api.TokenAsync).WithAutomaticReconnect().Build();
        connection.On<NotificationDto>("Notification", n => toast.Notify(NotificationSeverity.Info, "Document update", n.Message, 6000));
        try { await connection.StartAsync(); } catch { /* API can reconnect after first render */ }
    }
    public async ValueTask DisposeAsync() { if (connection is not null) await connection.DisposeAsync(); }
}
