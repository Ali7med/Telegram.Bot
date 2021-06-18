namespace Telegram.Bot.Tests.Integ.Framework
{
    public class TestConfigurations
    {
        public TestConfigurations(string apiToken,
                                  string allowedUserNames,
                                  string superGroupChatId,
                                  string channelChatId,
                                  string paymentProviderToken,
                                  string regularGroupMemberId)
        {
            ApiToken = apiToken;
            AllowedUserNames = allowedUserNames;
            SuperGroupChatId = superGroupChatId;
            ChannelChatId = channelChatId;
            PaymentProviderToken = paymentProviderToken;
            RegularGroupMemberId = regularGroupMemberId;
        }

        public string ApiToken { get; set; }

        public string AllowedUserNames { get; set; }

        public string SuperGroupChatId { get; set; }

        public string ChannelChatId { get; set; }

        public string PaymentProviderToken { get; set; }

        public long? TesterPrivateChatId { get; set; }

        public long? StickerOwnerUserId { get; set; }

        public string RegularGroupMemberId { get; set; }
    }
}
