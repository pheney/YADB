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
            await _cmds.AddModuleAsync<DictionaryModule>();
            //await _cmds.AddModuleAsync<MathModule>();
            await _cmds.AddModuleAsync<GameModule>();

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

            #region Special -- Haddaway Game

            //  Only users can start the game
            if (!context.User.IsBot)
            {
                //  When the game is NOT running, always monitor for a chance to start
                if (!GameModule.IsPlayingHaddawayGameOnChannel(context.Channel.Id))
                {
                    //  Try to start game
                    await GameModule.StartHaddaway(context, msg.Content.Trim());

                    //  When game is running (successfully started) abort further processing
                    if (GameModule.IsPlayingHaddawayGameOnChannel(context.Channel.Id)) return;
                }
                else
                {
                    //  The game is already running on this channel       

                    //  First, iterate the game
                    await GameModule.UpdateHaddawayGame(context, msg.Content.Trim());

                    //  Next, check for game over
                    if (GameModule.IsPlayingHaddawayGameOnChannel(context.Channel.Id)) return;
                }
            }

            #endregion

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
            //  Prefix, NO sub-command prefix --> user chatting with bot
            //  Prefix, sub-commad prefix --> user command bot to execute command
            //  NO Prefix, sub-command prefix --> user fumbled bot prefix, offer help
            //  NO Prefix, NO sub-command prefix --> not a message to the bot

            int argPos = 0;
            /*
            #region Check for UsernamePrefix, e.g., "Pqq"
            //  Check if the message has bot's username prefix

            string username = context.Guild.CurrentUser.Username;
            string[] usernamePrefix = new string[] {
                username, username + " ", username + ",", username + ", "
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
                nickname, nickname + " ", nickname + ",", nickname + ", "
            };
            bool hasNickPrefix = false;
            foreach (string prefix in nickPrefix)
            {
                hasNickPrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion
            #region Check for command + short nick Prefix, e.g., "!Ni"

            string cmdNick = Configuration.Load().Prefix[0] 
                + context.Guild.CurrentUser.Nickname.Substring(0, 2);
            string[] cmdNickPrefix = new string[] {
                cmdNick, cmdNick + " ", cmdNick + ",", cmdNick + ", "
            };

            bool hasCmdNickPrefix = false;
            foreach (string prefix in cmdNickPrefix)
            {
                hasCmdNickPrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            #endregion

            //  In a public channel, when there is no prefix to get the bot's
            //  attention, do nothing.
            if (!hasUsernamePrefix && 
                !hasMentionPrefix &&
                !hasNickPrefix &&
                !hasCmdNickPrefix) return;
            */

            if (!HasAnyAttentionFlag(context, msg, ref argPos)) return;

            //  SPECIAL
            //  If the bot is Rick-Rolling, any incoming message 
            //  on the same channel will abort the rick roll.
            if (GameModule.IsRickRollingOnChannel(context.Channel.Id))
            {
                await GameModule.StopRickRoll();
                return;
            }

            //  Attempt to parse whatever was said as a command
            var result = await _cmds.ExecuteAsync(context, argPos);
                        
            //  When parsing is successful, that means the bot received a command
            //  and was able to execute it. So we exit.
            if (result.IsSuccess) return;

            //  This ignores message from itself.
            //  This is located here, after the result.IsSuccess check because
            //  the bot DOES send commands to itself, but it does NOT need
            //  to literally chat with itself.

            ulong authorId = context.User.Id;
            ulong userId = _client.CurrentUser.Id;
            if (authorId == userId) return;

            // When command-parsing fails, try different responses
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    //  distinguish between chatting and fumbled #commands
                    string submessage = msg.Content.Substring(argPos);
                    string[] words = submessage.Split(' ');
                    bool hasSubprefix = false;
                    foreach (string subprefix in Configuration.Load().SubPrefix)
                    {
                        hasSubprefix |= words[0].StartsWith(subprefix);
                    }

                    if (hasSubprefix)
                    {
                        //  fumbled #command
                        await ErrorAsync(context, words[0], "typo?");
                    }
                    else
                    {
                        //  try chatting
                        await Services.Chat.Reply(context, msg.Content);
                    }
                    break;
                case CommandError.UnmetPrecondition:
                    await context.Channel.SendMessageAsync("You have no power here!");
                    break;
                default:
                    if (result.ErrorReason.Equals("The server responded with error 403: Forbidden"))
                    {
                        await context.Channel.SendMessageAsync("These actions are forbidden!");
                    }
                    else
                    {
                        //  Any other execution failures should show an error message
                        await ErrorAsync(context, msg.ToString().Substring(argPos), result.ErrorReason);
                    }
                    break;
            }
        }

        /// <summary>
        /// 2017-8-24
        /// Indicates if the SocketUserMessage has any of the flag used to get the 
        /// bot's attention, i.e.,
        ///     username
        ///     mention
        ///     nickname
        ///     short nickname ("!" + first two letters of nickname)
        /// </summary>
        private bool HasAnyAttentionFlag(SocketCommandContext context, SocketUserMessage msg, ref int messageStartIndex)
        {
            //  the person talking to this bot
            SocketUser msgAuthor = context.Message.Author;

            //  guild (server) where this message originated
            SocketGuild authorGuild = context.Guild;

            //  PQQ (this bot)
            SocketGuildUser botUser = context.Guild.CurrentUser;

            //  All the ways the author can prefix a message to 
            //  get this bot's attention.
            string username = botUser.Username;
            string mention = botUser.Mention;
            string nickname = botUser.Nickname;
            string shortNickname = Configuration.Load().Prefix[0] + nickname.Substring(0, 2);
            
            bool hasFlagAsUsername = HasAttentionFlag(username, msg, ref messageStartIndex);
            bool hasFlagAsMention = HasAttentionFlag(mention, msg, ref messageStartIndex);
            bool hasFlagAsNickname = HasAttentionFlag(nickname, msg, ref messageStartIndex);
            bool hasFlagAsShortNick = HasAttentionFlag(shortNickname, msg, ref messageStartIndex);

            return hasFlagAsUsername || hasFlagAsMention || hasFlagAsNickname || hasFlagAsShortNick;
        }

        /// <summary>
        /// 2017-8-24
        /// Indicates if the socket message starts with any variation of the provided
        /// flag, e.g., "nickname" or "nickname " or "nickname," or "nickname, "
        /// </summary>
        private bool HasAttentionFlag(string flag, SocketUserMessage socketMessage, ref int index)
        {
            string[] flags = GetFlagVariations(flag);
            bool hasFlag = false;
            foreach (var f in flags)
            {
                //  Only check flags that have something in them.
                //  Skip empty flag, such as "" and whitespace flags such as " ".
                if (!string.IsNullOrWhiteSpace(f))
                {
                    hasFlag |= socketMessage.HasStringPrefix(f, ref index, StringComparison.OrdinalIgnoreCase);
                }
            }
            return hasFlag;
        }

        /// <summary>
        /// 2017-8-24
        /// Returns the four variations of a name:
        ///     "name"
        ///     "name "
        ///     "name,"
        ///     "name, "
        /// </summary>
        private string[] GetFlagVariations(string name)
        {
            return new string[] { name, name + " ", name + ",", name + ", " };
        }

        /// <summary>
        /// 2017-8-20
        /// Message was received from a user in a DM channel.
        /// This message does not need to be check for the command prefixes.
        /// The message only needs to be checked for sub-command prefixes.
        /// </summary>
        private async Task HandleDMCommandAsync(SocketCommandContext context, SocketUserMessage msg)
        {
            //  the person talking to this bot
            SocketUser msgAuthor = context.Message.Author;

            //  PQQ (this bot)
            SocketSelfUser botUser = _client.CurrentUser;

            //  Ignore messages from ourself
            if (msgAuthor.Id == botUser.Id) return;
            
            int argPos = 0;

            #region Check for Username Prefix, e.g., "Pqq"
            //  Check if the message has bot's username prefix

            string username = _client.CurrentUser.Username;
            string[] usernamePrefix = new string[] {
                username, username + " ", username + ",", username + ", "
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
                SocketGuildUser guildUser = guild.GetUser(botUser.Id);
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
            if (hasUsernamePrefix ||
                hasMentionPrefix ||
                hasNickPrefix)
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
                        string submessage = msg.Content.Substring(argPos);
                        string[] words = submessage.Split(' ');
                        await ErrorAsync(context, words.JoinWith(" ", 0, 2), "typo?");
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
                Description = "Command failed: '" + message + "'\n"
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