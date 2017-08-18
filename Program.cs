using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace YADB
{
    /// <summary>
    /// 2017-8-17
    /// TODO:
    ///     DONE -- Compile and run this project AS IS. Make NO goddamn changes
    ///             until this thing works as is.
    ///     #. Add command listener that responds to the bot's name, e.g. "Pqq" instead of
    ///         just "!command"
    ///     #. Integrate CleverBot module from my console application.
    ///     #. Integrate OED module from my thesaurus console applciation.
    ///     #. Integrate Inspirobot, ref: https://github.com/bhberson/InspiroBotSlack
    ///         URL to hit: https://inspirobotslack.herokuapp.com/inspirobot
    ///         Image for icon: http://inspirobot.me/website/images/inspirobot-dark-green.png 
    ///     #. Add verbosity levels to console output, including colors
    ///     #. Redirect !Help output to a private message
    ///     #. Filter !Help results based on user access level
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

            _client.Log += (l)                               // Register the console log event.
                => Console.Out.WriteLineAsync(l.ToString());
                                   
            await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
            await _client.StartAsync();

            _commands = new CommandHandler();                // Initialize the command handler service
            await _commands.InstallAsync(_client);
            
            await Task.Delay(-1);                            // Prevent the console window from closing.
        }
    }
}
