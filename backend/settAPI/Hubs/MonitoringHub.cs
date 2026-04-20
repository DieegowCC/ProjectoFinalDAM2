using Microsoft.AspNetCore.SignalR;

namespace settAPI.Hubs;

// Hub de SignalR que gestiona las conexiones en tiempo real con el dashboard https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs
public class MonitoringHub : Hub
{
    // Se ejecuta automáticamente cuando un cliente (el admin desde el dashboard) se conecta
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("WorkerConnected", Context.ConnectionId); // notifica a todos los clientes
        await base.OnConnectedAsync();
    }

    // Se ejecuta automáticamente cuando un cliente se desconecta
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("WorkerDisconnected", Context.ConnectionId); // notifica a todos los clientes
        await base.OnDisconnectedAsync(exception);
    }
}