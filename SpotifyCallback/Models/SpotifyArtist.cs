namespace PlaylistGenerator.Models
{
    /// <summary>An artist.</summary>
    /// <param name="Artist">The name of the artist.</param>
    /// <param name="Id">The id of the artist.</param>
    public record SpotifyArtist(string Artist, string Id);
}