﻿using System.Linq;

namespace Telegram.Bot.Tests.Integ.Common
{
    public class TestConfigurations
    {
        public string ApiToken { get; set; }

        public string AllowedUserNames { get; set; }

        public string[] AllowedUserNamesArray
        {
            get
            {
                if (_allowedUsers == null)
                {
                    _allowedUsers = AllowedUserNames
                        .Split(',')
                        .Select(n => n.Trim())
                        .ToArray();
                }
                return _allowedUsers;
            }
        }

        public string PaymentProviderToken { get; set; }

        public string PrivateChatId { get; set; }

        public string SuperGroupChatId { get; set; }

        public string RegularMemberUserId { get; set; }

        public string RegularMemberUserName { get; set; }

        public string RegularMemberPrivateChatId { get; set; }

        private string[] _allowedUsers;
    }
}
