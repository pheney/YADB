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
using YADB.Services;

namespace YADB.Modules
{
    [Name("Admin Commands")]
    [RequireContext(ContextType.DM)]
    public class PuppetModule : ModuleBase<SocketCommandContext>
    {
        [Command("#Enable"), Alias("#Enabled", "#En")]
        [Remarks("Enable / disable bot conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task EnableChat(bool enabled)
        {
            await Chat.EnableChat(enabled);

            //  Create feedback message for user
            await SendFeedback("Chat enabled: " + enabled, Constants.SlateYellow);
        }
        
        /// <summary>
        /// 2017-8-18
        /// Always PMs to the current user.
        /// </summary>
        private async Task SendFeedback (string info, Color color)
        {
            var builder = new EmbedBuilder()
            {
                Color = color,
                Description = info
            };

            //  Send problem message via direct-message to the user            
            var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }

        [Command("#StartConvo"), Alias("#sc")]
        [Remarks("Prompt bot to start a conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task StartConvo([Remainder]string userName = null)
        {
            //  select a random user name from main channel
            if (userName == null)
            {
                DiscordSocketClient client = Context.Client;
                SocketGuild mainGuild = client.Guilds.First();
                string myUsername = Context.User.Username;
                string myMention = Context.User.Mention;
                do
                {
                    SocketGuildUser user = GetRandomActiveUser(mainGuild);
                    userName = Constants.rnd.NextDouble() < 0.5d ? user.Username : user.Mention;
                } while (userName.Equals(myUsername, StringComparison.OrdinalIgnoreCase)
                        || userName.Equals(myMention,StringComparison.OrdinalIgnoreCase));
                await StartConvo(userName);
                return;
            }

            await EnableChat(true);
            string response;
            await Chat.GetReply(Constants.Greetings.Random(), out response);
            response = userName + ", " + response;

            //  NOTE: this code is copied from IntroductionAsync() 
            //  and could probably be consolidated.

            DiscordSocketClient _client = Context.Client;

            //  Main channel Id is always the same as the guild Id
            //  according to Gavin.
            ulong mainChannelId = _client.Guilds.First().Id;
            var destinationChannel = _client.GetChannel(mainChannelId) as IMessageChannel;
            string feedback;
            Color color;

            if (destinationChannel == null)
            {
                //  User is not in a public channel

                //  Send message via direct-message to the user   
                feedback = "Unable to send message. Probably because the channel {channel} is not public.";
                feedback.Replace("{channel}", destinationChannel.Name);
                color = Constants.SlateRed;
            }
            else
            {
                //  Send conversation starter
                await destinationChannel.SendMessageAsync(response);
                feedback = "Sent message: \"{message}\"\n"
                + "To user: {user}";
                feedback = feedback
                    .Replace("{message}", response)
                    .Replace("{user}", userName);
                color = Constants.SlateGreen;
            }

            //  Create feedback message for user
            await SendFeedback(feedback, color);
        }

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private List<SocketGuildChannel> GetTextChannels(SocketGuild guild)
        {
            return guild.Channels.Where(x => x is SocketTextChannel).ToList();
        }

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private List<SocketGuildChannel> GetVoiceChannels(SocketGuild guild)
        {
            return guild.Channels.Where(x => x is SocketVoiceChannel).ToList();
        }

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private string ChannelTypeToString(IChannel channel)
        {
            if (channel is SocketVoiceChannel) return "Voice";
            if (channel is SocketTextChannel) return "Text";
            return "Unknown";
        }

        /// <summary>
        /// 2017-8-18
        /// </summary>
        private SocketGuildUser GetRandomActiveUser(SocketGuild guild)
        {
            SocketGuildChannel mainChannel = GetTextChannels(guild).First();
            IReadOnlyCollection<SocketGuildUser> userList = mainChannel.Users;
            int count = userList.Count();
            int index = Constants.rnd.Next(count);
            return userList.ElementAt(index);
        }

        [Command("#Inspire"), Alias("#Inspire me", "#In")]
        [Remarks("Post a fresh image from Inspirobot in the current channel")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task Inspire(string channelName = null)
        {
            string response = "";
            string url = "http://www.inspirobot.me/api?generate=true";

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(url);
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
                if (channelName != null)
                {
                    await Announce(channelName, response);
                    return;
                }
            }
            catch (WebException e)
            {
                response = "There was a problem with the inspirobot request.\n\n" + e.ToString();
            }
            await ReplyAsync(response);
        }

        [Command("#Announce"),Alias("#An")]
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

                await SendFeedback(confirmMessage, Constants.SlateGreen);
            }
            else
            {
                string errorMessage = "Failed to send message: \"{message}\"\n\"{channel}\" is not a text channel";
                errorMessage = errorMessage
                    .Replace("{message}", ellipsisMessage)
                    .Replace("{channel}", channelName);

                await SendFeedback(errorMessage, Constants.SlateRed);
            }
        }

        [Command("#Announce"),Alias("#An")]
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

                foreach (var c in channels.OrderBy(x => x.Name))
                {
                    noChannelMessage += "\n  {name} ({type})"
                        .Replace("{name}", c.Name)
                        .Replace("{type}", ChannelTypeToString(c));
                }

                await SendFeedback(noChannelMessage, Constants.SlateYellow);
            }
            else
            {
                await Announce(destinationChannel.Id, message);
            }
        }
    }
}
