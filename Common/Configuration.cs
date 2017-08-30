using Newtonsoft.Json;
using System;

namespace YADB
{
    /// <summary> 
    /// 2017-8-25
    /// Read and write to files for information you either don't want public
    /// or will want to change without having to compile another bot.
    /// 
    /// This object contains general configuration data for the bot, as well as the
    /// ability to load and save itself.
    /// </summary>
    public class Configuration
    {
        /// <summary> Ids of users who will have owner access to the bot. </summary>
        public ulong[] Owners { get; set; }
        /// <summary> Your bot's command prefix. </summary>
        public string[] Prefix { get; set; } = new string[] { "!" };
        /// <summary> Your bot's sub-command prefix </summary>
        public string[] SubPrefix { get; set; } = new string[] { "#" };
        /// <summary> Your bot's login token. </summary>
        public string Token { get; set; } = "";
        /// <summary>
        /// Link to provide admin so the bot can be invited to operate on a server.
        /// </summary>
        public string InviteLink { get; set; }

        [JsonIgnore]
        public static Configuration Get;

        public static void EnsureExists()
        {
            //  All files are stored in the 'config' directory
            string FileName = new Configuration().GetFilename();
            string file = FileOperations.PathToFile(FileName);

            // When the file does NOT exists, create it
            if (!FileOperations.Exists(FileName))
            {
                //  Create a new configuration object
                var config = new Configuration();

                //  Manually enter the bot token
                Console.WriteLine("Please enter your token: ");
                string token = Console.ReadLine();
                config.Token = token;

                //  Save the configuration object
                FileOperations.SaveAsJson(config);
            }

            Configuration.Get = FileOperations.Load<Configuration>();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Configuration Loaded");
            Console.ResetColor();
        }
    }
}
