using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

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
            Configuration.EnsureExists();                    // Ensure the configuration file has been created.
                                                             // Create a new instance of DiscordSocketClient.
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Verbose,              // Specify console verbose information level.
                MessageCacheSize = 1000                      // Tell discord.net how long to store messages (per channel).
            });

            //_client.Log += (l)                               // Register the console log event.
            //    => Console.Out.WriteLineAsync(l.ToString());

            //  Custom console writer - Patrick
            _client.Log += (l) => AsyncConsoleLog(l);

            await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
            await _client.StartAsync();

            _commands = new CommandHandler();                // Initialize the command handler service
            await _commands.InstallAsync(_client);

            await Task.Delay(-1);                            // Prevent the console window from closing.
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        /// <param name="logMessage"></param>
        private async Task AsyncConsoleLog(LogMessage logMessage)
        {
            //  default console color
            ConsoleColor fg = ConsoleColor.DarkMagenta;

            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    fg = ConsoleColor.Red;
                    break;
                case LogSeverity.Debug:
                    fg = ConsoleColor.Magenta;
                    break;
                case LogSeverity.Error:
                    fg = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    fg = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                    fg = ConsoleColor.Gray;
                    break;
                case LogSeverity.Warning:
                    fg = ConsoleColor.Green;
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
    }
}
