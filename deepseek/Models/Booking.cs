public class Booking
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingState State { get; set; } = BookingState.Booked; // Default state

    // Relationship with User
    public string UserId { get; set; }
    public User User { get; set; }

    // Relationship with MusicRoom
    public int MusicRoomId { get; set; }
    public MusicRoom MusicRoom { get; set; }
}