using Discord.Commands;
using YADB.Preconditions;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Modules
{
    [Name("Games")]
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        private static string[] EightBall = new string[]
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
        public async Task Mult([Remainder]string question = null)
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

                await ReplyAsync("\"" + EightBall.Random() + "\"");
            }
        }        
    }
}
