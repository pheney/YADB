using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YADB.Common;
using YADB.Preconditions;
using YADB.Services;
using static YADB.Common.Constants;

namespace YADB.Modules
{
    [Name("Admin Commands")]
    [RequireContext(ContextType.DM)]
    public class PuppetModule : ModuleBase<SocketCommandContext>
    {
        #region Status report

        [Command("#Status"), Alias("#s")]
        [Remarks("Report the current state of bot's internal systems")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task StatusReport()
        {
            int delayMillis = 100;
            await ChatStatus(false);

            await Task.Delay(delayMillis);
            await AttractStatus(false);

            await Task.Delay(delayMillis);
            await GameModule.RollingStatus(Context);

            await Task.Delay(delayMillis);
            await GameModule.HaddawayStatus(Context);

            await Task.Delay(delayMillis);
            await GameModule.EightBallStatus(Context);
        }

        #endregion
        #region Attract mode / autonomous start conversation mode

        //  minutes
        private static int attractDelayMin = 2;
        private static int attractDelayMax = 9;
        
        private async Task SetAttractDelay(int minimum, int maximum)
        {
            int min = Math.Min(minimum, maximum);
            int max = Math.Max(minimum, maximum);
            
            string message, details;

            //  negative values
            if (min < 1 || max < 1)
            {
                message = "Delay Change Failed";
                details = "Details: values must be greater than one.";
                await PMReportIssueAsync(message, details, MessageSeverity.CriticalOrFailure);
                return;
            }            

            //  update the values
            PuppetModule.attractDelayMin = min;
            PuppetModule.attractDelayMax = max;

            await ReportAttractDelay();
        }

        private async Task ReportAttractDelay(bool changed = true)
        {
            string message = "Delay set: {min} - {max} (minutes)";
            message = message
                .Replace("{min}", PuppetModule.attractDelayMin.ToString())
                .Replace("{max}", PuppetModule.attractDelayMax.ToString());
            string details = "(Attract currently " + (PuppetModule.attractEnabled ? "enabled." : "disabled")+")";
            MessageSeverity severity = attractEnabled ? MessageSeverity.Success : MessageSeverity.CriticalOrFailure;
            if (!changed) severity = MessageSeverity.Info;
            await PMReportIssueAsync(message, details, severity);
        }

        private static bool attractEnabled;
        private static Thread attractThread;

        /// <summary>
        /// 2017-8-19
        /// This starts and stops the periodic conversation prompts.
        /// </summary>
        [Command("#EnableAttract"), Alias("#Ea")]
        [Remarks("Enable / disable unprompted bot conversation starters")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task SetAttractMode(params string[] settings)
        {
            int min, max;
            bool enabled;

            switch (settings.Length)
            {
                case 0: //  Request status
                    await EnableAttractMode();
                    return;
                case 1: //  Enable / Disable
                    if (bool.TryParse(settings[0], out enabled))
                    {
                        await EnableAttractMode(enabled);
                        return;
                    }
                    break;
                case 2: //  Change delay times
                    if (int.TryParse(settings[0], out min) &&
                        int.TryParse(settings[1], out max))
                    {
                        await SetAttractDelay(min, max);
                        return;
                    }
                    break;
                case 3: //  Set delay times and enable or disable
                    if (int.TryParse(settings[0], out min) &&
                        int.TryParse(settings[1], out max)&&
                        bool.TryParse(settings[2], out enabled))
                    {
                        await SetAttractDelay(min, max);
                        await EnableAttractMode(enabled);
                        return;
                    }
                    break;
                default: // Do nothing
                    await PMReportIssueAsync("Usage:", "#ea\n#ea true|false\n#ea min max\n#ea min max true|false", MessageSeverity.Warning);
                    break;
            }   
        }

        private async Task EnableAttractMode(bool? enabled = null)
        {
            if (enabled != null)
            {
                bool status = (bool)enabled;

                //  Before doing anything at all, ensure CHAT is enabled
                if (status && !Chat.ChatStatus)
                {
                    string message = "There is no point to enable AttractMode when "
                        + "chat is disabled. Enable chat first.";
                    await PMFeedbackAsync(message, MessageSeverity.Warning);
                    return;
                }

                //  Do nothing when the setting is not changed
                if (attractEnabled != status)
                {
                    //  Change the status
                    attractEnabled = status;
                    if (attractEnabled)
                    {
                        //  Start loop thread
                        attractThread = new Thread(async () => await AttractLoop());
                        attractThread.Start();
                    }
                    await AttractStatus(true);
                    return;
                }
            }

            //  show current status
            await AttractStatus(false);
        }

        /// <summary>
        /// By default, displays color as "success" when active,
        /// and "failure" when disabled.
        /// </summary>
        /// <param name="changed">Use true to set color as "Info"</param>
        private async Task AttractStatus(bool changed = true)
        {
            //  Send feedback message to user
            string message = "Attract status: {status}";
            message = message.Replace("{status}", attractEnabled ? "enabled" : "disabled");

            string details = "Delay: {min} - {max} minutes";
            details = details
                .Replace("{min}", attractDelayMin.ToString())
                .Replace("{max}", attractDelayMax.ToString());

            //  when active, show how long it's been since the last message exchange
            if (attractEnabled)
            {
                details += "\nLast exchange: ~{min} min ago";
                details = details.Replace("{min}", string.Format("{0:N2}", Chat.LastMessageInterval.TotalMinutes));
            }

            MessageSeverity severity = attractEnabled ? MessageSeverity.Success : MessageSeverity.CriticalOrFailure;
            if (!changed) severity = MessageSeverity.Info;

            await PMReportIssueAsync(message, details, severity);
        }

        /// <summary>
        /// 2017-8-20
        /// Used on a separate Thread.
        /// </summary>
        private async Task AttractLoop()
        {
            while (attractEnabled && Chat.ChatStatus)
            {
                int delayMillis = (int)(Constants.rnd.Range(attractDelayMin, attractDelayMax) * 60 * 1000);
                await Task.Delay(delayMillis);

                //  When the last message exchanged exceeds
                //  the 'delay' period, then prompt for a conversation.
                if (Chat.LastMessageInterval.TotalMilliseconds > delayMillis)
                {
                    bool forceNick = false;
                    if (rnd.NextDouble() < 0.05d || forceNick)
                    {
                        //  change nickname
                        SocketSelfUser bot = Context.Client.CurrentUser;
                        ulong botId = bot.Id;
                        SocketGuild guild = Context.Client.Guilds.First();
                        SocketGuildUser botUser = guild.GetUser(botId);
                        string nickname = botUser.Nickname;

                        //  get the main channel of the guild
                        SocketGuildChannel guildChannel = guild.GetChannel(guild.Id);
                        ITextChannel textChannel = guildChannel as ITextChannel;

                        await textChannel.SendMessageAsync(nickname + ", #del you");
                        await textChannel.SendMessageAsync(nickname + ", #botnick");
                    }
                    else
                    {
                        //  try to start a conversation with someone
                        await StartConvo();
                    }
                }
            }
        }

        #endregion
        #region Cleverbot integration

        [Command("#EnableChat"), Alias("#Ec")]
        [Remarks("Enable / disable bot conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task EnableChat([Remainder]string enabled = null)
        {
            bool result, status;
            result = bool.TryParse(enabled, out status);

            if (result)
            {
                await Chat.EnableChat(status);
                await ChatStatus(true);
            }
            else
            {
                //  just report current status
                await ChatStatus(false);
            }
        }

        private async Task ChatStatus(bool changed = true)
        {
            //  Send feedback message to user
            string status = "Chat status: {status}";
            status = status.Replace("{status}", Chat.ChatStatus ? "enabled" : "disabled");
            MessageSeverity severity = Chat.ChatStatus ? MessageSeverity.Success : MessageSeverity.CriticalOrFailure;
            if (!changed) severity = MessageSeverity.Info;
            await PMFeedbackAsync(status, severity);
        }

        #endregion
        #region Private Helpers

        /// <summary>
        /// 2017-8-18
        /// Always PMs to the current user.
        /// </summary>
        private async Task PMFeedbackAsync(string info, MessageSeverity severity = MessageSeverity.Info)
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
            IReadOnlyCollection<SocketGuildUser> userList = mainChannel.Users.Where(x => x.Status.Equals(UserStatus.Online)).ToList();
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
                    await AnnounceOnChannelName(channelName, response);
                }
                else
                {
                    await ReplyAsync(response);
                }
            }
            catch (WebException e)
            {
                response = "There was a problem with the inspirobot request.\n"
                    + "\nException Thrown: " + e.Status.ToString();
                string details = "Message: " + e.Message;
                await PMReportIssueAsync(response, details, MessageSeverity.CriticalOrFailure);
            }
        }

        #endregion
        #region Start Conversation

        [Command("#StartConvo"), Alias("#sc")]
        [Remarks("Prompt bot to start a conversation")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task StartConvo([Remainder]string userName = null)
        {
            DiscordSocketClient client = Context.Client;
            SocketGuild guild = client.Guilds.Random();
            SocketTextChannel channel = guild.TextChannels.Random();

            //  select a random user name from main channel
            if (userName == null)
            {
                //  Limit the number of attempts to get a random user.
                //  Re-select when the selected user is the bot itself.

                SocketGuildUser recipientUser = null;
                ulong botId = client.CurrentUser.Id;
                int maxAttempts = 10;
                for (int i = 0; i < maxAttempts; i++)
                {
                    recipientUser = GetRandomActiveUser(guild);
                    if (recipientUser.Id != botId) break;
                };

                if (recipientUser.Id == botId)
                {
                    await PMFeedbackAsync("Nobody found to converse with.", MessageSeverity.CriticalOrFailure);
                }
                else
                {
                    string name = string.IsNullOrWhiteSpace(recipientUser.Nickname) ? recipientUser.Username : recipientUser.Nickname;
                    await StartConvo(name);
                }
                return;
            }

            //  Generate a greeting
            string response;
            switch (rnd.Next(9))
            {
                case 0:
                    response = "Anybody need a magic **#8ball**?";
                    break;
                case 1:
                    response = "Who wants to go on an _adventure_? I have a #quest all ready to go...";
                    break;
                case 2:
                    response = "Let's play the Haddaway game! You start.";
                    break;
                case 3:
                    response = "#rickroll";
                    break;
                default:
                    await Chat.GetReply(Constants.Greetings.Random(), out response);
                    break;
            }
            
            //  Send conversation starter
            await channel.SendMessageAsync(userName + ", " + response);

            //  Create feedback message for user
            string feedback, details;
            feedback = "Message sent: \"{message}\"";
            feedback = feedback.Replace("{message}", response);
            details = "To user: {user}";
            details = details.Replace("{user}", userName);
            await PMReportIssueAsync(feedback, details, MessageSeverity.Success);       
        }

        #endregion
        #region Puppet / Announcements

        [Command("#Announce"), Alias("#An", "#a", "#say")]
        [Remarks("PM the bot to make an announcement on another channel, using channel name or channel ID")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Announce([Remainder]string message)
        {
            //  When the first word is a ulong, treat it as a channel ID.
            //  Otherwise, treat it as a channel name.
            string[] words = message.Split(' ');
            ulong channelId;
            bool parseReslt = ulong.TryParse(words[0], out channelId);
            if (parseReslt)
            {
                //  Found a potential Channel ID
                await AnnounceOnChannelId(channelId, words.JoinWith(" ", 1));
                return;
            }

            //  assume the first word is a channel name
            await AnnounceOnChannelName(words[0], words.JoinWith(" ", 1));
        }

        private async Task AnnounceOnChannelId(ulong channelId, [Remainder]string message)
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

        private async Task AnnounceOnChannelName(string channelName, [Remainder]string message)
        {
            DiscordSocketClient client = Context.Client;
            List<string> channels = new List<string>();
            SocketGuildChannel destinationChannel = null;

            //  Find the channel named as the parameter among all the guilds (servers)
            //  the bot has access to.
            foreach (SocketGuild guild in client.Guilds)
            {
                foreach (SocketGuildChannel guildChannel in guild.TextChannels)
                {
                    if (guildChannel.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                    {
                        destinationChannel = guildChannel;
                        break;
                    }
                    else
                    {
                        string channelDisplay = string.Format("{0} --> {1} : {2}", 
                            guildChannel.Name,
                            "(" + guild.Name + ")",
                            ChannelTypeToString(guildChannel));
                        channels.Add(channelDisplay);
                    }
                }
                if (destinationChannel != null) break;
            }

            if (destinationChannel == null)
            {
                ////  Channel name was not found.

                //  Create feedback message for user
                string noChannelMessage = "Channel not found: \"{channel}\""
                    + "\nDid you mean one of these?\n";
                noChannelMessage = noChannelMessage
                    .Replace("{channel}", channelName);

                channels.Sort();
                foreach (var c in channels)
                {
                    noChannelMessage += "\n" + c;
                }

                await PMFeedbackAsync(noChannelMessage, MessageSeverity.Warning);
            }
            else
            {
                await AnnounceOnChannelId(destinationChannel.Id, message);
            }
        }

        #endregion
    }
}
