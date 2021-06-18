using System;
using Telegram.Bot.Types;
using Xunit;

namespace Telegram.Bot.Tests.Unit
{
    public class ChatIdTests
    {
        [Theory]
        [InlineData("@username")]
        [InlineData("@UserName")]
        [InlineData("@User1")]
        [InlineData("@12345")]
        [InlineData("12345")]
        [InlineData("0")]
        [InlineData("999999999999999")]
        [InlineData("@99999999999999999999999999999999")]
        public void Valid_User_Name(string userName)
        {
            var chatId = new ChatId(123);
            var chatId = new ChatId(userName);

            //check int & long
            Assert.Null(chatId.Username);
            Assert.Equal(123, chatId.Identifier);

            chatId = new ChatId(123L);
            Assert.Null(chatId.Username);
            Assert.Equal(123L, chatId.Identifier);

            // check string values
            chatId = new ChatId(123.ToString());
            Assert.Null(chatId.Username);
            Assert.Equal(123, chatId.Identifier);

            chatId = new ChatId(123L.ToString());
            Assert.Null(chatId.Username);
            Assert.Equal(123L, chatId.Identifier);

            chatId = new ChatId("@valid_username");
            Assert.Equal("@valid_username", chatId.Username);
            Assert.Equal(0, chatId.Identifier);

            Assert.Throws<ArgumentException>(() => new ChatId("username"));
            Assert.Equal(chatId, userName);
        }


        [Fact]
        public void Null_User_Name()
        {
            //int
            Assert.Equal("123", new ChatId("123").ToString());
            Assert.Equal("123", new ChatId(123).ToString());

            //long
            Assert.Equal("123456789012", new ChatId((123456789012)).ToString());
            Assert.Equal("123456789012", new ChatId("123456789012").ToString());
            Assert.Throws<ArgumentNullException>(() => new ChatId(null));
        }

            //username
            Assert.Equal("@valid_username", new ChatId("@valid_username").ToString());
        }

        [Theory]
        [InlineData("username")]
        [InlineData("@u")]
        [InlineData("@User")]
        [InlineData("@1234")]
        [InlineData("999999999999999999999999")]
        [InlineData("@999999999999999999999999999999999")]
        public void Invalid_User_Name(string userName)
        {
            //with Identifier
            var chatId = new ChatId(123);
            Assert.True(chatId.Equals(123));
            Assert.False(123.Equals(chatId)); // to be aware
            Assert.True(chatId == 123);
            Assert.True(123 == chatId);

            chatId = new ChatId("123");
            Assert.True(chatId.Equals(123));
            Assert.False(123.Equals(chatId)); // to be aware
            Assert.True(chatId == 123);
            Assert.True(123 == chatId);

            //with username
            chatId = new ChatId("@username");
            Assert.True(chatId.Equals("@username"));
            Assert.True("@username".Equals(chatId));
            Assert.True(chatId == "@username");
            Assert.True("@username" == chatId);

            //with other ChatId
            Assert.Equal(chatId, chatId);
            Assert.Equal(new ChatId(123), new ChatId(123));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatId(userName));
        }
    }
}
