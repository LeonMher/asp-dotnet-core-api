public class CreateBookingDto
{
    public int MusicRoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingState State { get; set; } = BookingState.Booked; // Default state
}