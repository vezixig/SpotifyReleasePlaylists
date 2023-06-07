namespace PlaylistGenerator.Models
{
    /// <summary>Contains token data received after an authentication against the Spotify API.</summary>
    /// <param name="Access_Token">The bearer access token.</param>
    /// <param name="Scope">The scope of actions the token authorized to do.</param>
    public record Token(string Access_Token, string Scope);
}