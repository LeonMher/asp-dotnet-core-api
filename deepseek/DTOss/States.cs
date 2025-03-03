using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingState
{
    Booked,
    Canceled,
    Completed
}