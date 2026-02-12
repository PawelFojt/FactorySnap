using Microsoft.AspNetCore.SignalR;

namespace FactorySnap.Api.Hubs;

public class LiveDataHub : Hub
{
  public override async Task OnConnectedAsync()
  {
    Console.WriteLine($"[SignalR] Klient połączony: {Context.ConnectionId}");
    await base.OnConnectedAsync();
  }
}
