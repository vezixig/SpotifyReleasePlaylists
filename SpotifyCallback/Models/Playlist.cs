namespace PlaylistGenerator.Models
{
    /// <summary>A playlist containing none to many tracks.</summary>
    /// <param name="Id">The id of the playlist.</param>
    /// <param name="Name">The name of the playlist.</param>
    public record Playlist(string Id, string Name)
    {
        #region Properties

        /// <summary>Gets the list of tracks in the playlist.</summary>
        /// <remarks>This is an added property to the Spotify data model.</remarks>
        public List<PlaylistItem> Tracklist { get; } = new();

        #endregion
    }
}