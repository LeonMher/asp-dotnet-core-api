public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationship with User
    public string UserId { get; set; }
    public User User { get; set; }
}