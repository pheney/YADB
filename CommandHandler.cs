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

            #region Add Commnand Modules

            // Load all modules from the assembly.
            //await _cmds.AddModulesAsync(Assembly.GetEntryAssembly());    

            //  Load modules individually.
            await _cmds.AddModuleAsync<PuppetModule>();
            await _cmds.AddModuleAsync<HelpModule>();
            await _cmds.AddModuleAsync<ModeratorModule>();
            await _cmds.AddModuleAsync<CleanModule>();
            await _cmds.AddModuleAsync<DictionaryModule>();
            await _cmds.AddModuleAsync<GameModule>();
            //await _cmds.AddModuleAsync<MathModule>();

            #endregion
            #region Event Handlers

            //  Register the command processor
            _client.MessageReceived += HandleCommandAsync;
            _client.MessageReceived += EchoTrafficAsync;
            
            //  Register the user-joined event
            _client.UserJoined += AsyncUserJoined;

            //  Regsiter the handler that announces the bot is online
            //_client.Ready += IntroductionAsync;

            #endregion
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

        /// <summary>
        /// 2017-8-25
        /// Echos all mention of a specific user to the console. 
        /// </summary>
        private async Task EchoTrafficAsync(SocketMessage s)
        {
            //  ignore system messages
            var msg = s as SocketUserMessage;
            if (msg == null) return;

            var context = new SocketCommandContext(_client, msg);
            string username = "patrickq";
            List<string> nicknames = new List<string>();

            //  get all nicknames
            foreach (var guild in context.Client.Guilds)
            {
                List<SocketGuildUser> users = new List<SocketGuildUser>();
                users = guild.Users.Where(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var user in users)
                {
                    if (string.IsNullOrWhiteSpace(user.Nickname)) continue;
                    nicknames.Add(user.Nickname);
                }
            }

            //  determine if message contains username or any nicknames
            string message = s.Content;
            bool contains = s.Content.Contains(username);
            foreach (string nick in nicknames)
            {
                contains |= message.Contains(nick);
            }
            if (!contains) return;

            //  send message to console
            string author = msg.Author.Username;
            string consoleMessage = author + " : " + message;
            await Program.AsyncConsoleMessage(consoleMessage, ConsoleColor.Cyan);
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
            //  Attention Flag, but NO sub-command prefix --> user chatting with bot
            //  Attention Flag, sub-commad prefix --> user command bot to execute command
            //  NO Attention Flag, sub-command prefix --> user fumbled bot prefix, offer help
            //  NO Attention Flag, NO sub-command prefix --> not a message to the bot

            int argPos = 0;
            bool hasFlags = HasAnyUsernameFlags(context, msg, ref argPos) ||
                HasAnyNicknameFlags(context, msg, ref argPos);
            if (!hasFlags) return;

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

            //  This ignores messages from itself.
            //  This is located after the result.IsSuccess check
            //  because the bot DOES send commands to itself, 
            //  but it does NOT chat with itself.
                        
            if (IsMessageFromOurself(msg)) return;

            // When command-parsing fails, try different responses
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    //  distinguish between chatting and fumbled #commands
                    string submessage = msg.Content.Substring(argPos);
                    string[] words = submessage.Split(' ');
                    bool hasSubprefix = false;
                    foreach (string subprefix in Configuration.Get.SubPrefix)
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
        /// 2017-8-20
        /// Message was received from a user in a DM channel.
        /// This message does not need to be check for the command prefixes.
        /// The message only needs to be checked for sub-command prefixes.
        /// </summary>
        private async Task HandleDMCommandAsync(SocketCommandContext context, SocketUserMessage msg)
        {
            //  Ignore messages from ourself
            if (IsMessageFromOurself(msg)) return;

            int argPos = 0;                       
            bool hasFlags = HasAnyUsernameFlags(context, msg, ref argPos) ||
                HasAnyNicknameFlags(context, msg, ref argPos);
            if (hasFlags)
            {
                //  Feedback message
                string message = "There is no need to directly address me in a DM channel. "
                    + "It is only us; I know who you are talking to.";

                //  Send message via direct-message to the user            
                await context.Channel.SendMessageAsync(message);
                return;
            }

            //  SPECIAL
            //  If the bot is Rick-Rolling, any incoming message 
            //  on the same channel will abort the rick roll.
            IDMChannel DMChannel = context.Channel as IDMChannel;
            if (GameModule.IsRickRollingOnChannel(DMChannel.Id))
            {
                await GameModule.StopRickRoll();
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
                    foreach (string subprefix in Configuration.Get.SubPrefix)
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
        /// 2017-8-24
        /// Indicates if the SocketUserMessage has any of the flag used to get the 
        /// bot's attention, i.e.,
        ///     username
        ///     mention
        /// </summary>
        private bool HasAnyUsernameFlags(SocketCommandContext context, SocketUserMessage msg, ref int messageStartIndex)
        {
            //  guild (server) where this message originated
            SocketGuild authorGuild = context.Guild;

            //  PQQ (this bot)
            SocketSelfUser botUser =  _client.CurrentUser;

            //  All the ways the author can prefix a message to 
            //  get this bot's attention.
            string username = botUser.Username;
            string mention = botUser.Mention;

            bool hasFlagAsUsername = HasAttentionFlag(username, msg, ref messageStartIndex);
            bool hasFlagAsMention = HasAttentionFlag(mention, msg, ref messageStartIndex);

            return hasFlagAsUsername || hasFlagAsMention;
        }

        /// <summary>
        /// 2017-8-24
        /// This must handle checks from a public (guild) channel, as
        /// well as checks from a private (PM) channel.
        /// Indicates if the SocketUserMessage has any of the flag used to get the 
        /// bot's attention, i.e.,
        ///     nickname
        ///     short nickname ("!" + first two letters of nickname)
        /// </summary>
        private bool HasAnyNicknameFlags(SocketCommandContext context, SocketUserMessage msg, ref int messageStartIndex)
        {
            List<string> nicknames = new List<string>();

            //  Determine if this is a public channel, or a DM channel
            bool isDMChannel = (context.Channel as IDMChannel) != null;

            if (isDMChannel)
            {
                ////  Check all nicknames on all guilds
                nicknames.AddRange(GetAllNicknames(context));
            } else
            {
                ////  Check the nickname on the current guild
                nicknames.Add(GetGuildNickname(context.Guild));
            }

            bool hasFlagAsNick = false, hasFlagAsShortNick = false;

            //  All the ways the author can prefix a message to 
            //  get this bot's attention.
            foreach (var nick in nicknames)
            {
                string shortNick = Configuration.Get.Prefix[0] + nick.Substring(0, 2);
                hasFlagAsNick |= HasAttentionFlag(nick, msg, ref messageStartIndex);
                hasFlagAsShortNick |= HasAttentionFlag(shortNick, msg, ref messageStartIndex);
            }
            
            return hasFlagAsNick || hasFlagAsShortNick;
        }

        /// <summary>
        /// 2017-8-24
        /// Returns the nickname for this bot from the Guild parameter.
        /// </summary
        private string GetGuildNickname(SocketGuild guild)
        {
            return guild.CurrentUser.Nickname;
        }

        /// <summary>
        /// 2017-8-24
        /// Returns every nickname associate with this bot, from every guild (server)
        /// that it is on.
        /// </summary>
        private List<string> GetAllNicknames(SocketCommandContext context)
        {
            List<string> nicknames = new List<string>();
            List<SocketGuild> guilds = context.Client.Guilds.ToList();

            foreach (SocketGuild guild in guilds)
            {
                string nickname = GetGuildNickname(guild);
                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    nicknames.Add(nickname);
                }
            }
            return nicknames;
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
        /// 2017-8-24
        /// Indicates if this is a message sent by this bot.
        /// </summary>
        private bool IsMessageFromOurself(SocketUserMessage socketUserMessage)
        {
            return socketUserMessage.Author.Id == _client.CurrentUser.Id;
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