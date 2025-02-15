// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<Comment> Comments { get; set; }
    public DbSet<MusicRoom> MusicRooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}