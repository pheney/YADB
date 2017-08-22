using Discord.Commands;
using YADB.Preconditions;
using System.Linq;
using System.Threading.Tasks;
using YADB.Services;
using System;

namespace YADB.Modules
{
    [Name("Query Commands")]
    public class DictionaryModule:ModuleBase<SocketCommandContext>
    {
        [Command("#homonym"), Alias("#hom")]
        [Remarks("Lists or explains frequently misused homonyms")]
        [MinPermissions(AccessLevel.User)]
        public async Task Homonym(string word =null)
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

        [Command("#define"),Alias("#def")]
        [Remarks("Returns the meaning of a word")]
        [MinPermissions(AccessLevel.User)]
        public async Task Define(string word)
        {
            await ReplyAsync("**" + word + "**");
            await Definition(word);
            await Syn(word);
            await Ant(word);
        }

        [Command("#synonym"), Alias("#syn")]
        [Remarks("Returns words with similar meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task Synonym(string word)
        {
            await ReplyAsync("**" + word + "**");
            await Syn(word);
        }

        [Command("#antonym"), Alias("#ant")]
        [Remarks("Returns words with opposite meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task Antonym(string word)
        {
            await ReplyAsync("**" + word + "**");
            await Ant(word);
        }

        #region Private Helpers

        private string LimitLength(string source, int maxLength)
        {
            source = source.Replace(",", ", ");
            source = source.Substring(0, Math.Min(source.Length, maxLength));
            int lastIndex = source.LastIndexOf(", ");
            if (lastIndex > -1) source = source.Substring(0, lastIndex);
            return source;
        }

        private async Task Definition(string word)
        {
            string results = "";
            await OED.GetDefinition(word, out results);
            if (word.Equals(results, System.StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("No definition found");
            }
            else
            {
                //  Ensure results are less than 2000 characters.
                //  The maximum permitted message length on Discord.
                if (results.Length>2000)
                {
                    results = results.Substring(0, Math.Min(1997, results.Length)) + "...";
                }
                await ReplyAsync("\n_meaning_\n" + results);
            }
        }

        private async Task Syn(string word)
        {
            string results = "";
            await OED.GetSynonym(word, out results);
            if (word.Equals(results, System.StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("No synonyms found");
            }
            else
            {
                //  Ensure results are less than 2000 characters.
                //  The maximum permitted message length on Discord.
                await ReplyAsync("\n_synonym_\n" + LimitLength(results, 2000));
            }
        }

        private async Task Ant(string word) { 

            string results = "";
            await OED.GetAntonym(word, out results);
            if (word.Equals(results, System.StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("No antonyms found");
            }
            else
            {
                //  Ensure results are less than 2000 characters.
                //  The maximum permitted message length on Discord.
                await ReplyAsync("\n_antonym_\n" + LimitLength(results, 2000));
            }
        }

        #endregion
    }
}
