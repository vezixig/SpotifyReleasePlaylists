namespace PlaylistGenerator
{
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Web;
    using Enums;
    using Models;

    // todo: Replace console write with logger
    // todo: Add all API endpoints into separate class
    // todo: Refactor LoadArtistsAsync

    /// <summary>An App that currates spotify playlists based on the release date of albums for provided artists.</summary>
    public class PlaylistGenerator
    {
        private const string ClientId = "048ffaaf02da43a2bf87dcd4b3ecc328";

        /// <summary>Endpoint URI of the Spotify Account API</summary>
        private const string EndpointAccount = "https://accounts.spotify.com/api/";

        /// <summary>Endpoint URI of the Spotify Web API</summary>
        /// <seealso href="https://developer.spotify.com/documentation/web-api" />
        private const string EndpointApi = "https://api.spotify.com/v1/";

        /// <summary>The callback URL opened by spotify after the Spotify user authorized the APP.</summary>
#pragma warning disable S1075
        private const string LocalCallbackUri = "https://localhost:8080/callback";
#pragma warning restore S1075

        /// <summary>A Suffix attached to the playlists after the date YYYY-MM.</summary>
        private const string PlaylistSuffix = " - New Releases - Postpunk/Gothic Rock/Darkwave";

        #region Fields

        /// <summary>The client used to communicate with tha Spotify API.</summary>
        private readonly HttpClient _client = new();


        /// <remarks>Key is the date in YYYY-MM format, value is the playlist id.</remarks>
        private readonly Dictionary<string, Playlist> _playlists = new();

        /// <summary>The secret received through the Spotify Dev-Dashboard</summary>
        /// <seealso href="https://developer.spotify.com/dashboard" />
        private string _clientSecret = string.Empty;

        /// <summary>The code received after a successful authorization by Spotify.</summary>
        private string _spotifyCode = string.Empty;

        /// <summary>Is (optionally) used by Spotify to prevent cross-site attacks.</summary>
        private string _state = string.Empty;

        /// <summary>The authenticated user containing the bearer token.</summary>
        private User _user = new(string.Empty);

        #endregion

        #region Methods

        /// <summary>Starts the playlist generation.</summary>
        /// <returns>An awaitable task object.</returns>
        public async Task RunAsync()
            => await HandleStateAsync(AppState.Startup);

        private async Task CheckPlaylistsAsync()
        {
            Console.WriteLine("Requesting user id...");
            _user = await GetUserAsync();
            Console.WriteLine("Requesting playlists...");
            await LoadPlaylistsAsync();
            Console.WriteLine($"{_playlists.Count} matching playlists found.");
            Console.WriteLine("Requesting artists...");
            await LoadArtistsAsync();

            _client.Dispose();
        }

        private async Task<Playlist> CreatePlaylistAsync(string releaseDate)
        {
            Console.WriteLine($"Creating new playlist for {releaseDate}");

            var payload = new { name = $"{releaseDate}{PlaylistSuffix}", description = "", @public = false };
            var json = JsonSerializer.Serialize(payload);
            var newPlaylist = await _client.WriteAsync<Playlist>(EndpointApi + $"users/{_user.Id}/playlists", new StringContent(json));

            return newPlaylist;
        }

        private async ValueTask<Playlist> GetPlaylist(string dateKey)
        {
            if (!_playlists.TryGetValue(dateKey, out var playlist))
            {
                playlist = await CreatePlaylistAsync(dateKey);
                _playlists.Add(dateKey, new(playlist.Id, playlist.Name));
            }

            if (playlist.Tracklist.Count == 0) playlist.Tracklist.AddRange(await LoadPlaylistItemsAsync(playlist.Id));
            return playlist;
        }

        private async Task<User> GetUserAsync()
            => await _client.ReadScalarAsync<User>(EndpointApi + "me");

        private async Task HandleStartupAsync()
        {
            Console.WriteLine("App started");
            Console.WriteLine("Starting web listener...");
            await StartWebServiceAsync();
            await HandleStateAsync(AppState.ServiceStarted);
        }


        private async Task HandleStateAsync(AppState state)
        {
            switch (state)
            {
                case AppState.Startup:
                    await HandleStartupAsync();
                    break;
                case AppState.ServiceStarted:
                    RequestSpotifyAuth();
                    break;
                case AppState.ReceivedCode:
                    await RequestToken();
                    break;
                case AppState.ReceivedToken:
                    await CheckPlaylistsAsync();
                    break;
                case AppState.Finished:
                    Console.WriteLine("Playlist generation finished. Press RETURN to close this app.");
                    Console.ReadLine();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private async Task LoadArtistsAsync()
        {
            // load artists from json
            var fileContent = await File.ReadAllTextAsync("artists.json");

            // remove commented lines as JSON accepts no comments
            var lines = fileContent.Split("\r\n");
            lines = lines.Where(o => !o.Contains("//")).ToArray();
            fileContent = string.Join("\r\n", lines);

            var artists = JsonSerializer.Deserialize<SpotifyArtist[]>(fileContent)
                          ?? throw new JsonException("Artists file could not be parsed.");

            Console.WriteLine($"Loaded {artists.Length} artists from file");

            // load albums per artist
            foreach (var artist in artists)
            {
                Console.WriteLine($"Processing artist {artist.Artist}");

                var albums = await _client.ReadManyAsync<Album>(EndpointApi + $"artists/{artist.Id}/albums");

                Console.WriteLine($"\tFound {albums.Count} albums");
                foreach (var album in albums)
                {
                    // skip compilation albums
                    if (album.Album_Type == "compilation") continue; //todo: put magic string into static class

                    // if no month is set january is used
#pragma warning disable S1643
                    if (album.Release_Date.Length == 4) album.Release_Date += "-01";
#pragma warning restore S1643
                    album.Release_Date = album.Release_Date[..7];

                    // get playlist tracks
                    var playlist = await GetPlaylist(album.Release_Date);

                    // check if album is in playlist
                    var isAlbumInPlaylist = playlist.Tracklist.Exists(o => o.Track.Album.Id == album.Id);
                    if (isAlbumInPlaylist)
                    {
                        Console.WriteLine($"\t\tSKIP {album.Name} already in playlist");
                        continue;
                    }

                    // get album tracks
                    Console.WriteLine($"\t\tADD {album.Name} to playlists");
                    var albumTracks = await _client.ReadManyAsync<Track>(EndpointApi + $"albums/{album.Id}/tracks");

                    var trackIds = albumTracks.Select(o => o.Uri);
                    var payload = new { uris = trackIds };
                    var json = JsonSerializer.Serialize(payload);

                    var response = _client.PostAsync(EndpointApi + $"playlists/{playlist.Id}/tracks", new StringContent(json)).Result;
                    if (!response.IsSuccessStatusCode) throw new(response.StatusCode.ToString());
                }
            }

            await HandleStateAsync(AppState.Finished);
        }


        private async Task<List<PlaylistItem>> LoadPlaylistItemsAsync(string playlistId)
        {
            var tracks = await _client.ReadManyAsync<PlaylistItem>(EndpointApi + $"playlists/{playlistId}/tracks");
            return tracks;
        }

        private async Task LoadPlaylistsAsync()
        {
            _playlists.Clear();
            var playlists = await _client.ReadManyAsync<Playlist>(EndpointApi + "me/playlists");
            foreach (var playlist in playlists)
            {
                if (!playlist.Name.EndsWith(PlaylistSuffix)) continue;
                var key = playlist.Name[..7];
                _playlists.Add(key, playlist);
            }
        }


        /// <summary>Handles the receiving of the auth code through the callback API endpoint.</summary>
        /// <param name="code">The Spotify auth code</param>
        /// <param name="state">The received state to prevent cross-site attacks.</param>
        private async Task ReceivedAuthCode(string code, string state)
        {
            if (state != _state) throw new("State mismatch. Possible cross-site attack detected.");
            Console.WriteLine("Auth code received");

            _spotifyCode = code;

            await HandleStateAsync(AppState.ReceivedCode);
        }

        /// <summary>Request the auth code from spotify.</summary>
        /// <remarks>An auth site from spotify is opened in the browser and the user needs to authorize this app.</remarks>
        private void RequestSpotifyAuth()
        {
            Console.WriteLine("Enter client secret:");
            _clientSecret = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_clientSecret)) throw new ArgumentException("No client secret entered.");

            _state = Helper.GenerateRandomString(16);

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("response_type", "code");
            queryString.Add("client_id", ClientId);
            queryString.Add("scope", "playlist-read-private playlist-modify-private");
            queryString.Add("redirect_uri", LocalCallbackUri);
            queryString.Add("state", _state);
            queryString.Add("show_dialog", "true");
            var query = queryString.ToString();

            // Open the spotify auth site in the default browser
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = $"https://accounts.spotify.com/authorize?{query}",
                    UseShellExecute = true
                });

            Console.WriteLine("Waiting for Spotify code...");
        }

        private async Task RequestToken()
        {
            Console.WriteLine("Requesting token...");

            var col = new Dictionary<string, string>
            {
                { "redirect_uri", LocalCallbackUri },
                { "code", _spotifyCode },
                { "grant_type", "authorization_code" }
            };

            _client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(ClientId + ":" + _clientSecret)));

            var token = await _client.WriteAsync<Token>(EndpointAccount + "token", new FormUrlEncodedContent(col));

            _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Bearer " + token.Access_Token);

            Console.WriteLine("Token received");
            await HandleStateAsync(AppState.ReceivedToken);
        }

        /// <summary>Starts the webservice that is listening for the auth callback.</summary>
        /// <returns>An awaitable task object.</returns>
        /// <exception cref="MissingFieldException">Thrown if a field is missing in the callback request.</exception>
        private async Task StartWebServiceAsync()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            app.UseHttpsRedirection();
            app.MapGet(
                "/callback",
                async o =>
                {
                    if (!o.Request.Query.ContainsKey("code")) throw new MissingFieldException("Code not found in callback!");
                    if (!o.Request.Query.ContainsKey("state")) throw new MissingFieldException("State not found in callback!");

                    await ReceivedAuthCode(o.Request.Query["code"]!, o.Request.Query["state"]!);
                });
            await app.StartAsync();
            Console.WriteLine("Listener running");
        }

        #endregion
    }
}