using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class MusicRoomsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MusicRoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/musicrooms
    [HttpGet]
    public async Task<IActionResult> GetAllMusicRooms(string name = null, int page = 1, int pageSize = 10)
    {
        var query = _context.MusicRooms.AsQueryable();

        // Filter by name
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(m => m.Name.Contains(name));
        }

        // Paginate results
        var musicRooms = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Description
            })
            .ToListAsync();

        return Ok(musicRooms);
    }

    [HttpGet("{id}/busydates")]
    public async Task<IActionResult> GetBusyDates(int id)
    {
        // Fetch bookings for the specified music room
        var busyDates = await _context.Bookings
            .Where(b => b.MusicRoomId == id)
            .Select(b => new
            {
                StartTime = b.StartTime,
                EndTime = b.EndTime
            })
            .ToListAsync();

        return Ok(busyDates);
    }
}