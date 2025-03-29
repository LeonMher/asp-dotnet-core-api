using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace YourProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MusicRoomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MusicRoomsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/musicrooms
        [HttpGet]
        public async Task<IActionResult> GetAllMusicRooms(
            string? name = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.MusicRooms.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(m => m.Name.Contains(name));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    ImageUrl = Url.Action("GetImage", new { fileName = m.ImageFileName })!,
                    ThumbnailUrl = Url.Action("GetThumbnail", new { fileName = m.ImageFileName })!
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET: api/musicrooms/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMusicRoom(int id)
        {
            var musicRoom = await _context.MusicRooms
                .Where(m => m.Id == id)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    ImageUrl = Url.Action("GetImage", new { fileName = m.ImageFileName })!,
                    ThumbnailUrl = Url.Action("GetThumbnail", new { fileName = m.ImageFileName })!
                })
                .FirstOrDefaultAsync();

            return musicRoom == null ? NotFound() : Ok(musicRoom);
        }

        // GET: api/musicrooms/image/filename.jpg
        [HttpGet("image/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Filename cannot be empty");
            }

            if (_environment.WebRootPath == null)
            {
                return StatusCode(500, "Web root path not configured");
            }

            try
            {
                var imagePath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    fileName);

                if (!System.IO.File.Exists(imagePath))
                {
                    return NotFound();
                }

                return PhysicalFile(imagePath, "image/jpeg");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading image: {ex.Message}");
            }
        }

        [HttpGet("thumbnail/{fileName}")]
        public IActionResult GetThumbnail(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Filename cannot be empty");
            }

            if (_environment.WebRootPath == null)
            {
                return StatusCode(500, "Web root path not configured");
            }

            try
            {
                var thumbnailPath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    "thumbnails",
                    fileName);

                if (System.IO.File.Exists(thumbnailPath))
                {
                    return PhysicalFile(thumbnailPath, "image/jpeg");
                }

                // Fallback to original image
                return GetImage(fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading thumbnail: {ex.Message}");
            }
        }

        // GET: api/musicrooms/5/busydates
        [HttpGet("{id}/busydates")]
        public async Task<IActionResult> GetBusyDates(int id)
        {
            var busyDates = await _context.Bookings
                .Where(b => b.MusicRoomId == id && b.EndTime > DateTime.Now)
                .Select(b => new
                {
                    b.StartTime,
                    b.EndTime
                })
                .ToListAsync();

            return Ok(busyDates);
        }

        // POST: api/musicrooms
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMusicRoom([FromForm] MusicRoomCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                return BadRequest("Image file is required");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");
            var thumbnailsFolder = Path.Combine(uploadsFolder, "thumbnails");

            Directory.CreateDirectory(uploadsFolder);
            Directory.CreateDirectory(thumbnailsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var thumbnailPath = Path.Combine(thumbnailsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(stream);
            }

            // In a real app, you'd use ImageSharp or similar to create a proper thumbnail
            System.IO.File.Copy(filePath, thumbnailPath, true);

            var musicRoom = new MusicRoom
            {
                Name = model.Name,
                Description = model.Description,
                ImageFileName = uniqueFileName,
                ImagePath = filePath,
                ImageContentType = model.ImageFile.ContentType
            };

            _context.MusicRooms.Add(musicRoom);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMusicRoom), new { id = musicRoom.Id }, new
            {
                musicRoom.Id,
                musicRoom.Name,
                musicRoom.Description,
                ImageUrl = Url.Action("GetImage", new { fileName = musicRoom.ImageFileName })!
            });
        }

        // PUT: api/musicrooms/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMusicRoom(int id, [FromForm] MusicRoomUpdateDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var musicRoom = await _context.MusicRooms.FindAsync(id);
            if (musicRoom == null)
            {
                return NotFound();
            }

            musicRoom.Name = model.Name;
            musicRoom.Description = model.Description;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Delete old files
                var oldImagePath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    musicRoom.ImageFileName);

                var oldThumbnailPath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    "thumbnails",
                    musicRoom.ImageFileName);

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                if (System.IO.File.Exists(oldThumbnailPath))
                {
                    System.IO.File.Delete(oldThumbnailPath);
                }

                // Upload new files
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
                var filePath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    uniqueFileName);

                var thumbnailPath = Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "rooms",
                    "thumbnails",
                    uniqueFileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                System.IO.File.Copy(filePath, thumbnailPath, true);

                musicRoom.ImageFileName = uniqueFileName;
                musicRoom.ImagePath = filePath;
                musicRoom.ImageContentType = model.ImageFile.ContentType;
            }

            _context.Entry(musicRoom).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MusicRoomExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/musicrooms/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMusicRoom(int id)
        {
            var musicRoom = await _context.MusicRooms.FindAsync(id);
            if (musicRoom == null)
            {
                return NotFound();
            }

            // Delete associated files
            var imagePath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "rooms",
                musicRoom.ImageFileName);

            var thumbnailPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "rooms",
                "thumbnails",
                musicRoom.ImageFileName);

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            if (System.IO.File.Exists(thumbnailPath))
            {
                System.IO.File.Delete(thumbnailPath);
            }

            _context.MusicRooms.Remove(musicRoom);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MusicRoomExists(int id)
        {
            return _context.MusicRooms.Any(e => e.Id == id);
        }
    }

    public class MusicRoomCreateDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IFormFile ImageFile { get; set; } = null!;
    }

    public class MusicRoomUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IFormFile? ImageFile { get; set; }
    }
}