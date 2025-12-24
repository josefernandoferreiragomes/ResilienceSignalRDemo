using Microsoft.AspNetCore.SignalR;

namespace ConfigApi;

public class ConfigHub : Hub
{
    private readonly ErrorChanceStore _store;

    public ConfigHub(ErrorChanceStore store)
    {
        _store = store;
    }

    // Called by Dashboard client
    public async Task UpdateErrorChance(double newChance)
    {
        _store.SetChance(newChance);
        Console.WriteLine($"---------------------->[ConfigApi] [Inside ConfigHub] Error chance updated !!! to {newChance}");
        // Broadcast to all connected clients (ProducerApi, etc.)
        await Clients.All.SendAsync("BroadcastErrorChance", newChance);
    }

    public double GetErrorChance()
    {
        var chance = _store.GetChance();
        Console.WriteLine($"---------------------->[ConfigApi] [Inside ConfigHub] Get Error chance: {chance}");
        return chance;
    }
}