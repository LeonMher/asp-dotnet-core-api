using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YourProject.Services
{
    public class BookingCompletionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BookingCompletionService> _logger;

        public BookingCompletionService(IServiceProvider services, ILogger<BookingCompletionService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Completion Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for completed bookings...");

                    using (var scope = _services.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Get all bookings that have ended but are still in the "Booked" state
                        var now = DateTime.UtcNow;
                        var completedBookings = await dbContext.Bookings
                            .Where(b => b.EndTime <= now && b.State == BookingState.Booked)
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation($"Found {completedBookings.Count} bookings to mark as completed.");

                        // Update their state to "Completed"
                        foreach (var booking in completedBookings)
                        {
                            booking.State = BookingState.Completed;
                            _logger.LogInformation($"Booking {booking.Id} marked as completed.");
                        }

                        // Save changes to the database
                        if (completedBookings.Any())
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

                // Wait for a certain interval before running again (e.g., every 5 minutes)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}