using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Services
{
    /// <summary>
    /// 2017-9-1
    /// 
    /// For nice things to say, use this:
    /// https://quotes.rest/#!/qod/get_qod
    /// </summary>
    public static class Insult
    {
        #region Public API

        public static string GetInsult(int vulgarity = 2)
        {
            if (InsultData.Get == null) InsultData.Init();

            string first = SelectFirstWord(vulgarity);
            string second = SelectSecondWord(vulgarity, first);
            string third = SelectThirdWord(vulgarity, second);

            string insult = first + " " + second + " " + third;
            string def = Insult.GetDefiniteArticle(insult);
            string indef = Insult.GetIndefiniteArticle(insult);
            string phrase = Insult.GetPhrase();

            string result = phrase
                .Replace("{def}", def)
                .Replace("{indef}", indef)
                .Replace("{insult}", insult);
            return result;
        }
        
        public static string GetInsult(string username, int vulgarity = 2)
        {
            return username + ", " + GetInsult(vulgarity);
        }

        public enum WordType { Phrase, Adjective, SophisticatedAdjective,
            AdjectivalNoun, VulgarAdjectivalNoun, Noun, VulgarNoun, ActiveVerb
        }

        public static string[] ShowTerms(WordType wordType)
        {
            if (InsultData.Get == null) InsultData.Init();
            string[] result = null;
            switch (wordType)
            {
                case WordType.Phrase:
                    result = InsultData.Get.Phrases;
                    break;
                case WordType.Adjective:
                    result = InsultData.Get.SimpleAdjectives;
                    break;
                case WordType.SophisticatedAdjective:
                    result = InsultData.Get.SophisticatedAdjectives;
                    break;
                case WordType.AdjectivalNoun:
                    result = InsultData.Get.SimpleAdjectivalNouns;
                    break;
                case WordType.VulgarAdjectivalNoun:
                    result = InsultData.Get.VulgarAdjectivalNouns;
                    break;
                case WordType.Noun:
                    result = InsultData.Get.SimpleNouns;
                    break;
                case WordType.VulgarNoun:
                    result = InsultData.Get.VulgarNouns;
                    break;
                case WordType.ActiveVerb:
                    result = InsultData.Get.ActiveFormVerbs;
                    break;
            }
            return result;
        }

        public static async Task AddPhrase(string phrase)
        {
            if (InsultData.Get == null) InsultData.Init();
            if (phrase.Length == 0) return;
            InsultData.Get.Phrases = Combine(InsultData.Get.Phrases, new string[] { phrase });
            await Save();
        }

        public static async Task RemovePhrase(string phrase)
        {
            if (InsultData.Get == null) InsultData.Init();
            if (phrase.Length == 0) return;
            InsultData.Get.Phrases = Remove(InsultData.Get.Phrases, new string[] { phrase });
            await Save();
        }

        public static async Task AddTerms(string[] words, WordType wordType)
        {
            if (InsultData.Get == null) InsultData.Init();
            if (words.Length == 0) return;
            switch (wordType)
            {
                case WordType.Adjective:
                    InsultData.Get.SimpleAdjectives = Combine(InsultData.Get.SimpleAdjectives, words);
                    break;
                case WordType.SophisticatedAdjective:
                    InsultData.Get.SophisticatedAdjectives= Combine(InsultData.Get.SophisticatedAdjectives, words);
                    break;
                case WordType.AdjectivalNoun:
                    InsultData.Get.SimpleAdjectivalNouns = Combine(InsultData.Get.SimpleAdjectivalNouns, words);
                    break;
                case WordType.VulgarAdjectivalNoun:
                    InsultData.Get.VulgarAdjectivalNouns = Combine(InsultData.Get.VulgarAdjectivalNouns, words);
                    break;
                case WordType.Noun:
                    InsultData.Get.SimpleNouns = Combine(InsultData.Get.SimpleNouns, words);
                    break;
                case WordType.VulgarNoun:
                    InsultData.Get.VulgarNouns = Combine(InsultData.Get.VulgarNouns, words);
                    break;
                case WordType.ActiveVerb:
                    InsultData.Get.ActiveFormVerbs= Combine(InsultData.Get.ActiveFormVerbs, words);
                    break;
            }
            await Save();
        }

        public static async Task RemoveTerms(string[] words, WordType wordType)
        {
            if (InsultData.Get == null) InsultData.Init();
            if (words.Length == 0) return;
            switch (wordType)
            {
                case WordType.Adjective:
                    InsultData.Get.SimpleAdjectives = Remove(InsultData.Get.SimpleAdjectives, words);
                    break;
                case WordType.SophisticatedAdjective:
                    InsultData.Get.SophisticatedAdjectives = Remove(InsultData.Get.SophisticatedAdjectives, words);
                    break;
                case WordType.AdjectivalNoun:
                    InsultData.Get.SimpleAdjectivalNouns = Remove(InsultData.Get.SimpleAdjectivalNouns, words);
                    break;
                case WordType.VulgarAdjectivalNoun:
                    InsultData.Get.VulgarAdjectivalNouns = Remove(InsultData.Get.VulgarAdjectivalNouns, words);
                    break;
                case WordType.Noun:
                    InsultData.Get.SimpleNouns = Remove(InsultData.Get.SimpleNouns, words);
                    break;
                case WordType.VulgarNoun:
                    InsultData.Get.VulgarNouns = Remove(InsultData.Get.VulgarNouns, words);
                    break;
                case WordType.ActiveVerb:
                    InsultData.Get.ActiveFormVerbs = Remove(InsultData.Get.ActiveFormVerbs, words);
                    break;
            }
            await Save();
        }
        
        #endregion
        #region Data

        [Serializable]
        private class InsultData
        {
            [JsonIgnore]
            public static InsultData Get;

            #region Data

            public string[] Phrases = new string[] { "" };

            public string[] VulgarityOptions = new string[] {
            "Child", "American", "British", "Australian" };

            public string[] SimpleAdjectives = new string[] { "fat" };

            public string[] SophisticatedAdjectives = new string[] { "pathetic" };

            public string[] SimpleAdjectivalNouns = new string[] { "butt" };

            public string[] VulgarAdjectivalNouns = new string[] { "douche" };

            public string[] SimpleNouns = new string[] { "canoe" };

            public string[] VulgarNouns = new string[] { "pilot" };

            public string[] Australian = new string[] { "cunt" };

            public string[] Teen = new string[] { "shit" };

            public string[] ActiveFormVerbs = new string[] { "fucking" };

            #endregion

            public static void Init()
            {
                string FileName = new InsultData().GetFilename();
                string file = FileOperations.PathToFile(FileName);

                // When the file does NOT exists, create it
                if (!FileOperations.Exists(FileName))
                {
                    //  Create a new configuration object
                    var data = new InsultData();
                                       
                    //  Save the configuration object
                    FileOperations.SaveAsJson(data);
                }

                InsultData.Get = FileOperations.Load<InsultData>();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(FileName + " loaded");
                Console.ResetColor();
            }
        }

        #endregion

        //  Patrick Heney (C) 2017
        //  This is mean purely for fun. Even the bit about Australian profanity.
        //  The language selection is an after-the-fact hack. Ideally this would
        //  be built with a database.

        private const int redundancyDepth = 1;

        /* Rules:
          Child - no profanity, basic vocabulary
          Angsty Teen - all profanity, basic vocabulary, lots more "fuck"
          British - all profanity, full vocabulary
          Australian - all profanity, full vocabulary, lots more "cunt"
        */

        private static bool MatchToDepth(string wordA, string wordB, int depth = redundancyDepth)
        {
            if (!string.IsNullOrWhiteSpace(wordA) && string.IsNullOrWhiteSpace(wordB)) return false;

            return wordA.Substring(0, depth).Equals(wordB.Substring(0, depth), StringComparison.OrdinalIgnoreCase);
        }

        private static string Select(string[] choices, string previousWord)
        {

            string result = "";
            do
            {
                result = choices.Random();
            } while (MatchToDepth(result, previousWord));
            return result;
        }

        private static string Select(List<string> choices, string previousWord)
        {
            string result = "";
            do
            {
                result = choices.Random();
            } while (MatchToDepth(result, previousWord));
            return result;
        }

        private static List<string> CombineToList(params string[][] arrays)
        {
            List<string> words = new List<string>();
            foreach (string[] a in arrays) words.AddRange(a.ToList());
            return words;
        }

        private static string SelectFirstWord(int vulgarIndex)
        {
            string result;

            switch (vulgarIndex)
            {
                case 0:
                    //  Child (basic vocabulary)
                    result = Select(InsultData.Get.SimpleAdjectives, "");
                    break;
                case 1:
                    //  Angsty Teen (basic vocabulary, Teen-ing)
                    result = Select(CombineToList(InsultData.Get.SimpleAdjectives, InsultData.Get.ActiveFormVerbs), "");
                    break;
                default:
                    //  British, Australian (full vocabulary)
                    result = Select(CombineToList(InsultData.Get.SimpleAdjectives, InsultData.Get.SophisticatedAdjectives), "");
                    break;
            }

            return result;
        }

        private static string SelectSecondWord(int vulgarIndex, string firstWord)
        {
            string result;
            switch (vulgarIndex)
            {
                case 0:
                    //  Child (no profanity)
                    result = Select(InsultData.Get.SimpleAdjectivalNouns, firstWord);
                    break;
                case 1:
                    //  Angsty Teen (yes profanity, Teen language)
                    result = Select(CombineToList(InsultData.Get.SimpleAdjectivalNouns, InsultData.Get.VulgarAdjectivalNouns, InsultData.Get.Teen), firstWord);
                    break;
                case 2:
                    //  British (yes profanity)
                    result = Select(CombineToList(InsultData.Get.SimpleAdjectivalNouns, InsultData.Get.VulgarAdjectivalNouns), firstWord);
                    break;
                default:
                    //  Australian (yes profanity, Australian language)
                    result = Select(CombineToList(InsultData.Get.VulgarAdjectivalNouns, InsultData.Get.Australian), firstWord);
                    break;
            }
            return result;
        }

        private static string SelectThirdWord(int vulgarIndex, string secondWord)
        {
            string result;
            switch (vulgarIndex)
            {
                case 0:
                    //  Child (no profanity)
                    result = Select(InsultData.Get.SimpleNouns, secondWord);
                    break;
                case 1:

                    //  Angsty Teen (use profanity, Teen language)
                    result = Select(CombineToList(InsultData.Get.SimpleNouns, InsultData.Get.VulgarNouns, InsultData.Get.Teen), secondWord);
                    break;
                case 2:
                    //  British (use profanity)
                    result = Select(CombineToList(InsultData.Get.SimpleNouns, InsultData.Get.VulgarNouns), secondWord);
                    break;
                default:
                    //  Australian (use profanity, Australian language)
                    result = Select(CombineToList(InsultData.Get.SimpleNouns, InsultData.Get.VulgarNouns, InsultData.Get.Australian), secondWord);
                    break;
            }
            return result;
        }

        private static string GetPhrase()
        {
            return InsultData.Get.Phrases.Random();
        }

        private static string vowels = "aeiou";

        private static string GetIndefiniteArticle(string word)
        {
            char firstLetter = word[0];
            if (vowels.Contains(firstLetter)) return "an";
            else return "a";
        }

        private static string GetDefiniteArticle(string word)
        {
            return "the";
        }

        private static string Capitalize(this string source)
        {
            return source.Substring(0, 1).ToUpper() + source.Substring(1).ToLower();
        }

        private static string[] Combine(string[] source, string[] newWords)
        {
            List<string> words = source.ToList();
            foreach (string word in newWords)
            {
                words.AddUnique(word);
            }
            return words.ToArray();
        }

        private static string[] Remove(string[] source, string[] newWords)
        {
            List<string> words = source.ToList();
            foreach (string word in newWords)
            {
                if (words.Contains(word)) words.Remove(word);
            }
            return words.ToArray();
        }

        private static async Task Save()
        {
            if (InsultData.Get == null) return;
            await FileOperations.SaveAsJson(InsultData.Get);
        }
    }
}
