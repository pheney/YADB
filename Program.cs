using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB
{
    /// <summary>
    /// 2017-8-18
    ///     DONE -- Compile and run this project AS IS. Make NO goddamn changes
    ///             until this thing works as is.
    ///     DONE -- Add command listener that responds to the bot's name, e.g. "Pqq" instead of
    ///             just "!command"
    ///     DONE -- Integrate CleverBot module from my console application.
    ///     DONE -- Integrate Inspirobot
    ///     DONE -- Add verbosity levels to console output, including colors
    ///     DONE -- Redirect !Help output to a private message
    ///     DONE -- Filter !Help results based on user access level
    ///     #### -- Integrate OED module from my thesaurus console application.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _commands;

        public async Task StartAsync()
        {
            //  Ensure the configuration file has been created
            Configuration.EnsureExists();                    
                                                             
            //  Create a new instance of DiscordSocketClient
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                WebSocketProvider = WS4NetProvider.Instance,

                //  Specify console verbose information level
                LogLevel = LogSeverity.Verbose,

                //  Tell discord.net how long to store messages (per channel)
                MessageCacheSize = 1000                      
            });

            //  Register the console log event using a custom console writer
            _client.Log += (l) => AsyncConsoleLog(l);

            //  Register the user-joined event
            //_client.UserJoined += (u) => AsyncUserJoined(u);

            //  Basic console log output
            //_client.Log += (l) => Console.Out.WriteLineAsync(l.ToString());
            
            //  Connect to Discord
            await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
            await _client.StartAsync();

            //  Initialize the command handler service
            _commands = new CommandHandler();                
            await _commands.InstallAsync(_client);

            //  Prevent the console window from closing
            await Task.Delay(-1);                            
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        private async Task AsyncConsoleLog(LogMessage logMessage)
        {
            //  default console color
            ConsoleColor fg = ConsoleColor.DarkMagenta;

            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    fg = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    fg = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    fg = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    fg = ConsoleColor.Gray;
                    break;
                case LogSeverity.Debug:
                    fg = ConsoleColor.Cyan;
                    break;
                default:
                    //  do nothing
                    break;
            }

            string message = string.Format("{0} [{1}] {2}: {3}", DateTime.Now, logMessage.Severity.ToString().Substring(0, 1), logMessage.Source, logMessage.Message);
            await AsyncConsoleMessage(message, fg);
        }

        /// <summary>
        /// 2017-8-17
        /// Custom logger allows for changing log level.
        /// Allows color coded console messages.
        /// </summary>
        public static async Task AsyncConsoleMessage(string message, ConsoleColor fg = ConsoleColor.Gray, ConsoleColor bg = ConsoleColor.Black, bool newline = true)
        {
            ConsoleColor ofg, obg;

            //  save original colors
            ofg = Console.ForegroundColor;
            obg = Console.BackgroundColor;

            //  set desired colors
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;

            //  echo message
            await Console.Out.WriteAsync(message);
            if (newline) await Console.Out.WriteLineAsync();

            //  restore original colors
            Console.ForegroundColor = ofg;
            Console.BackgroundColor = obg;
        }

        /// <summary>
        /// 2017-8-19
        /// </summary>
        /// <param name="user">The user that just joined</param>
        public static async Task AsyncUserJoined(SocketGuildUser user)
        {
            await AsyncConsoleMessage("User (" + user.Username + ") joined channel", ConsoleColor.Cyan);
            var userChannel = GetUserChannel(user);
            string greeting;
            await Services.Chat.GetReply(Constants.Greetings.Random(), out greeting);
            await userChannel.SendMessageAsync(user.Username+", " + greeting);
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
    }
}
