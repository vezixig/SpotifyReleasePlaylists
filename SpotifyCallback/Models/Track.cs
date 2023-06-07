namespace PlaylistGenerator.Models
{
    /// <summary>A track contained in an album or a playlist.</summary>
    /// <param name="Album">The album the track is contained in.</param>
    /// <param name="Id">The id of the Track.</param>
    /// <param name="Name">The name of the track.</param>
    /// <param name="Uri">The Uri the track can be reached at.</param>
    public record Track(
        Album Album,
        string Id,
        string Name,
        string Uri);
}