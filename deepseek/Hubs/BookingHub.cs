using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace YourProject.Hubs
{
    public class BookingHub : Hub
    {
        public async Task SendCountdown(string roomId, string timeRemaining)
        {
            await Clients.All.SendAsync("ReceiveCountdown", roomId, timeRemaining);
        }
    }
}