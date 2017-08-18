using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB
{
    /// <summary> Detect whether a message is a command, then execute it. </summary>
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _cmds;

        public async Task InstallAsync(DiscordSocketClient c)
        {
            _client = c;                                                 // Save an instance of the discord client.
            _cmds = new CommandService();                                // Create a new instance of the commandservice.                              
            
            await _cmds.AddModulesAsync(Assembly.GetEntryAssembly());    // Load all modules from the assembly.
            
            _client.MessageReceived += HandleCommandAsync;               // Register the messagereceived event to handle commands.

            //  Patrick
            _client.Ready += IntroductionAsync;
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        /// <param name="s"></param>
        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null)                                          // Check if the received message is from a user.
                return;
            
            var context = new SocketCommandContext(_client, msg);     // Create a new command context.

            //  Check if the message has any of the string prefixes
            int argPos = 0;
            bool hasStringPrefix = false;
            foreach (string prefix in Configuration.Load().Prefix)
            {
                hasStringPrefix |= msg.HasStringPrefix(prefix, ref argPos, System.StringComparison.OrdinalIgnoreCase);
            }

            //  Check if the message has a mention prefix
            bool hasMentionPrefix = msg.HasMentionPrefix(_client.CurrentUser, ref argPos);
            
            //  Do nothing if there is no prefix to get the bot's attention
            if (!hasStringPrefix && !hasMentionPrefix) return;
            
            // Try and execute a command with the given context.
            var result = await _cmds.ExecuteAsync(context, argPos);

            if (result.IsSuccess) return;

            // When execution fails, try different responses
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    //  try chatting
                    await Services.Chat.Reply(context, msg.Content);
                    break;
                default:
                    //  Any other execution failures should show an error message
                    await ErrorAsync(context, msg, argPos, result);
                    break;
            }        
        }
        
        /// <summary>
        /// 2017-6-18
        /// </summary>
        private async Task ErrorAsync(SocketCommandContext context, SocketMessage msg, int argPos, IResult result)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = Constants.SlateRed,
                Description = "Command failed: '" + msg.ToString().Substring(argPos) + "'\n"
                    + "Reason: " + result.ErrorReason.ToString()
            };

            //  Send errors via direct-message to the user
            var dmchannel = await context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }

        private async Task IntroductionAsync()
        {
            string response;
            await Services.Chat.GetReply("hi", out response);

            //  Main channel Id is always the same as the guild Id
            //  according to Gavin.
            ulong mainChannelId = _client.Guilds.First().Id;
            var destinationChannel = _client.GetChannel(mainChannelId) as IMessageChannel;

            if (destinationChannel == null)
            {
                //  User is not in a public channel

                //  Send message via direct-message to the user            
                var dmchannel = await _client.CurrentUser.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync(response);
            } else{
                await destinationChannel.SendMessageAsync(response);
            }
        }
    }
}
