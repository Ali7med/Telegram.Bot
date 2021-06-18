using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.Tests.Unit
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _httpStatusCode;
        private readonly HttpContent? _responseContent;
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _action;

        public MockHttpMessageHandler(
            HttpStatusCode httpStatusCode,
            HttpContent? responseContent = default)
        {
            _httpStatusCode = httpStatusCode;
            _responseContent = responseContent;
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> action)
        {
            _action = action;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_action != null)
            {
                return Task.FromResult(_action.Invoke(request));
            }

            if (_httpStatusCode is null)
            {
                throw new InvalidOperationException();
            }

            HttpResponseMessage httpResponse = new(_httpStatusCode.Value)
            {
                Content = _responseContent
            };

            return Task.FromResult(httpResponse);
        }
    }
}
