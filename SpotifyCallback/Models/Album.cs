namespace PlaylistGenerator.Models
{
    /// <summary>A music album.</summary>
    /// <param name="Album_Type">The type of the Album - possible values are: album, single, compilation.</param>
    /// <param name="Id">The id if the album.</param>
    /// <param name="Name">The album name.</param>
    public record Album(
        string Album_Type,
        string Id,
        string Name)
    {
        #region Properties

        /// <summary>Gets or sets the release date of the album.</summary>
        public required string Release_Date { get; set; }

        #endregion
    }
}