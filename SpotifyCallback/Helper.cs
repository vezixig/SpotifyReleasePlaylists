namespace PlaylistGenerator
{
    using System.Text;

    /// <summary>Contains helper functions.</summary>
    public static class Helper
    {
        #region Methods

        /// <summary>Returns a random string consisting of uppercase letters.</summary>
        /// <param name="length">The length of the string to generate.</param>
        /// <returns>A string.</returns>
        public static string GenerateRandomString(int length)
        {
            var random = new Random(DateTime.Now.Microsecond);
            var randomString = new StringBuilder(length);
            for (var i = 0; i < length; i++) randomString.Append((char)random.Next(65, 90));
            return randomString.ToString();
        }

        #endregion
    }
}