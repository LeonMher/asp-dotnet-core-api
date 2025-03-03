using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;
using YourProject.Hubs;

namespace YourProject.Services
{
    public class BookingCompletionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BookingCompletionService> _logger;
        private readonly IHubContext<BookingHub> _hubContext;

        public BookingCompletionService(IServiceProvider services, ILogger<BookingCompletionService> logger, IHubContext<BookingHub> hubContext)
        {
            _services = services;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Completion Service is running.");

            var yerevanTimeZone = TZConvert.GetTimeZoneInfo("Asia/Yerevan");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Get current time in Yerevan
                        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, yerevanTimeZone);

                        // Get all active bookings
                        var activeBookings = await dbContext.Bookings
                            .Include(b => b.MusicRoom)
                            .Where(b => b.State == BookingState.Booked)
                            .ToListAsync(stoppingToken);

                        foreach (var booking in activeBookings)
                        {
                            // Calculate remaining time
                            var timeRemaining = booking.EndTime - now;

                            if (timeRemaining.TotalSeconds > 0)
                            {
                                // Send real-time update to clients
                                await _hubContext.Clients.All.SendAsync("ReceiveCountdown", booking.MusicRoomId.ToString(), timeRemaining.ToString(@"hh\:mm\:ss"));
                            }
                            else
                            {
                                // Mark booking as completed
                                booking.State = BookingState.Completed;
                                _logger.LogInformation($"Booking {booking.Id} marked as completed.");
                            }
                        }

                        // Save changes to the database
                        if (activeBookings.Any(b => b.State == BookingState.Completed))
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Changes saved to the database.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating booking states.");
                }

                // Wait for a certain interval before running again (e.g., every 1 second for real-time updates)
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}