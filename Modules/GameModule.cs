using Discord.Commands;
using YADB.Preconditions;
using System.Threading.Tasks;
using YADB.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using Discord;
using Newtonsoft.Json;

namespace YADB.Modules
{
    [Name("Games")]
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        protected GameModule():base()
        {
            if (EightBallData.Get==null) EightBallData.Init();
        }

        #region Magic Eight Ball

        private class EightBallData
        {
            [JsonIgnore]
            public static EightBallData Get;

            public int TotalRolls = 0;
            
            public static void Init()
            {
                string FileName = new EightBallData().GetFilename();
                string file = FileOperations.PathToFile(FileName);

                // When the file does NOT exists, create it
                if (!FileOperations.Exists(FileName))
                {
                    //  Create a new configuration object
                    var data = new EightBallData();
                    
                    //  Save the configuration object
                    FileOperations.SaveAsJson(data);
                }

                EightBallData.Get = FileOperations.Load<EightBallData>();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(FileName+" loaded");
                Console.ResetColor();
            }
        }
        
        public static Task EightBallStatus(SocketCommandContext context)
        {
            if (EightBallData.Get == null) EightBallData.Init();
            string display = "Magic Eight Ball: " + EightBallData.Get.TotalRolls + " predictions made";

            EmbedBuilder builder = new EmbedBuilder
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = display
            };

            //  Send problem message via direct-message to the user            
            context.Channel.SendMessageAsync("", false, builder.Build());

            return Task.CompletedTask;
        }
        
        private static string[] EightBallResults = new string[]
        {
            "It is certain",
            "It is decidedly so",
            "Without a doubt",
            "Yes definitely",
            "You may rely on it",
            "As I see it, yes",
            "Most likely",
            "Outlook good",
            "Yes",
            "Signs point to yes",
            "Reply hazy try again",
            "Ask again later",
            "Better not tell you now",
            "Cannot predict now",
            "Concentrate and ask again",
            "Don't count on it",
            "My reply is no",
            "My sources say no",
            "Outlook not so good",
            "Very doubtful"
        };

        [Command("#8ball"), Alias ("#8")]
        [Remarks("Ask a question, get an answer")]
        [MinPermissions(AccessLevel.User)]
        public async Task MagicEightBall([Remainder]string question = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                await ReplyAsync("You must ask a question for the mystic Magic Eight Ball to help you!");
            }
            else
            {
                string display = "In response to your question, \"" + question + "\"...\n\n";
                await ReplyAsync(display);
                await Task.Delay(2250);

                await ReplyAsync("_turns Magic Eight Ball over_\n\n");
                await Task.Delay(3500);

                await ReplyAsync("_\\*suspenseful music plays\\*_\n\n");
                await Task.Delay(4500);

                await ReplyAsync("\"" + EightBallResults.Random() + "\"");
                EightBallData.Get.TotalRolls++;
                FileOperations.SaveAsJson(EightBallData.Get);
            }
        }

        #endregion
        #region Rickroll

        public static Task RollingStatus(SocketCommandContext context)
        {
            string display = "Rick-roll status: ";
            if (rolling)
            {
                display += "rolling on channel " + context.Client.GetChannel(rollingChannelId).ToString();
            }
            else
            {
                display += "not rolling";
            }
            if (lastRollByChannel != null && lastRollByChannel.Count>0)
            {
                display += "\n" + RollingHistoryToString(context);
            }

            EmbedBuilder builder = new EmbedBuilder
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = display
            };

            //  Send problem message via direct-message to the user            
            context.Channel.SendMessageAsync("", false, builder.Build());

            return Task.CompletedTask;
        }

        private static string RollingHistoryToString(SocketCommandContext context)
        {
            string result = "";
            foreach (var entry in lastRollByChannel)
            {
                result += "Channel " + context.Client.GetChannel(entry.Key).ToString()
                + ", last roll at " + entry.Value.ToLocalTime()+"\n";
            }
            return result.Substring(0, result.Length - 1);
        }

        private static string[] astleyLyrics = new string[]
        {
            "We're no strangers to love",
            "You know the rules and so do I",
            "A full commitment's what I'm thinking of",
            "You wouldn't get this from any other guy",
            "I just want to tell you how I'm feeling",
            "Gotta make you understand",
            "Never gonna give you up, never gonna let you down",
            "Never gonna run around and desert you",
            "Never gonna make you cry, never gonna say goodbye",
            "Never gonna tell a lie and hurt you",
            "We've known each other for so long",
            "Your heart's been aching but you're too shy to say it",
            "Inside we both know what's been going on",
            "We know the game and we're gonna play it",
            "And if you ask me how I'm feeling",
            "Don't tell me you're too blind to see",
            "Never gonna give you up, never gonna let you down",
            "Never gonna run around and desert you",
            "Never gonna make you cry, never gonna say goodbye",
            "Never gonna tell a lie and hurt you",
            "Never gonna give you up, never gonna let you down",
            "Never gonna run around and desert you",
            "Never gonna make you cry, never gonna say goodbye",
            "Never gonna tell a lie and hurt you",
            "We've known each other for so long",
            "Your heart's been aching but you're too shy to say it",
            "Inside we both know what's been going on",
            "We know the game and we're gonna play it",
            "I just want to tell you how I'm feeling",
            "Gotta make you understand",
            "Never gonna give you up, never gonna let you down",
            "Never gonna run around and desert you",
            "Never gonna make you cry, never gonna say goodbye",
            "Never gonna tell a lie and hurt you..."
        };
        private static bool rolling = false;
        private static ulong rollingChannelId;

        /// <summary>
        /// 2017-8-24
        /// Indicates if the bot is currently Rick-Rolling on the channel
        /// provided.
        /// </summary>
        public static bool IsRickRollingOnChannel(ulong channelId)
        {
            return rolling && rollingChannelId == channelId;
        }

        private static Dictionary<ulong, DateTime> lastRollByChannel;
        private static Thread rollThread;
        private static TimeSpan rollFrequency = new TimeSpan(1, 0, 0);

        [Command("#rickroll"), Alias("#rr")]
        [Remarks("We're no strangers to love")]
        [MinPermissions(AccessLevel.User)]
        public async Task RickRoll([Remainder]string abort = null)
        {
            if (lastRollByChannel == null) lastRollByChannel = new Dictionary<ulong, DateTime>();

            //  When called parameterless, try to start a RickRoll.
            if (abort == null)
            {
                //  Conditions for start:
                //  1. There is not already one going
                //  2. It has been "awhile" since one happened on this channel

                //  When there is already a roll going, abort.
                if (rolling)
                {
                    await ReplyAsync("(We're already 'rolling baby!)");
                    return;
                }

                //  Check how long it has been since a roll was done
                //  on this channel.
                ulong channelId = Context.Channel.Id;
                if (lastRollByChannel.ContainsKey(channelId))
                {
                    DateTime lastRoll = lastRollByChannel[channelId];
                    TimeSpan howlonghasitbeen = DateTime.Now - lastRoll;

                    //  When it has been less than an hour, on this channel,
                    //  since the last time this was used, abort.
                    if (howlonghasitbeen < rollFrequency)
                    {
                        await ReplyAsync("I think this channel has had enough for awhile.");
                        return;
                    }
                }

                //  Start the RickRoll
                await StartRickRoll();
                return;
            }

            //  When called with any parameter at all, stop ongoing RickRoll.
            //  This can be used from any channel to stop any RickRoll.
            //  From within a channel currently being rolled, the CommandHandler
            //  has a listener that stops the roll any time the bot is addressed.
            if (rolling) await StopRickRoll();
            else await ReplyAsync("Who's Rick-rolling around here?");
        }

        private Task StartRickRoll()
        {
            rolling = true;
            rollingChannelId = Context.Channel.Id;

            //  Start loop thread
            rollThread = new Thread(async () => await RollLoop());
            rollThread.Start();
            return Task.CompletedTask;
        }
        
        private async Task RollLoop()
        {
            int delayMillis = 2000;
            await Task.Delay(delayMillis);
            for (int i = 0; i < astleyLyrics.Length && rolling; i++)
            {
                await Context.Channel.SendMessageAsync(astleyLyrics[i]);
                await Task.Delay(delayMillis);
            }
            await StopRickRoll();
        }

        public static Task StopRickRoll()
        {
            rolling = false;
            if (lastRollByChannel.ContainsKey(rollingChannelId))
            {
                lastRollByChannel[rollingChannelId] = DateTime.Now;
            }else
            {
                lastRollByChannel.Add(rollingChannelId, DateTime.Now);
            }
            return Task.CompletedTask;
        }

        #endregion
        #region Haddaway, What is Love

        private static string HaddawayGamesToString()
        {
            string result = "";
            foreach(var entry in haddawayGames)
            {
                result += "Channel " + entry.Key
                    + ", at index " + entry.Value
                    + "/" + haddawayLyrics.Length
                    + " (" + string.Format("{0:0%}", entry.Value/(float)haddawayLyrics.Length) + " complete)\n";
            }
            return result.Substring(0,result.Length-1);
        }

        public static Task HaddawayStatus(SocketCommandContext context)
        {
            string display = "Haddaway status: ";
            if (haddawayGames == null||haddawayGames.Count==0)
            {
                display += "No games running";
            }
            else
            {
                display += haddawayGames.Count + " games running\n"
                + HaddawayGamesToString();
            }

            EmbedBuilder builder = new EmbedBuilder
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = display
            };

            //  Send problem message via direct-message to the user            
            context.Channel.SendMessageAsync("", false, builder.Build());

            return Task.CompletedTask;
        }

        private static string[] haddawayLyrics = new string[]
        {
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Baby don't hurt me",
            "don't hurt me",
            "No more",
            "What is love?",
            "Yeah",
            "I don't know why you're not fair",
            "I give you my love",
            "but you don't care",
            "So what is right and what is wrong?",
            "Gimme a sign",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Oh, I don't know",
            "what can I do?",
            "What else can I say",
            "it's up to you",
            "I know we're one",
            "just me and you",
            "I can't go on",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?",
            "What is love?",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Don't hurt me",
            "Don't hurt me",
            "I want no other",
            "no other lover",
            "This is our life",
            "our time",
            "We are together I need you forever",
            "Is it love?",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Yeah, yeah,\n(woah-woah-woah, oh, oh)\n(woah-woah-woah, oh, oh)",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "What is love?"
        };
        
        /// <summary>
        /// Key: channelId
        /// Value: current lyric index
        /// </summary>
        private static Dictionary<ulong, int> haddawayGames;

        /// <summary>
        /// 2017-8-24
        /// Indicates if the bot is currently playing the lyric-game on the channel
        /// provided.
        /// </summary>
        public static bool IsPlayingHaddawayGameOnChannel(ulong channelId)
        {
            return haddawayGames != null
                && haddawayGames.ContainsKey(channelId);
        }

        /// <summary>
        /// 2017-8-24
        /// Rules
        /// Game of alternate entering lyrics
        /// Game continues as long as text is 90% accurate
        /// </summary>
        public static Task StartHaddaway(SocketCommandContext context, [Remainder]string lyric)
        {
            //  Lazy initialize the game library
            if (haddawayGames == null) haddawayGames = new Dictionary<ulong, int>();

            //  When game is already playing on this channel, do nothing
            if (haddawayGames.ContainsKey(context.Channel.Id)) return Task.CompletedTask;

            //  When the lyric isn't the first line of the song, do nothing
            if (GetMatchAccuracy(haddawayLyrics[0], lyric) < 0.8) return Task.CompletedTask;

            //  Otherwise, start a game
            context.Channel.SendMessageAsync(haddawayLyrics[1]);
            haddawayGames.Add(context.Channel.Id, 2);

            //  NOTE
            //  To capture input without requiring the command,
            //  This code needs to be moved to a service like CleverChat
            //  and then input intercepted in the CommandHandler.   

            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-24
        /// Assumes it has already been confirmed there is a game in progress
        /// on the provided channelId. Probably using IsPlayingHaddawayGameOnChannel()
        /// </summary>
        /// <param name="channelId">channel Id where the game is confirmed to be running</param>
        /// <param name="userLyric">lyrics entered by user</param>
        /// <returns></returns>
        public static Task UpdateHaddawayGame(SocketCommandContext context, [Remainder]string userLyric)
        {
            //  lyric index the user must match
            ulong channelId = context.Channel.Id;
            int currentIndex = haddawayGames[channelId];
            string currentLyric = haddawayLyrics[currentIndex];

            float matchPercent = GetMatchAccuracy(currentLyric, userLyric);

            //  Yes, check for end game twice. Because depending on who has
            //  the last line, end game can occur before the bot's line
            //  (when the player has the last line) or after the bot's line
            //  (when the bot has the last line).
            if (matchPercent > 0.8)
            {
                haddawayGames[channelId]++;

                //  Check for end game
                if (haddawayGames[channelId] >= haddawayLyrics.Length)
                {
                    //  You win
                    context.Channel.SendMessageAsync("We did the whole song!! Awesome!");
                    haddawayGames.Remove(channelId);
                    return Task.CompletedTask;
                }

                //  Continue game
                string displayLyric = haddawayLyrics[haddawayGames[channelId]];
                haddawayGames[channelId]++;
                context.Channel.SendMessageAsync(displayLyric);

                //  Check for end game
                if (haddawayGames[channelId]>=haddawayLyrics.Length) {
                    //  You win
                    context.Channel.SendMessageAsync("We did the whole song!! Awesome!");
                    haddawayGames.Remove(channelId);
                    return Task.CompletedTask;
                }
            }
            else
            {
                //  game over
                string display;
                float successRatio = currentIndex / (float)haddawayLyrics.Length;
                if (currentIndex > 2)
                {
                    if (successRatio < 0.5)
                    {
                        display = "Aw, we barely made " + string.Format("{0:0%}", successRatio) + " of the song!";
                    }
                    else
                    {
                        display = "Not bad! We got more than " + string.Format("{0:0%}", successRatio) + " of the way through the song!";
                    }
                    context.Channel.SendMessageAsync(display);
                }
                haddawayGames.Remove(channelId);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-24
        /// Returns the match percentage, from 0 to 1.
        /// Uses the Levenshtein distance calculation to deterimine accurace.
        /// Ref: https://en.wikipedia.org/wiki/Levenshtein_distance
        /// </summary>
        public static float GetMatchAccuracy(string left, string right)
        {
            left = left.ToLower();
            right= right.ToLower();

            int n = left.Length;
            int m = right.Length;
            int[,] d = new int[n + 1, m + 1];

            //  Exit conditions
            if (n == 0) return m;
            if (m == 0) return n;

            //  Iterate the strings
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            //  Magic
            for(int i = 1; i <= n; i++)
            {
                for(int j = 1; j <= m; j++)
                {
                    int cost = (left[i - 1] == right[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            //  Levenshtein distance is d[n,m]
            //  To convert to a percentage, normalize against longest string:
            //      ("longest length" - "edit distance") / "longest length"

            return (Math.Max(n, m) - d[n, m]) / (float)Math.Max(n, m);
        }

        #endregion
    }
}
