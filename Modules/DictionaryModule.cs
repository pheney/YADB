using Discord.Commands;
using YADB.Preconditions;
using System.Linq;
using System.Threading.Tasks;
using YADB.Services;
using System;
using YADB.Common;

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
            await ReplyAsync("**" + word.ToUpper() + "**");
            await Definition(word);
            await Syn(word);
            await Ant(word);
        }

        [Command("#synonym"), Alias("#syn")]
        [Remarks("Returns words with similar meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task Synonym(string word)
        {
            await ReplyAsync("**" + word.ToUpper() + "**");
            await Syn(word);
        }

        [Command("#antonym"), Alias("#ant")]
        [Remarks("Returns words with opposite meanings")]
        [MinPermissions(AccessLevel.User)]
        public async Task Antonym(string word)
        {
            await ReplyAsync("**" + word.ToUpper() + "**");
            await Ant(word);
        }

        #region Private Helpers
        
        private async Task Definition(string word)
        {
            string[] results = null;
            await OED.GetDefinition(word, out results);
            if (results==null)
            {
                await ReplyAsync("No definition found");
            }
            else
            {
                await ReplyAsync("\n_meaning_\n");
                for (int i = 0;i <results.Length;i++) {
                    string display = results[i];

                    //  Ensure each result is less than 2000 characters.
                    //  The maximum permitted message length on Discord.
                    if (display.Length > 2000)
                    {
                        display = display.Substring(0, Math.Min(1997, display.Length)) + "...";
                    } 
                    await ReplyAsync("  **"+i+"**: " + display);
                }
            }
        }

        private async Task Syn(string word)
        {
            string[] results = null; 
            await OED.GetSynonym(word, out results);
            if (results == null)
            {
                await ReplyAsync("No synonyms found");
            }
            else
            {
                await ReplyAsync("\n_synonym_\n");

                //  Ensure results are less than 2000 characters.
                //  The maximum permitted message length on Discord.
                string display = results.JoinWith(", ");
                if (display.Length > 2000)
                {
                    display = display.Substring(0, Math.Min(1997, display.Length)) + "...";
                }
                await ReplyAsync(display);
            }
        }

        private async Task Ant(string word) {

            string[] results = null;
            await OED.GetAntonym(word, out results);
            if (results == null)
            {
                await ReplyAsync("No antonyms found");
            }
            else
            {
                await ReplyAsync("\n_antonym_\n");

                //  Ensure results are less than 2000 characters.
                //  The maximum permitted message length on Discord.
                string display = results.JoinWith(", ");
                if (display.Length > 2000)
                {
                    display = display.Substring(0, Math.Min(1997, display.Length)) + "...";
                }
                await ReplyAsync(display);
            }
        }

        #endregion
    }
}
