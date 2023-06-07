namespace PlaylistGenerator.Models
{
    /// <summary>An item contained in <see cref="Playlist" />.</summary>
    /// <param name="Track">The actual track in the playlist.</param>
    public record PlaylistItem(Track Track);
}