public class BookingDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingState State { get; set; }
    public string MusicRoomName { get; set; }
    public string UserName { get; set; }
}