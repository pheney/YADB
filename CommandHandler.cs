using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;
using YADB.Modules;

namespace YADB
{
    /// <summary> Detect whether a message is a command, then execute it. </summary>
    public class CommandHandler
    {
        #region Main 

        private DiscordSocketClient _client;
        private CommandService _cmds;

        public async Task InstallAsync(DiscordSocketClient c)
        {
            _client = c;                                                 // Save an instance of the discord client.
            _cmds = new CommandService();                                // Create a new instance of the commandservice.                              

            // Load all modules from the assembly.
            //await _cmds.AddModulesAsync(Assembly.GetEntryAssembly());    

            //  Load modules individually.
            //  Because this project has more modules than I want to use all the time.
            await _cmds.AddModuleAsync<PuppetModule>();
            await _cmds.AddModuleAsync<HelpModule>();
            await _cmds.AddModuleAsync<ModeratorModule>();
            await _cmds.AddModuleAsync<CleanModule>();

            _client.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.

            //  Register the user-joined event
            _client.UserJoined += AsyncUserJoined;

            //  Patrick
            //_client.Ready += IntroductionAsync;
        }

        /// <summary>
        /// 2017-8-17
        /// Process all messages sent across the channel. This also
        /// receives system messages, which should be ignored
        /// </summary>
        /// <param name="s"></param>
        private async Task HandleCommandAsync(SocketMessage s)
        {
            //  Attempt to cast the message as a user message
            //  i.e., *not* a system message.
            var msg = s as SocketUserMessage;

            //  This will be null if the message was a system message,
            //  and NOT NULL if it is a user message. Abort processing
            //  if this is a system message.
            if (msg == null) return;

            // Create a new command context.
            var context = new SocketCommandContext(_client, msg);

            //  Determine if this is a public channel, or a DM channel
            bool isDMChannel = (context.Channel as IDMChannel) != null;

            if (isDMChannel)
            {
                //await Program.AsyncConsoleMessage("DM Channel message", ConsoleColor.Cyan);
                await HandleDMCommandAsync(context, msg);
                return;
            }
            else
            {
                //await Program.AsyncConsoleMessage("NON-DM Channel message", ConsoleColor.Magenta);
                await HandlePublicCommandAsync(context, msg);
                return;
            }
        }

        #endregion
        #region Primary Methods

        /// <summary>
        /// 201-8-20
        /// Message was received from a user in a public channel.
        /// The message needs to be checked for the command prefixes
        /// prior to processing.
        /// </summary>
        private async Task HandlePublicCommandAsync(SocketCommandContext context, SocketUserMessage msg)
        {
            //  Ignore messages from ourself
            ulong authorId = context.User.Id;
            ulong userId = _client.CurrentUser.Id;
            if (authorId == userId) return;

            //  Prefix, NO sub-command prefix --> user chatting with bot
            //  Prefix, sub-commad prefix --> user command bot to execute command
            //  NO Prefix, sub-command prefix --> user fumbled bot prefix, offer help
            //  NO Prefix, NO sub-command prefix --> not a message to the bot

            #region Check for commandPrefix, e.g., "!"
            //  Check if the message has any of the command prefixes

            int argPos = 0;
            bool hasCommandPrefix = false;
            foreach (string prefix in Configuration.Load().Prefix)
            {
                hasCommandPrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion
            #region Check for UsernamePrefix, e.g., "Pqq"
            //  Check if the message has bot's username prefix

            string username = context.Guild.CurrentUser.Username;
            string[] usernamePrefix = new string[] {
                username,                username + " ",                username + ",",                username + ", "
            };

            bool hasUsernamePrefix = false;
            foreach (string prefix in usernamePrefix)
            {
                hasUsernamePrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion
            #region Check for Mention prefix, e.g., "@Username"
            //  Check if the message has a mention prefix

            bool hasMentionPrefix = msg.HasMentionPrefix(_client.CurrentUser, ref argPos);

            #endregion
            #region Check for Nickname prefix, e.g., "nickname"
            //  Check if the message has bot's nickname prefix

            string nickname = context.Guild.CurrentUser.Nickname;
            string[] nickPrefix = new string[] {
                nickname,                nickname + " ",                nickname + ",",                nickname + ", "
            };
            bool hasNickPrefix = false;
            foreach (string prefix in nickPrefix)
            {
                hasNickPrefix|= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion

            //  In a public channel, when there is no prefix to get the bot's
            //  attention, do nothing.
            if (!hasCommandPrefix 
                && !hasUsernamePrefix 
                && !hasMentionPrefix 
                && !hasNickPrefix) return;
                        
            //  Attempt to parts whatever was said to the bot as a command
            var result = await _cmds.ExecuteAsync(context, argPos);

            //  When parsing is successful, that means the bot received a command
            //  and was able to execute it. So we exit.
            if (result.IsSuccess) return;

            // When command-parsing fails, try different responses
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    //  distinguish between chatting and fumbled #commands
                    string[] words = msg.Content.Split(' ');
                    bool hasSubprefix = false;
                    foreach (string subprefix in Configuration.Load().SubPrefix)
                    {
                        hasSubprefix |= words[1].StartsWith(subprefix);
                    }

                    if (hasSubprefix)
                    {
                        //  fumbled #command
                        await ErrorAsync(context, words[1], "typo?");
                    }
                    else
                    {
                        //  try chatting
                        await Services.Chat.Reply(context, msg.Content);
                    }
                    break;
                default:
                    //  Any other execution failures should show an error message
                    await ErrorAsync(context, msg.ToString().Substring(argPos), result.ErrorReason);
                    break;
            }
        }

        /// <summary>
        /// 2017-8-20
        /// Message was received from a user in a DM channel.
        /// This message does not need to be check for the command prefixes.
        /// The message only needs to be checked for sub-command prefixes.
        /// </summary>
        private async Task HandleDMCommandAsync(SocketCommandContext context, SocketUserMessage msg)
        {
            //  Ignore messages from ourself
            ulong authorId = context.User.Id;
            ulong userId = _client.CurrentUser.Id;
            if (authorId==userId) return;

            #region Check for commandPrefix, e.g., "!"
            //  Check if the message has any of the command prefixes

            int argPos = 0;
            bool hasCommandPrefix = false;
            foreach (string prefix in Configuration.Load().Prefix)
            {
                hasCommandPrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion
            #region Check for Username Prefix, e.g., "Pqq"
            //  Check if the message has bot's username prefix

            string username = _client.CurrentUser.Username;
            string[] usernamePrefix = new string[] {
                username,                username + " ",                username + ",",                username + ", "
            };

            bool hasUsernamePrefix = false;
            foreach (string prefix in usernamePrefix)
            {
                hasUsernamePrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion
            #region Check for Mention prefix, e.g., "@Username"
            //  Check if the message has a mention prefix

            bool hasMentionPrefix = msg.HasMentionPrefix(_client.CurrentUser, ref argPos);

            #endregion
            #region Check for Nickname prefix, e.g., "nickname"
            //  Nicknames are server/guild specific.
            //  In order to check nicknames, every server needs to be iterated.

            bool hasNickPrefix = false;
            string[] nickPrefix = new string[] { "", " ", ",", ", " };
            List<SocketGuild> guilds = context.Client.Guilds.ToList();
            foreach (SocketGuild guild in guilds)
            {
                SocketGuildUser guildUser = guild.GetUser(userId);
                string nickname = guildUser.Nickname;
                if (string.IsNullOrWhiteSpace(nickname)) continue;

                foreach (string np in nickPrefix)
                {
                    string nicknamePrefix = nickname + np;
                    hasNickPrefix |= msg.HasStringPrefix(nicknamePrefix, ref argPos, StringComparison.OrdinalIgnoreCase);
                }
            }

            #endregion
            
            //  In a private DM channel, when there IS a prefix to get the bot's
            //  attention, inform the user not to use it.
            if (hasCommandPrefix
                || hasUsernamePrefix
                || hasMentionPrefix
                || hasNickPrefix)
            {
                string message = "There is no need to directly address me in a DM channel. "
                    + "It is only us; I know who you are talking to.";

                //  Send message via direct-message to the user            
                await context.Channel.SendMessageAsync(message);
                return;
            }

            //  First, assume user has entered a command.
            //  Try and execute the user input as a command with the given context.
            var result = await _cmds.ExecuteAsync(context, argPos);

            //  When successful, this means the user entered a valid command and
            //  the bot was able to execute it. No further processing is required.
            if (result.IsSuccess) return;
            
            // When execution fails, try different responses
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    //  Distinguish between chatting and fumbled #commands
                    bool hasSubprefix = false;
                    foreach (string subprefix in Configuration.Load().SubPrefix)
                    {
                        hasSubprefix |= msg.Content.StartsWith(subprefix);
                    }

                    if (hasSubprefix)
                    {
                        //  fumbled #command
                        string[] words = msg.Content.Split(' ');
                        await ErrorAsync(context, words.JoinWith(" ",0,1), "typo?");
                    }
                    else
                    {
                        //  try chatting
                        await Services.Chat.Reply(context, msg.Content, false);
                    }
                    break;
                default:
                    //  Any other execution failures should show an error message
                    await ErrorAsync(context, msg.ToString().Substring(argPos), result.ErrorReason);
                    break;
            }
        }

        /// <summary>
        /// Event handler for the Ready event. This fires
        /// when the bot first joins a guild.
        /// </summary>
        private async Task IntroductionAsync()
        {
            string response;
            await Services.Chat.GetReply(Constants.Greetings.Random(), out response);

            //  Main channel Id is always the same as the guild Id, according to Gavin.
            ulong mainChannelId = _client.Guilds.First().Id;
            var destinationChannel = _client.GetChannel(mainChannelId) as IMessageChannel;

            if (destinationChannel == null)
            {
                //  User is not in a public channel

                //  Send message via direct-message to the user            
                var dmchannel = await _client.CurrentUser.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync(response);
            }
            else
            {
                await destinationChannel.SendMessageAsync(response);
            }

            await Services.Chat.EnableChat(true);
        }

        #endregion
        #region Utilities

        /// <summary>
        /// 2017-6-18
        /// </summary>
        private async Task ErrorAsync(SocketCommandContext context, string message, string reason)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = Constants.SlateRed,
                Description = "Command failed: '" + message  + "'\n"
                    + "Reason: " + reason
            };

            //  Send errors via direct-message to the user
            var dmchannel = await context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }
     
        /// <summary>
        /// 2017-8-19
        /// </summary>
        /// <param name="user">The user that just joined</param>
        public static async Task AsyncUserJoined(SocketGuildUser user)
        {
            await Program.AsyncConsoleMessage("User (" + user.Username + ") joined channel", ConsoleColor.Cyan);
            var userChannel = GetUserChannel(user);
            string greeting;
            await Services.Chat.GetReply(Constants.Greetings.Random(), out greeting);
            await userChannel.SendMessageAsync(user.Username + ", " + greeting);
        }

        /// <summary>
        /// 2017-8-19
        /// Returns the current channel the user is on.
        /// Returns null if the user is in a PM channel.
        /// </summary>
        /// <param name="user">A user object</param>
        private static ITextChannel GetUserChannel(SocketGuildUser user)
        {
            ///  Users login to Discord and are at the Direct-Message screen
            ///  This fires when the user selects a server or "guild."

            //  Guilds and servers are the same thing
            //  Ref: https://discordapp.com/developers/docs/resources/guild

            //  When the user connects to the Guild, the user returns
            //  to whatever channel they were in most recently.

            SocketTextChannel defaultChannel = user.Guild.DefaultChannel;

            return defaultChannel;
        }

        #endregion
    }
}
