using Discord.Commands;
using Discord.WebSocket;
using YADB.Preconditions;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using System.Collections.Generic;
using System;

namespace YADB.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("kick")]
        [Remarks("Kick the specified user.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            await ReplyAsync("cya " + user.Mention + " :wave:");
            await user.KickAsync();
        }

        [Command("Announce"), Alias("Ann")]
        [Remarks("Make an announcement on another channel")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Announce(string channel, [Remainder]string message)
        {
            await Program.AsyncConsoleMessage("Announce, enter", System.ConsoleColor.Cyan);

            SocketGuild guild = Context.Guild;
            await Program.AsyncConsoleMessage("SocketGuild guild = Context.Guild", System.ConsoleColor.Cyan);

            IReadOnlyCollection<SocketGuildChannel> channels = null;
            try
            {
                channels = guild.Channels;
                await Program.AsyncConsoleMessage("var channels = guild.Channels", System.ConsoleColor.Cyan);
            } catch (Exception e)
            {
                await Program.AsyncConsoleMessage(e.StackTrace.Replace("   at", "\n   at"), System.ConsoleColor.Cyan);
            }

            var destinationChannel = channels.Where(c => c.Name.Equals(channel, System.StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            await Program.AsyncConsoleMessage("var destinationChannel = channe...", System.ConsoleColor.Cyan);

            IMessageChannel messageChannel = destinationChannel as IMessageChannel;
            await Program.AsyncConsoleMessage("IMessageChannel messageChannel = desti...", System.ConsoleColor.Cyan);
            
            if (messageChannel != null)
            {
                await Program.AsyncConsoleMessage("Announce, step 2", System.ConsoleColor.Cyan);
                // this is a text channel
                await messageChannel.SendMessageAsync(message);

                //  sends message to current channel
                string confirmMessage = "Sent \"{message}\" to channel \"{channel}\"";
                confirmMessage
                    .Replace("{message}", message)
                    .Replace("{channel}", channel);

                //  Send confirmation message via direct-message to the user            
                var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync(confirmMessage);
            }
            else
            {
                await Program.AsyncConsoleMessage("Announce, step 3", System.ConsoleColor.Cyan);
                string errorMessage = "Could not send message \"{message}\" because \"{channel}\" is not a text channel";
                errorMessage
                    .Replace("{message}", message.Substring(0, 9) + "...")
                    .Replace("{channel}", channel);

                //  Send error message via direct-message to the user     
                var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync(errorMessage);
            }
            await Program.AsyncConsoleMessage("Announce, step 4", System.ConsoleColor.Cyan);
        }
    }
}
