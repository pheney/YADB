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
using static YADB.Common.Constants;
using System.Threading;

namespace YADB.Modules
{
    [Name("Admin Commands")]
    [RequireContext(ContextType.DM)]
    public class PuppetModule : ModuleBase<SocketCommandContext>
    {
        #region Attract mode / autonomous start conversation mode

        //  minutes
        private static float attractDelayMin = 2;
        private static float attractDelayMax = 9;

        [Command("#SetDelay"), Alias("#sd")]
        [Remarks("Set the mininum and maximum delay (in minutes) for the attract message")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task SetAttractDelay(float min, float max)
        {
            //  handle all the stupid requests
            string message, details;
            
            //  negative values
            if (min < 0 || max < 0)
            {
                message = "Delay Change Failed";
                details = "Details: values must be greater than zero.";
                await PMReportIssueAsync(message, details, MessageSeverity.CriticalOrFailure);
                return;
            }

            //  max < min
            if (max < min)
            {
                message = "Delay Change Failed";
                details = "Details: maximum value must be greater than minimum value.";
                await PMReportIssueAsync(message, details, MessageSeverity.CriticalOrFailure);
                return;
            }

            //  very short minimum value
            if (min < 3f/60)
            {
                message = "Delay Change Failed";
                details = "Details: minimum value must be greater than 0.05 (3 seconds)";
                await PMReportIssueAsync(message, details, MessageSeverity.CriticalOrFailure);
                return;
            }

            //  update the values
            PuppetModule.attractDelayMin = min;
            PuppetModule.attractDelayMax = max;
            await ReportAttractDelay();
        }

        [Command("#SetDelay"), Alias("#sd")]
        [Remarks("Show the current mininum and maximum delay (in minutes) for the attract message")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task ReportAttractDelay()
        {
            string message = "Delay set: {min} - {max} (minutes)";
            message = message
                .Replace("{min}", PuppetModule.attractDelayMin.ToString())
                .Replace("{max}", PuppetModule.attractDelayMax.ToString());
            string details = "Attract status: " + (PuppetModule.attractEnabled ? "enabled." : "disabled");
            MessageSeverity severity = attractEnabled ? MessageSeverity.Success : MessageSeverity.CriticalOrFailure;
            await PMReportIssueAsync(message, details, severity);
        }
        
        private static bool attractEnabled;

        /// <summary>
        /// 2017-8-19
        /// This starts and stops the periodic conversation prompts.
        /// </summary>
        [Command("#EnableAttract"), Alias("#Ea")]
        [Remarks("Enable / disable unprompted bot conversation starters")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task EnableAttractMode(bool enabled)
        {
            //  do nothing if the setting was not changed
            if (attractEnabled == enabled)
            {
                await ReportAttractstatus();
                return;
            }
            //  update the setting
            attractEnabled = enabled;
            await ReportAttractstatus();

            Thread attractThread = new Thread(AttractLoop);
            attractThread.Start();
        }

        [Command("#EnableAttract"), Alias("#Ea")]
        [Remarks("Enable / disable unprompted bot conversation starters")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task ReportAttractstatus()
        {
            //  Send feedback message to user
            if (attractEnabled)
            {
                string message = "Attract enabled.";
                string details = "Delay: {min} - {max} (minutes)";
                details = details
                    .Replace("{min}", PuppetModule.attractDelayMin.ToString())
                    .Replace("{max}", PuppetModule.attractDelayMax.ToString());
                await PMReportIssueAsync(message, details, MessageSeverity.Success);

                if (!Chat.ChatStatus) await ReportChatStatus();
            }
            else
            {
                await PMFeedbackAsync("Attract disabled", MessageSeverity.CriticalOrFailure);
            }
        }

        private void AttractLoop() { 
            while (attractEnabled && Chat.ChatStatus)
            {
                int delayMillis = (int)(Constants.rnd.Range(attractDelayMin, attractDelayMax) * 60 * 1000);
                Task.Delay(delayMillis);
                StartConvo();
            }
        }

        #endregion
        #region Cleverbot integration

        [Command("#EnableChat"), Alias("#Ec")]
        [Remarks("Enable / disable bot conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task EnableChat(bool enabled)
        {
            await Chat.EnableChat(enabled);

            //  Send feedback message to user
            await ReportChatStatus();

            //  Activate attract mode
            await EnableAttractMode(enabled);
        }

        [Command("#EnableChat"), Alias("#Ec")]
        [Remarks("Enable / disable bot conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task ReportChatStatus()
        {
            //  Send feedback message to user
            if (Chat.ChatStatus)
            {
                await PMFeedbackAsync("Chat enabled: " + Chat.ChatStatus, MessageSeverity.Success);
            }
            else
            {
                await PMFeedbackAsync("Chat enabled: " + Chat.ChatStatus, MessageSeverity.CriticalOrFailure);
            }
        }

        #endregion
        #region Private Helpers

        /// <summary>
        /// 2017-8-18
        /// Always PMs to the current user.
        /// </summary>
        private async Task PMFeedbackAsync (string info, MessageSeverity severity = MessageSeverity.Info)
        {
            var builder = new EmbedBuilder()
            {
                Color = SeverityToColor(severity),
                Description = info
            };

            //  Send problem message via direct-message to the user            
            var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }

        /// <summary>
        /// 2017-8-19
        /// </summary>
        private async Task PMReportIssueAsync(string summary, string details, MessageSeverity severity)
        {
            string info = summary + "\n" + details;
            await PMFeedbackAsync(info, severity);
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

        #endregion
        #region Inspirobot integration

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
                } else
                {
                    await ReplyAsync(response);
                }
            }
            catch (WebException e)
            {
                response = "There was a problem with the inspirobot request.\n"
                    +"\nException Thrown: " + e.Status.ToString();
                string details = "Message: " +e.Message;
                await PMReportIssueAsync(response, details, MessageSeverity.CriticalOrFailure);
            }
        }

        #endregion
        #region Puppet / Announcements

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
                        || userName.Equals(myMention, StringComparison.OrdinalIgnoreCase));
                await StartConvo(userName);
                return;
            }
            
            string response;
            await Chat.GetReply(Constants.Greetings.Random(), out response);

            //  NOTE: this code is copied from IntroductionAsync() 
            //  and could probably be consolidated.

            DiscordSocketClient _client = Context.Client;

            //  Main channel Id is always the same as the guild Id
            //  according to Gavin.
            ulong mainChannelId = _client.Guilds.First().Id;
            var destinationChannel = _client.GetChannel(mainChannelId) as IMessageChannel;
            string feedback, details;

            if (destinationChannel == null)
            {
                //  User is not in a public channel

                //  Create feedback message for user 
                feedback = "Failure: Failed to send message \"{message}\"";
                feedback = feedback.Replace("{message}", response);
                details = "Reason: Probably because {channel} channel is not public.";
                details = details.Replace("{channel}", destinationChannel.Name);
                await PMReportIssueAsync(feedback, details, MessageSeverity.CriticalOrFailure);
            }
            else
            {
                //  Send conversation starter
                await destinationChannel.SendMessageAsync(userName + ", " + response);

                //  Create feedback message for user
                feedback = "Message sent: \"{message}\"";
                feedback = feedback.Replace("{message}", response);
                details = "To user: {user}";
                details = details.Replace("{user}", userName);
                await PMReportIssueAsync(feedback, details, MessageSeverity.Success);
            }
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
                string feedbackMessage = "Message sent: {message}";
                feedbackMessage = feedbackMessage.Replace("{message}", ellipsisMessage);

                string details = "On channel: {channel}";
                details = details.Replace("{channel}", channelName);

                await PMReportIssueAsync(feedbackMessage, details, MessageSeverity.Success);
            }
            else
            {
                string feedbackMessage = "Failed to send message: \"{message}\"";
                feedbackMessage = feedbackMessage.Replace("{message}", ellipsisMessage);

                string details = "Reason: \"{channel}\" is not a text channel";
                details = details.Replace("{message}", ellipsisMessage);

                await PMReportIssueAsync(feedbackMessage, details, MessageSeverity.CriticalOrFailure);
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

                await PMFeedbackAsync(noChannelMessage, MessageSeverity.Warning);
            }
            else
            {
                await Announce(destinationChannel.Id, message);
            }
        }

        #endregion
    }
}
