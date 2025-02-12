using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Only authenticated users can access these endpoints
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public CommentsController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // POST: api/comments
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto commentDto)
    {
        // Log the incoming request
        Console.WriteLine("Received comment creation request:");
        Console.WriteLine($"Content: {commentDto.Content}");

        // Get the current user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"User ID from token: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User ID not found in token.");
            return Unauthorized();
        }

        // Find the user
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            Console.WriteLine("User not found in the database.");
            return Unauthorized();
        }

        // Log the user details
        Console.WriteLine($"User found: {user.UserName}");

        // Create the comment
        var comment = new Comment
        {
            Content = commentDto.Content,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Save the comment
        _context.Comments.Add(comment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving comment: {ex.Message}");
            return StatusCode(500, "An error occurred while saving the comment.");
        }

        // Return the comment with the user's name
        return Ok(new
        {
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            Author = $"{user.FirstName} {user.LastName}"
        });
    }

    // PUT: api/comments/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto updateCommentDto)
    {
        // Log the incoming request
        Console.WriteLine("Received comment update request:");
        Console.WriteLine($"Comment ID: {id}");
        Console.WriteLine($"New Content: {updateCommentDto.Content}");

        // Get the current user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"User ID from token: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User ID not found in token.");
            return Unauthorized();
        }

        // Find the comment
        var comment = await _context.Comments
            .Include(c => c.User) // Include the user details
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
        {
            Console.WriteLine("Comment not found.");
            return NotFound();
        }

        // Verify the user is the author of the comment
        if (comment.UserId != userId)
        {
            Console.WriteLine("User is not the author of the comment.");
            return Forbid();
        }

        // Update the comment content
        comment.Content = updateCommentDto.Content;
        comment.CreatedAt = DateTime.UtcNow; // Optionally update the timestamp

        // Save the changes
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating comment: {ex.Message}");
            return StatusCode(500, "An error occurred while updating the comment.");
        }

        // Return the updated comment
        return Ok(new
        {
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            Author = $"{comment.User.FirstName} {comment.User.LastName}"
        });
    }

    // DELETE: api/comments/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        // Log the incoming request
        Console.WriteLine("Received comment deletion request:");
        Console.WriteLine($"Comment ID: {id}");

        // Get the current user's ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"User ID from token: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User ID not found in token.");
            return Unauthorized();
        }

        // Find the comment
        var comment = await _context.Comments
            .Include(c => c.User) // Include the user details
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
        {
            Console.WriteLine("Comment not found.");
            return NotFound();
        }

        // Verify the user is the author of the comment
        if (comment.UserId != userId)
        {
            Console.WriteLine("User is not the author of the comment.");
            return Forbid();
        }

        // Delete the comment
        _context.Comments.Remove(comment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting comment: {ex.Message}");
            return StatusCode(500, "An error occurred while deleting the comment.");
        }

        // Return a success message
        return Ok(new { Message = "Comment deleted successfully" });
    }

    // GET: api/comments
    [HttpGet]
    [AllowAnonymous] // Allow anyone to view comments
    public async Task<IActionResult> GetAllComments()
    {
        var comments = await _context.Comments
            .Include(c => c.User) // Include the user details
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                Author = $"{c.User.FirstName} {c.User.LastName}"
            })
            .ToListAsync();

        return Ok(comments);
    }
}