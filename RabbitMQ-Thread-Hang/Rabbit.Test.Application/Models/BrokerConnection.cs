/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using System.Text;
namespace Rabbit.Test.Application.Models
{
    public class BrokerConnection
    {
        public string Host { get; set; }
        public string UserName { get; set; }

        public string Password { get; set; }

        public int RequestedHeartbeat { get; set; }
        public int Timeout { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; } = false;
        public string ConnectionName { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendIfNotNull(Host, "host");
            sb.AppendIfNotNull(Password, "password");
            sb.AppendIfNotNull(UserName, "username");
            sb.AppendIfNotNull(RequestedHeartbeat, "requestedHeartbeat");
            sb.AppendIfNotNull(Timeout, "timeout");
            sb.AppendIfNotNull(ConnectionName, "name");
            return sb.ToString().TrimLastCharacter();
        }

      
    }
    public static class StringBuilderExtension
    {
        public static void AppendIfNotNull(this StringBuilder builder, string text, string phrase)
        {
            if (!string.IsNullOrEmpty(text))
            {
                builder.Append($"{phrase}={text};");
            }

        }

        public static void AppendIfNotNull(this StringBuilder builder, int text, string phrase)
        {
            if (text > 0)
            {
                builder.Append($"{phrase}={text};");
            }
        }

        public static void AppendIfNotNull(this StringBuilder builder, bool text, string phrase)
        {
            builder.Append($"{phrase}={text};");
        }

        public static string TrimLastCharacter(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            else
            {
                return text.TrimEnd(text[text.Length - 1]);
            }
        }
    }
}
