using Microsoft.AspNetCore.SignalR.Client;

namespace ProducerApi
{
    public class CustomHubAdapter(IConfiguration config, ErrorChanceStore errorChanceModel)
    {
        private HubConnection? hubConnection;
        public string GetConnectionStatus() => hubConnection?.State.ToString() ?? "Not initialized";
        public async Task StartHubConnectionAsync()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(config["ConfigApi:HubUrl"]!)
                .WithAutomaticReconnect()
                .Build();

            // Subscribe to broadcasts before connecting
            hubConnection.On<double>("BroadcastErrorChance", (newChance) =>
            {
                errorChanceModel.SetChance(newChance);
            });

            if (hubConnection is not null &&
                hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await hubConnection.StartAsync();
                    Console.WriteLine("---------------------->[ProducerApi] [ConfigApi] Hub connection started.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"---------------------->[ProducerApi] [ConfigApi] Error *** starting hub connection: {ex.Message}");
                }
            }
        }
    }
}
