using Discord.Commands;
using Discord.WebSocket;

namespace YADB.Common
{
    public static class DebugUtils
    {
        public static string ContentsToString(SocketCommandContext socketCommandContext)
        {
            string result = "SocketCommandContext:";
            result += "\nChannel: " + socketCommandContext.Channel.Name + " : " + socketCommandContext.Channel.Id;
            result += "\nClient: " + socketCommandContext.Client != null;
            result += "\nGuild: " + socketCommandContext.Guild != null;
            result += "\nMessage: {\n" + ContentsToString(socketCommandContext.Message) + "\n}";
            result += "\nUser: " + socketCommandContext.User != null;
            return result;
        }

        public static string ContentsToString(SocketUserMessage socketUserMessage)
        {
            string result = "SocketUserMessage:";
            result += "\nAuthor: {\n" + ContentsToString(socketUserMessage.Author) + "\n}";
            result += "\nContent:" + socketUserMessage.Content;
            return result;
        }

        public static string ContentsToString(SocketUser socketUser)
        {
            string result = "SocketUser:";
            result += "\nUsername: " + socketUser.Username + " : " + socketUser.Id;
            result += "\nIsBot: " + socketUser.IsBot;
            return result;
        }
    }
}
