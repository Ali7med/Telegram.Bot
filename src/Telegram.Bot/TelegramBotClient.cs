using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Helpers;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using File = Telegram.Bot.Types.File;

namespace Telegram.Bot
{
    /// <summary>
    /// A client to use the Telegram Bot API
    /// </summary>
    public class TelegramBotClient : ITelegramBotClient
    {
        private readonly string BaseTelegramUrl = "https://api.telegram.org";

        // Max long value that fits 52 bits is 9007199254740991 and is 16 chars long
        private static readonly Regex TokenFormat = new("^(?<token>[-]?[0-9]{1,16}):.*$", RegexOptions.Compiled);

        private readonly string _baseFileUrl;
        private readonly string _baseRequestUrl;
        private readonly bool _localBotServer;
        private readonly HttpClient _httpClient;

        private IExceptionParser _exceptionParser = new ExceptionParser();

        #region Config Properties

        /// <inheritdoc/>
        public long? BotId { get; }

        /// <summary>
        /// Timeout for requests
        /// </summary>
        public TimeSpan Timeout
        {
            get => _httpClient.Timeout;
            set => _httpClient.Timeout = value;
        }

        /// <inheritdoc />
        public IExceptionParser ExceptionParser
        {
            get => _exceptionParser;
            set => _exceptionParser = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion Config Properties

        #region Events


        /// <summary>
        /// Occurs before sending a request to API
        /// </summary>
        public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

        /// <summary>
        /// Occurs after receiving the response to an API request
        /// </summary>
        public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

        #endregion

        /// <summary>
        /// Create a new <see cref="TelegramBotClient"/> instance.
        /// </summary>
        /// <param name="token">API token</param>
        /// <param name="httpClient">A custom <see cref="HttpClient"/></param>
        /// <param name="baseUrl">
        /// Used to change base url to your private bot api server URL. It looks like
        /// http://localhost:8081. Path, query and fragment will be omitted if present.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="token"/> format is invalid
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="baseUrl"/> format is invalid
        /// </exception>
        public TelegramBotClient(string token,
                                 HttpClient? httpClient = null,
                                 string? baseUrl = default)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));

            var match = TokenFormat.Match(token);
            if (match.Success)
            {
                var botIdString = match.Groups["token"].Value;
                if (long.TryParse(botIdString, out var botId))
                {
                    BotId = botId;
                }
            }

            _localBotServer = TrySetBaseUrl(
                baseUrl ?? BaseTelegramUrl,
                out var effectiveBaseUrl
            );

            _baseRequestUrl = $"{effectiveBaseUrl}/bot{token}";
            _baseFileUrl = $"{effectiveBaseUrl}/file/bot{token}";
            _httpClient = httpClient ?? new HttpClient();
        }


        private static bool TrySetBaseUrl(
             string baseUrl,
             out string target)
        {
            if (baseUrl is null) throw new ArgumentNullException(nameof(baseUrl));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                string.IsNullOrEmpty(baseUri.Scheme) ||
                string.IsNullOrEmpty(baseUri.Authority))
            {
                throw new ArgumentException(
                    "Invalid format. A valid base url looks \"http://localhost:8081\" ",
                    nameof(baseUrl)
                );
            }

            if (!baseUri.Host.Equals("api.telegram.org", StringComparison.Ordinal))
            {
                target = $"{baseUri.Scheme}://{baseUri.Authority}";
                return true;
            }

            target = baseUrl;
            return false;
        }

#if DEBUG
        #region For testing purposes

        internal string BaseRequestUrl => _baseRequestUrl;
        internal string BaseFileUrl => _baseFileUrl;
        internal bool LocalBotServer => _localBotServer;

        #endregion
#endif

        #region Helpers

        private static async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, HttpRequestMessage httpRequest, CancellationToken cancellationToken)
        {
            try
            {
                return await httpClient.SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new RequestException("Request timed out", exception);
            }
            catch (Exception exception)
            {
                throw new RequestException("Exception during making request", exception);
            }
        }

        /// <inheritdoc />
        public async Task<TResult> MakeRequestAsync<TResult>(
            IRequest<TResult> request,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseRequestUrl}/{request.MethodName}";

            var httpRequest = new HttpRequestMessage(request.Method, url)
            {
                Content = request.ToHttpContent()
            };


            if (OnMakingApiRequest != null)
            {
                var reqDataArgs = new ApiRequestEventArgs
                {
                    MethodName = request.MethodName,
                    HttpContent = httpRequest.Content,
                };

                await OnMakingApiRequest.Invoke(this, reqDataArgs, cancellationToken)
                    .ConfigureAwait(false);
            }

            using HttpResponseMessage? httpResponse = await SendAsync(_httpClient, httpRequest, cancellationToken)
                .ConfigureAwait(false);

            if (OnApiResponseReceived != null)
            {
                var reqDataArgs = new ApiRequestEventArgs
                {
                    MethodName = request.MethodName,
                    HttpContent = httpRequest.Content,
                };
                await OnApiResponseReceived.Invoke(
                    this,
                    new ApiResponseEventArgs
                    {
                        ResponseMessage = httpResponse,
                        ApiRequestEventArgs = reqDataArgs
                    },
                    cancellationToken
                ).ConfigureAwait(false);
            }

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                var failedApiResponse = await httpResponse
                    .DeserializeContentAsync<ApiResponse>()
                    .ConfigureAwait(false);

                throw ExceptionParser.Parse(failedApiResponse);
            }

            var successfulApiResponse = await httpResponse
                .DeserializeContentAsync<SuccessfulApiResponse<TResult>>()
                .ConfigureAwait(false);

            return successfulApiResponse.Result;
        }

        /// <inheritdoc />
        public async Task<ApiResponse<TResult>> SendRequestAsync<TResult>(
            IRequest<TResult> request,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseRequestUrl}/{request.MethodName}";

            var httpRequest = new HttpRequestMessage(request.Method, url)
            {
                Content = request.ToHttpContent()
            };


            if (OnMakingApiRequest != null)
            {
                var reqDataArgs = new ApiRequestEventArgs
                {
                    MethodName = request.MethodName,
                    HttpContent = httpRequest.Content,
                };

                await OnMakingApiRequest.Invoke(this, reqDataArgs, cancellationToken)
                    .ConfigureAwait(false);
            }

            using HttpResponseMessage? httpResponse = await SendAsync(_httpClient, httpRequest, cancellationToken)
                .ConfigureAwait(false);

            if (OnApiResponseReceived != null)
            {
                var reqDataArgs = new ApiRequestEventArgs
                {
                    MethodName = request.MethodName,
                    HttpContent = httpRequest.Content,
                };
                await OnApiResponseReceived.Invoke(
                    this,
                    new ApiResponseEventArgs
                    {
                        ResponseMessage = httpResponse,
                        ApiRequestEventArgs = reqDataArgs
                    },
                    cancellationToken
                ).ConfigureAwait(false);
            }

            var apiResponse = await httpResponse
                .DeserializeContentAsync<ApiResponse<TResult>>()
                .ConfigureAwait(false);

            return apiResponse;
        }

        /// <summary>
        /// Test the API token
        /// </summary>
        /// <returns><c>true</c> if token is valid</returns>
        public async Task<bool> TestApiAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await MakeRequestAsync(new GetMeRequest(), cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (ApiRequestException e)
                when (e.ErrorCode == 401)
            {
                return false;
            }
        }

        #endregion Helpers

        /// <inheritdoc />
        public async Task DownloadFileAsync(string filePath,
                                            Stream destination,
                                            CancellationToken cancellationToken = default)
        {
            if (filePath is { Length: < 2 })
                throw new ArgumentException("Invalid file path", nameof(filePath));

            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            //case file is local
            if (_localBotServer && System.IO.File.Exists(filePath))
            {
#if NETCOREAPP3_1_OR_GREATER
                await using var fileStream = System.IO.File.OpenRead(filePath);
#else
                using var fileStream = System.IO.File.OpenRead(filePath);
#endif
                await fileStream.CopyToAsync(destination).ConfigureAwait(false);

                return;
            }

            var fileUri = $"{_baseFileUrl}/{filePath}";

            //var response = await _httpClient
            //    .GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            using var httpResponse = await GetAsync(_httpClient, fileUri, cancellationToken)
                .ConfigureAwait(false);
            //response.EnsureSuccessStatusCode();

            //using (httpResponse)
            //{
            //await response.Content.CopyToAsync(destination)
            //    .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var failedApiResponse = await httpResponse
                    .DeserializeContentAsync<ApiResponse>(includeBody: false)
                    .ConfigureAwait(false);

                throw ExceptionParser.Parse(failedApiResponse);
            }

            if (httpResponse.Content is null)
                throw new RequestException(
                    "Response doesn't contain any content",
                    httpResponse.StatusCode
                );

            try
            {
                await httpResponse.Content.CopyToAsync(destination)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new RequestException(
                    "Exception during file download",
                    httpResponse.StatusCode,
                    exception
                );
            }
            //}

            static async Task<HttpResponseMessage> GetAsync(HttpClient httpClient, string requestUri, CancellationToken cancellationToken)
            {
                try
                {
                    return await httpClient
                        .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (TaskCanceledException exception)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    throw new RequestException("Request timed out", exception);
                }
                catch (Exception exception)
                {
                    throw new RequestException("Exception during file download", exception);
                }
            }

        }

        /// <inheritdoc />
        public async Task<File> GetInfoAndDownloadFileAsync(string fileId,
                                                            Stream destination,
                                                            CancellationToken cancellationToken = default)
        {
            var file = await MakeRequestAsync(new GetFileRequest(fileId), cancellationToken)
                .ConfigureAwait(false);

            await DownloadFileAsync(file.FilePath, destination, cancellationToken)
                .ConfigureAwait(false);

            return file;
        }
    }
}
