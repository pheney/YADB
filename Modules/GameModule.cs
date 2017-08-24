using Discord.Commands;
using YADB.Preconditions;
using System.Threading.Tasks;
using YADB.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace YADB.Modules
{
    [Name("Games")]
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        #region Magic Eight Ball

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
            }
        }

        #endregion
        #region Rickroll

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
        private static Dictionary<ulong, DateTime> lastRollByChannel;
        private static Thread rollThread;
        private static TimeSpan rollFrequency = new TimeSpan(1, 0, 0);

        [Command("#rickroll"), Alias("#rr")]
        [Remarks("We're no strangers to love")]
        [MinPermissions(AccessLevel.User)]
        public async Task RickRoll([Remainder]string abort = null)
        {
            if (lastRollByChannel == null) lastRollByChannel = new Dictionary<ulong, DateTime>();

            //  When called parameterless,
            //  Try to start a RickRoll.
            if (abort == null)
            {
                //  Conditions for start:
                //  1. There is not already one going
                //  2. It has been "awhile" since one happened (on this channel)

                //  When there is already a roll going, arbort.
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

            //  When called with any parameter at all, 
            //  Stop the RickRoll.
            if (rolling) await StopRickRoll();
            else await ReplyAsync("Who's Rick-rolling around here?");
        }

        private Task StartRickRoll()
        {
            rolling = true;
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

        private Task StopRickRoll()
        {
            rolling = false;
            ulong channelId = Context.Channel.Id;
            if (lastRollByChannel.ContainsKey(channelId))
            {
                lastRollByChannel[channelId] = DateTime.Now;
            }else
            {
                lastRollByChannel.Add(channelId, DateTime.Now);
            }
            return Task.CompletedTask;
        }

        #endregion
        #region Haddaway, What is Love

        private static string[] haddawayLyrics = new string[]
        {
            "What is love?",
            "Baby don't hurt me",
            "Don't hurt me",
            "No more",
            "Baby don't hurt me, don't hurt me",
            "No more",
            "What is love?",
            "Yeah",
            "I don't know why you're not fair",
            "I give you my love, but you don't care",
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
            "Oh, I don't know, what can I do?",
            "What else can I say, it's up to you",
            "I know we're one, just me and you",
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
            "I want no other, no other lover",
            "This is our life, our time",
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
            "Yeah, yeah,\n(woah-woah-woah, oh, oh)\n(Woah-woah-woah, oh, oh)",
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

        private static int haddawayIndex;

        [Command("what is love")]
        [Remarks("baby don't hurt me")]
        [MinPermissions(AccessLevel.User)]
        public async Task Haddaway([Remainder]string lyric = null)
        {
            //  TODO
            //  Game -- alternate entering lyrics
            //  Game continues as long as text is 90% accurate
            //  haddawayIndex indicates progress

            //  NOTE
            //  To capture input without requiring the command,
            //  This code needs to be moved to a service like CleverChat
            //  and then input intercepted in the CommandHandler.

            await ReplyAsync("baby don't hurt me");
        }

        #endregion
    }
}
