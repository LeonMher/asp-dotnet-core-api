using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;
using Microsoft.EntityFrameworkCore;

namespace deepseek.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountdownController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CountdownController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetCountdown(int bookingId)
        {
            // Get the booking
            var booking = await _context.Bookings
                .Include(b => b.MusicRoom)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Get current time in Yerevan
            var yerevanTimeZone = TZConvert.GetTimeZoneInfo("Asia/Yerevan");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, yerevanTimeZone);

            // Calculate remaining time
            var timeRemaining = booking.EndTime - now;

            if (timeRemaining.TotalSeconds < 0)
            {
                return Ok(new { Message = "Booking has already ended." });
            }

            return Ok(new
            {
                BookingId = booking.Id,
                RoomId = booking.MusicRoomId,
                RoomName = booking.MusicRoom.Name,
                TimeRemaining = timeRemaining.ToString(@"hh\:mm\:ss")
            });
        }
    }
}