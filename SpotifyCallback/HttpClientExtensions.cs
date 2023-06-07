namespace PlaylistGenerator
{
    using System.Text.Json;
    using Models;

    /// <summary>Extension methods for <seealso cref="HttpClient" />.</summary>
    public static class HttpClientExtensions
    {
        /// <summary>The default item return limit is 20, the maximum is 50.</summary>
        /// <seealso href="https://developer.spotify.com/documentation/web-api" />
        private const int SpotifyResultLimit = 50;

        #region Fields

        /// <remarks>
        ///     Text.Json default is to read case sensitive.
        ///     C# classes are pascal case and the Spotify API returns snake case.
        /// </remarks>
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        #endregion

        #region Methods

        /// <summary>Asynchronously reads multiple instances of type T from the specified URI using the provided HttpClient.</summary>
        /// <typeparam name="T">The type of objects to read.</typeparam>
        /// <param name="client">The HttpClient instance to use for the request.</param>
        /// <param name="uri">The URI to read the data from.</param>
        /// <returns>A Task representing the asynchronous operation. The task result contains a List of instances of type T.</returns>
        public static async Task<List<T>> ReadManyAsync<T>(this HttpClient client, string uri)
        {
            var offset = 0;
            var result = new List<T>();

            while (true)
            {
                var data = await client.ReadScalarAsync<Container<T>>($"{uri}?limit=50&offset={offset}");
                result.AddRange(data.Items);
                if (data.Items.Count < SpotifyResultLimit) break;

                offset += SpotifyResultLimit;
            }

            return result;
        }

        /// <summary>Asynchronously reads a single instance of type T from the specified URI using the provided HttpClient.</summary>
        /// <typeparam name="T">The type of object to read.</typeparam>
        /// <param name="client">The HttpClient instance to use for the request.</param>
        /// <param name="uri">The URI to read the data from.</param>
        /// <returns>A Task representing the asynchronous operation. The task result contains an instance of type T.</returns>
        public static async Task<T> ReadScalarAsync<T>(this HttpClient client, string uri)
        {
            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode) throw new(response.StatusCode.ToString());

            var jsonStream = await response.Content.ReadAsStreamAsync()
                             ?? throw new NullReferenceException("JSON-stream could not be red.");

            var data = await JsonSerializer.DeserializeAsync<T>(jsonStream, JsonOptions);
            return data ?? throw new NullReferenceException($"JSON could not be converted to {typeof(T).Name}.");
        }

        /// <summary>Asynchronously writes data to the specified URI using the provided HttpClient.</summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="client">The HttpClient instance to use for the request.</param>
        /// <param name="uri">The URI to write the data to.</param>
        /// <param name="payload">The HTTP content representing the data to write.</param>
        /// <returns>A Task representing the asynchronous operation. The task result contains an instance of type T.</returns>
        public static async Task<T> WriteAsync<T>(this HttpClient client, string uri, HttpContent payload)
        {
            var response = client.PostAsync(uri, payload)
                .Result;
            if (!response.IsSuccessStatusCode) throw new(response.StatusCode.ToString());

            var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
            return data ?? throw new NullReferenceException($"JSON could not be converted to {typeof(T).Name}.");
        }

        #endregion
    }
}