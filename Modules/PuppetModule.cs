using Discord.Commands;
using Discord.WebSocket;
using YADB.Preconditions;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using System.Collections.Generic;
using System;
using YADB.Common;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace YADB.Modules
{
    [Name("Admin Commands")]
    [RequireContext(ContextType.DM)]
    public class PuppetModule : ModuleBase<SocketCommandContext>
    {
        [Command("#Inspire"), Alias("#inspire me")]
        [Remarks("Post a fresh image from Inspirobot in the current channel")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task Inspire()
        {
            string response = "";
            string url = "http://www.inspirobot.me/api?generate=true";

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(url);
            //webRequest.ContentType = "application/json"
            webRequest.ContentType = "application/text";

            //  Create an empty response
            WebResponse webResp = null;

            try
            {
                //  Execute the request and put the result into response
                webResp = webRequest.GetResponse();
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(webResp.GetResponseStream(), encoding))
                {
                    response = await reader.ReadToEndAsync();
                }
            }
            catch (WebException e)
            {
                response = "There was a problem with the inspirobot request.\n\n" + e.ToString();
            }
            await ReplyAsync(response);
        }

        [Command("#Announce")]
        [Remarks("PM the bot to make an announcement on another channel, using channel ID")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Announce(ulong channelId, [Remainder]string message)
        {
            DiscordSocketClient client = Context.Client;
            IMessageChannel messageChannel = client.GetChannel(channelId) as IMessageChannel;
            string channelName = messageChannel.Name;

            int ellipsisLength = Math.Min(24, message.Length);
            string ellipsisMessage = message.Substring(0, ellipsisLength) + "...";

            if (messageChannel != null)
            {
                //  Channel is a valid text channel.
                //  Send the messages to the channel.
                await messageChannel.SendMessageAsync(message);

                //  Create confirmation message for user
                string confirmMessage = "Message sent: {message}\nOn channel: {channel}";
                confirmMessage = confirmMessage
                    .Replace("{message}", ellipsisMessage)
                    .Replace("{channel}", channelName);

                var builder = new EmbedBuilder()
                {
                    Color = Constants.SlateGreen,
                    Description = confirmMessage
                };

                //  Send confirmation message via direct-message to the user            
                var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                string errorMessage = "Failed to send message: \"{message}\"\n\"{channel}\" is not a text channel";
                errorMessage = errorMessage
                    .Replace("{message}", ellipsisMessage)
                    .Replace("{channel}", channelName);

                var builder = new EmbedBuilder()
                {
                    Color = Constants.SlateYellow,
                    Description = errorMessage
                };

                //  Send confirmation message via direct-message to the user            
                var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync("", false, builder.Build());
            }
        }

        [Command("#Announce")]
        [Remarks("PM the bot to make an announcement on another channel, using channel name")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Announce(string channelName, [Remainder]string message)
        {
            DiscordSocketClient client = Context.Client;
            IReadOnlyCollection<SocketGuildChannel> channels = client.Guilds.First().Channels;
            var destinationChannel = channels.Where(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (destinationChannel == null)
            {
                //  Channel name was not found.

                //  Create feedback message for user
                string noChannelMessage = "Channel not found: \"{channel}\""
                    + "\nDid you mean one of these?\n";
                noChannelMessage = noChannelMessage
                    .Replace("{channel}", channelName);

                foreach (var c in channels) noChannelMessage += "\n  " + c.Name;

                var builder = new EmbedBuilder()
                {
                    Color = Constants.SlateYellow,
                    Description = noChannelMessage
                };

                //  Send problem message via direct-message to the user            
                var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync("", false, builder.Build());
            }
            else
            {
                await Announce(destinationChannel.Id, message);
            }
        }
    }
}
