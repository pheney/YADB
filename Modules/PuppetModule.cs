using Discord.Commands;
using Discord.WebSocket;
using YADB.Preconditions;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using System.Collections.Generic;
using System;
using YADB.Common;

namespace YADB.Modules
{
    [Name("Puppet")]
    [RequireContext(ContextType.DM)]
    public class PuppetModule : ModuleBase<SocketCommandContext>
    {
        [Command("Announce"), Alias("A")]
        [Remarks("PM the bot to make an announcement on another channel")]
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

        [Command("Announce"), Alias("Ann")]
        [Remarks("PM the bot to make an announcement on another channel")]
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

                //  Send confirmation message via direct-message to the user            
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
