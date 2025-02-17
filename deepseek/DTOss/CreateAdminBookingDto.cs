public class CreateAdminBookingDto
{
    public string UserId { get; set; } // Admin specifies the user

    public int MusicRoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}