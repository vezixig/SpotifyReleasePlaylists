namespace PlaylistGenerator.Models
{
    /// <summary>Helper class to encapsulate multiple results received from the Spotify API.</summary>
    /// <typeparam name="T">The actual return type of the API.</typeparam>
    /// <param name="Items">The items contained.</param>
    public record Container<T>(List<T> Items);
}