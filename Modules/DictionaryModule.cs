using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using YADB.Common;
using YADB.Preconditions;
using YADB.Services;

namespace YADB.Modules
{
    [Name("Grammar Commands")]
    public class DictionaryModule : ModuleBase<SocketCommandContext>
    {
        private static string NoResultsFound = "No results found for \"{word}\".\n\n"
            + "Try the Root (unconjugated) form of the word. "
            + "The root form is the infinitive form with \"to\" removed, "
            + "i.e., swimming -> to swim -> swim. For nouns, try removing all "
            + "prefixes and suffixes, i.e., anthropocentrism -> anthropocentric.";

        [Command(".define"), Alias(".def")]
        [Remarks("Returns the meaning of a word")]
        [MinPermissions(AccessLevel.User)]
        public async Task DefineWord(string word, string maxDefinitions = null)
        {
            int defaultCount = 3;
            int showCount = defaultCount;
            if (maxDefinitions != null)
            {
                if (!int.TryParse(maxDefinitions, out showCount))
                {
                    showCount = defaultCount;
                }
            }
            string[] results;
            await OED.GetDefinition(word, out results);
            string definition = Italic("meaning") + "\n";
            if (results == null || results.Length == 0) definition += NoResultFor(word);
            else definition += results.EnumerateArray(showCount);

            var builder = new EmbedBuilder()
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = (Bold(word) + ", " + definition).CapLength()
            };

            await ReplyAsync("", false, builder);
        }

        [Command(".synonym"), Alias(".syn")]
        [Remarks("Returns words with similar meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task SynonymForWord(string word, string maxDefinitions = null)
        {
            int defaultCount = 3;
            int showCount = defaultCount;
            if (maxDefinitions != null)
            {
                if (!int.TryParse(maxDefinitions, out showCount))
                {
                    showCount = defaultCount;
                }
            }
            string[] results;
            await OED.GetSynonym(word, out results);
            string synonyms = Italic("synonyms") + "\n";
            if (results == null || results.Length == 0) synonyms += NoResultFor(word);
            else synonyms += results.JoinWith(", ", 0, showCount);

            var builder = new EmbedBuilder()
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = (Bold(word) + ", " + synonyms).CapLength()
            };

            await ReplyAsync("", false, builder);
        }

        [Command(".antonym"), Alias(".ant")]
        [Remarks("Returns words with opposite meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task AntonymForWord(string word, string maxDefinitions = null)
        {
            int defaultCount = 3;
            int showCount = defaultCount;
            if (maxDefinitions != null)
            {
                if (!int.TryParse(maxDefinitions, out showCount))
                {
                    showCount = defaultCount;
                }
            }
            string[] results;
            await OED.GetAntonym(word, out results);
            string antonyms = Italic("antonyms") + "\n";
            if (results == null || results.Length == 0) antonyms += NoResultFor(word);
            else antonyms += results.JoinWith(", ", 0, showCount);

            var builder = new EmbedBuilder()
            {
                Color = Constants.SeverityToColor(Constants.MessageSeverity.Info),
                Description = (Bold(word) + ", " + antonyms).CapLength()
            };

            await ReplyAsync("", false, builder);
        }

        [Command(".homonym"), Alias(".hom")]
        [Remarks("Lists or explains frequently misused homonyms")]
        [MinPermissions(AccessLevel.User)]
        public async Task HomonymForWord(string word = null)
        {
            if (word != null)
            {
                //  Explain a specific word
                await ReplyAsync("", false, OED.GetHomophone(word));
                return;
            }

            //  Provide list of frequently misused homonyms                       
            await ReplyAsync("http://grammarist.com/homophones/");
        }

        #region Private Helpers

        private static string NoResultFor(string word)
        {
            return NoResultsFound.Replace("{word}", word);
        }

        private static string Italic(string word)
        {
            return "_" + word + "_";
        }

        private static string Bold(string word)
        {
            return "**" + word + "**";
        }

        #endregion
    }
}
