using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Xunit;

namespace Telegram.Bot.Tests.Unit
{
    public class RequestExceptionsTests
    {
        [Fact]
        public async Task Should_Throw_RequestException_On_Incorrect_Proxy()
        {
            HttpClientHandler httpClientHandler = new()
            {
                Proxy = new WebProxy("http://proxy.test")
            };
            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.NotNull(requestException.InnerException);
            Assert.Null(requestException.HttpStatusCode);
            Assert.Null(requestException.Body);

            Assert.IsType<HttpRequestException>(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_TaskCancelledException_On_Cancelled_Token()
        {
            TelegramBotClient botClient = new ("1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy");

            using CancellationTokenSource cts = new ();
            CancellationToken token = cts.Token;
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await botClient.GetUpdatesAsync(cancellationToken: token)
            );
        }

        [Fact]
        public async Task Should_Throw_RequestException_On_Timed_Out_Request()
        {
            MockHttpMessageHandler httpClientHandler = new (
                _ => throw new TaskCanceledException()
            );
            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.NotNull(requestException.InnerException);
            Assert.IsType<TaskCanceledException>(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_RequestException_With_Null_Response_Content()
        {
            MockHttpMessageHandler httpClientHandler = new (HttpStatusCode.OK);
            HttpClient httpClient = new (httpClientHandler);

            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.Equal(HttpStatusCode.OK, requestException.HttpStatusCode);
            Assert.Null(requestException.Body);
            Assert.Null(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_RequestException_Due_To_Empty_Or_Null_Content()
        {
            MockHttpMessageHandler httpClientHandler = new (
                HttpStatusCode.OK,
                new StringContent(string.Empty)
            );

            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.Equal(HttpStatusCode.OK, requestException.HttpStatusCode);
            Assert.NotNull(requestException.Body);
            Assert.Empty(requestException.Body);

            Assert.Null(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_RequestException_With_JsonSerializationException_On_Successful_Response()
        {
            MockHttpMessageHandler httpClientHandler = new (
                HttpStatusCode.OK,
                new StringContent("{}")
            );

            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.Equal(HttpStatusCode.OK, requestException.HttpStatusCode);
            Assert.NotNull(requestException.Body);
            Assert.Equal("{}", requestException.Body);

            Assert.NotNull(requestException.InnerException);
            Assert.IsType<JsonSerializationException>(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_RequestException_With_JsonSerializationException_On_Failed_Response()
        {
            MockHttpMessageHandler httpClientHandler = new (
                HttpStatusCode.BadRequest,
                new StringContent("{}")
            );

            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            RequestException requestException = await Assert.ThrowsAsync<RequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.Equal(HttpStatusCode.BadRequest, requestException.HttpStatusCode);
            Assert.NotNull(requestException.Body);
            Assert.Equal("{}", requestException.Body);

            Assert.NotNull(requestException.InnerException);
            Assert.IsType<JsonSerializationException>(requestException.InnerException);
        }

        [Fact]
        public async Task Should_Throw_ApiRequestException_With_JsonSerializationException_On_Failed_Response()
        {
            MockHttpMessageHandler httpClientHandler = new (
                HttpStatusCode.BadRequest,
                new StringContent(@"{""ok"":false,""description"":""Bad Request: chat_id is incorrect"",""error_code"":400}")
            );

            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            ApiRequestException apiRequestException = await Assert.ThrowsAsync<ApiRequestException>(
                async () => await botClient.GetUpdatesAsync()
            );

            Assert.Equal(400, apiRequestException.ErrorCode);
            Assert.Equal("Bad Request: chat_id is incorrect", apiRequestException.Message);
        }

        [Fact]
        public async Task Should_Pass_Same_Instance_Of_Request_Event_Args()
        {
            MockHttpMessageHandler httpClientHandler = new (
                HttpStatusCode.OK,
                new StringContent(@"{""ok"":true,""result"":[]}}")
            );

            HttpClient httpClient = new (httpClientHandler);
            TelegramBotClient botClient = new (
                "1234567:4TT8bAc8GHUspu3ERYn-KGcvsvGB9u_n4ddy",
                httpClient
            );

            ApiRequestEventArgs requestEventArgs1 = default!;
            ApiRequestEventArgs requestEventArgs2 = default!;

            botClient.OnMakingApiRequest += async (sender, args, ct) => requestEventArgs1 = args;
            botClient.OnApiResponseReceived += async (sender, args, ct) => requestEventArgs2 = args.ApiRequestEventArgs;

            await botClient.GetUpdatesAsync();

            Assert.NotNull(requestEventArgs1);
            Assert.NotNull(requestEventArgs2);
            Assert.Equal(requestEventArgs1.MethodName, requestEventArgs2.MethodName);
            Assert.Equal(requestEventArgs1.HttpContent, requestEventArgs2.HttpContent);
        }
    }
}
