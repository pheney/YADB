using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Services
{
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

        public static async Task Save()
        {
            if (InsultData.Get == null) return;
            await FileOperations.SaveAsJson(InsultData.Get);
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

            public string[] ColumnAbasic = new string[] { "fat" };

            public string[] ColumnA = new string[] { "pathetic" };

            public string[] ColumnB = new string[] { "butt" };

            public string[] ColumnBvulgar = new string[] { "douche" };

            public string[] ColumnC = new string[] { "canoe" };

            public string[] ColumnCvulgar = new string[] { "pilot" };

            public string[] Australian = new string[] { "cunt" };

            public string[] Teen = new string[] { "shit" };

            public string[] Teening = new string[] { "fucking" };

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

        private static List<string> Combine(params string[][] arrays)
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
                    result = Select(InsultData.Get.ColumnAbasic, "");
                    break;
                case 1:
                    //  Angsty Teen (basic vocabulary, Teen-ing)
                    result = Select(Combine(InsultData.Get.ColumnAbasic, InsultData.Get.Teening), "");
                    break;
                default:
                    //  British, Australian (full vocabulary)
                    result = Select(Combine(InsultData.Get.ColumnAbasic, InsultData.Get.ColumnA), "");
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
                    result = Select(InsultData.Get.ColumnB, firstWord);
                    break;
                case 1:
                    //  Angsty Teen (yes profanity, Teen language)
                    result = Select(Combine(InsultData.Get.ColumnB, InsultData.Get.ColumnBvulgar, InsultData.Get.Teen), firstWord);
                    break;
                case 2:
                    //  British (yes profanity)
                    result = Select(Combine(InsultData.Get.ColumnB, InsultData.Get.ColumnBvulgar), firstWord);
                    break;
                default:
                    //  Australian (yes profanity, Australian language)
                    result = Select(Combine(InsultData.Get.ColumnBvulgar, InsultData.Get.Australian), firstWord);
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
                    result = Select(InsultData.Get.ColumnC, secondWord);
                    break;
                case 1:

                    //  Angsty Teen (use profanity, Teen language)
                    result = Select(Combine(InsultData.Get.ColumnC, InsultData.Get.ColumnCvulgar, InsultData.Get.Teen), secondWord);
                    break;
                case 2:
                    //  British (use profanity)
                    result = Select(Combine(InsultData.Get.ColumnC, InsultData.Get.ColumnCvulgar), secondWord);
                    break;
                default:
                    //  Australian (use profanity, Australian language)
                    result = Select(Combine(InsultData.Get.ColumnC, InsultData.Get.ColumnCvulgar, InsultData.Get.Australian), secondWord);
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

    }
}
