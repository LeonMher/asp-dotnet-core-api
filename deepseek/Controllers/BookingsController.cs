using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Only authenticated users can access these endpoints
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public BookingsController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/bookings
    [HttpGet]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.MusicRoom)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                MusicRoomName = b.MusicRoom.Name,
                UserName = $"{b.User.FirstName} {b.User.LastName}"
            })
            .ToListAsync();

        return Ok(bookings);
    }


    // GET: api/bookings/mybookings
    [HttpGet("mybookings")]
    public async Task<IActionResult> GetMyBookings()
    {
        // Get the current user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Fetch bookings for the current user
        var bookings = await _context.Bookings
            .Include(b => b.User)
            .Include(b => b.MusicRoom)
            .Where(b => b.UserId == userId)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                MusicRoomName = b.MusicRoom.Name,
                UserName = $"{b.User.FirstName} {b.User.LastName}"
            })
            .ToListAsync();

        return Ok(bookings);
    }


    // POST: api/bookings
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto createBookingDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var musicRoom = await _context.MusicRooms.FindAsync(createBookingDto.MusicRoomId);
        if (musicRoom == null)
        {
            return NotFound("Music room not found.");
        }

        var isRoomBooked = await _context.Bookings
            .AnyAsync(b => b.MusicRoomId == createBookingDto.MusicRoomId &&
                           b.StartTime < createBookingDto.EndTime &&
                           b.EndTime > createBookingDto.StartTime);

        if (isRoomBooked)
        {
            return BadRequest("The room is already booked for the selected time.");
        }

        var booking = new Booking
        {
            StartTime = createBookingDto.StartTime,
            EndTime = createBookingDto.EndTime,
            UserId = userId,
            MusicRoomId = createBookingDto.MusicRoomId
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(new BookingDto
        {
            Id = booking.Id,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            MusicRoomName = musicRoom.Name,
            UserName = $"{user.FirstName} {user.LastName}"
        });
    }


    // DELETE: api/bookings/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        // Get the current user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find the booking
        var booking = await _context.Bookings
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        // Verify the user is the author of the booking
        if (booking.UserId != userId)
        {
            return Forbid();
        }

        // Cancel the booking
        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Booking canceled successfully" });
    }
}