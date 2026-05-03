using Microsoft.AspNetCore.SignalR;

namespace settAPI.Hubs;

// Hub de SignalR para comunicación en tiempo real con el dashboard.
//
// Un Hub es una clase a la que los clientes WebSocket se conectan. Desde el
// servidor podemos llamar a "SendAsync(nombreEvento, payload)" y todos los
// clientes que estén conectados (los navegadores con el dashboard abierto)
// recibirán ese evento al instante.
//
// En este proyecto el Hub NO recibe llamadas del cliente — solo emite eventos
// hacia el frontend cuando los controladores notifican cambios:
//   - "SesionAbierta" / "SesionCerrada"  → desde WorkSessionsController
//   - "NuevaActividad"                   → desde AppActivityController
//   - "NuevoPeriodo"                     → desde ActivityPeriodsController
//
// Los métodos OnConnectedAsync / OnDisconnectedAsync se ejecutan
// automáticamente cuando un navegador (el dashboard del admin) se conecta
// o desconecta del hub. Emitimos "WorkerConnected" / "WorkerDisconnected"
// al resto de clientes — el nombre es un poco confuso porque aquí "Worker"
// se refiere al admin mirando el dashboard, no al agente del empleado.
// El frontend tiene esos handlers registrados pero vacíos hoy; están
// pensados por si en el futuro se quiere mostrar cuántos admins están
// viendo el dashboard en tiempo real.
// 
// Documentación por si aca: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs
public class MonitoringHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("WorkerConnected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("WorkerDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
