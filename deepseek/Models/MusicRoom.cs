public class MusicRoom
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "Blue", "Red", "Green"
    public string Description { get; set; } // Optional: Add a description for the room
    public string ImagePath { get; set; }       // Path where image is stored
    public string ImageFileName { get; set; }   // Original filename
    public string ImageContentType { get; set; } // MIME type
}