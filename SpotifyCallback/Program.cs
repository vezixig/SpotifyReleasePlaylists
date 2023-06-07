namespace PlaylistGenerator;

internal static class Program
{
    #region Methods

    private static void Main(string[] args)
    {
        try
        {
            var generator = new PlaylistGenerator();
            generator.RunAsync().GetAwaiter().GetResult();
            Console.ReadLine();
        }
        catch (Exception ex) { Console.WriteLine($"An error occurred while creating the playlists: {ex.Message}"); }
    }

    #endregion
}