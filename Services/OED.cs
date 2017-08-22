using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Services
{
    /// <summary>
    /// 2017-8-22
    /// Ref: https://developer.oxforddictionaries.com/documentation
    /// </summary>
    public static class OED
    {
        #region Homonym Data

        public class Homophone
        {
            private static int _id = 0;
            public int id;
            public string word, definition;

            public Homophone(string word, string definition, bool increment = false)
            {
                if (increment) _id++;
                this.id = _id;
                this.word = word;
                this.definition = definition;
            }
            public bool Equals(string word)
            {
                return this.word.Equals(word, System.StringComparison.OrdinalIgnoreCase);
            }
            public override string ToString()
            {
                return word + " : " + definition;
            }
        }

        /// <summary>
        /// List of the most common offenders.
        /// Ref: http://grammarist.com/homophones/
        /// </summary>
        private static Homophone[] h = new Homophone[]
        {
            new Homophone("their", "belonging to them", true),
            new Homophone("they're", "they are"),
            new Homophone("there", "a location"),
            new Homophone("to", "towards", true),
            new Homophone("too", "also"),
            new Homophone("two", "the number between 1 and 3"),
            new Homophone("mold", "fungas that grows in moist damp places, or the act of shaping something", true),
            new Homophone("mould", "container used to shape molten liquid like metal or Jello"),
            new Homophone("weather", "sun, rain, clouds", true),
            new Homophone("whether", "a choice"),
            new Homophone("carrot", "vegetable", true),
            new Homophone("caret", "a proofreading symbol ^"),
            new Homophone("karat", "measure of purity of gold"),
            new Homophone("carat", "unit of weight for diamonds"),
            new Homophone("affect", "to change or to pretend",true),
            new Homophone("effect", "a result"),
            new Homophone("laser", "a beam of coherent light", true),
            new Homophone("lazer", "a gross mispelling of 'laser'"),
            new Homophone("roll", "to turn over, (roll a die), a sequence (on a roll), a list (roll call)", true),
            new Homophone("role", "a position or function"),
            new Homophone("Bazaar", "place to shop", true),
            new Homophone("bizarre", "weird"),
            new Homophone("your", "belonging to you", true),
            new Homophone("you're", "you are"),
            new Homophone("turret", "big thing that rotates a gun, or a tall round tower on a castle wall", true),
            new Homophone("turrent", "shameful mispelling of 'turret'"),
            new Homophone("Tourette", "neuropsychiatric disorder, or a weak escuse by the uneducated to use excessive profanity"),
            new Homophone("here", "a location", true),
            new Homophone("hear", "to listen"),
            new Homophone("farther", "comparison of physical distance", true),
            new Homophone("further", "copmarison of figurative distance"),
            new Homophone("bored", "nothing to do, or to have punctured with a hole", true),
            new Homophone("board", "a piece of wood"),
            new Homophone("canon", "general principle or criteria by which something is judged", true),
            new Homophone("cannon", "a big gun"),
            new Homophone("buy", "to purchase", true),
            new Homophone("by", "atributed to someone"),
            new Homophone("where", "a location", true),
            new Homophone("wear", "to put on a piece of clothing"),
            new Homophone("through", "to penetrate", true),
            new Homophone("thru", "horrible mispelling of 'through'"),
            new Homophone("threw", "to forcefully project")
        };

        /// <summary>
        /// 2017-8-22
        /// Returns an EmbedBuilder object with formatted contents
        /// containing all matching homophones of the provided word.
        /// When no results are found, a website is provided that has
        /// a complete list of homophones and their definitions.
        /// </summary>
        public static EmbedBuilder GetHomophone(string word)
        {
            return GetBuilderFor(GetAllHomophones(word));
        }

        /// <summary>
        /// 2017-8-22
        /// Returns all words that sound alike, but are spelt different.
        /// </summary>
        private static Homophone[] GetAllHomophones(string word)
        {
            Homophone found = h.Where(x => x.Equals(word)).FirstOrDefault();
            if (found == null) return null;
            Homophone[] result = h.Where(x => x.id == found.id).ToArray();
            return result;
        }

        private static EmbedBuilder GetBuilderFor(Homophone[] homophones)
        {
            string description = "";
            if (homophones != null)
            {
                foreach (var h in homophones) description += h.ToString() + "\n";
                description = description.Substring(0, description.Length - 1);
            } else
            {
                description = "No results found. Try http://grammarist.com/homophones/";
            }

            return new EmbedBuilder()
            {
                Color = Constants.SlateBlue,
                Description = description
            };
        }

        #endregion
        #region Connection Data

        static string protocol = "https";
        static string endpoint = "od-api.oxforddictionaries.com";
        static string port = "443";
        static string version = "v1";
        static string lang = "en";
        static string template = "{protocol}://{endpoint}:{port}/api/{version}/entries/{lang}/{word}";

        static string id = "4a62e7ee76";
        static string key = "c72e5c078cbd8202e07d6cee6f95efac4e";
        static string de = "2";

        #endregion
        #region API

        public static Task GetDefinition(string word, out string definition)
        {
            SuggestWordsFor(word, "definitions", out definition);
            return Task.CompletedTask;
        }

        public static Task GetSynonym(string word, out string synonym)
        {
            SuggestWordsFor(word, "synonyms", out synonym);
            return Task.CompletedTask;
        }

        public static Task GetAntonym(string word, out string antonym)
        {
            SuggestWordsFor(word, "antonyms", out antonym);
            return Task.CompletedTask;
        }

        #endregion
        #region Helpers

        /// <summary>
        /// 2017-8-17
        /// Sample URL: https://od-api.oxforddictionaries.com:443/api/v1/entries/en/ace/synonyms
        /// </summary>
        /// <param name="word">word to look up</param>
        /// <param name="wordType">synonyms, antonyms, etc</param>
        /// <returns></returns>
        private static Task SuggestWordsFor(string word, string wordType, out string result)
        {
            result = word;

            //  query string
            string fullurl = template
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());

            //  alternate word type
            fullurl += "/" + wordType.ToLower();

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(fullurl);
            webRequest.ContentType = "application/json";
            webRequest.Headers.Add("app_id", decrypt(id, de));
            webRequest.Headers.Add("app_key", decrypt(key, de));

            //  Create an empty response
            WebResponse webResp = null;

            try
            {
                //  Execute the request and put the result into response
                webResp = webRequest.GetResponse();
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(webResp.GetResponseStream(), encoding))
                {
                    //  Convert the json string to a json object
                    JObject json = (JObject)JsonConvert.DeserializeObject(reader.ReadToEnd());

                    //  Find synonyms
                    var found = GetAlternateWords(json, wordType);
                    string[] resultArray = JObjectListToStringArray(found, "text");
                    result = string.Join(",", resultArray);
                }
            }
            catch (WebException)
            {
                //  404	: No entry is found matching supplied id and source_lang or filters are not recognized
                //  500 : Internal Error. An error occurred while processing the data.
                //  Do nothing -- by default this returns the original word if
                //  no alternates are found.
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sample input:
        ///     "synonyms": [
        ///         {
        ///             "id": "wunderkind",
        ///             "language": "en",
        ///             "text": "wunderkind"
        ///         },
        ///         {
        ///             "id": "hotshot",
        ///             "language": "en",
        ///             "text": "hotshot"
        ///         }
        ///     ]
        /// Sample output:
        ///     [ "wunderkind", "hotshot" ]
        /// </summary>
        private static List<JObject> GetAlternateWords(JObject source, string type)
        {
            List<JObject> result = new List<JObject>();

            foreach (var item in source)
            {
                if (item.Key.Equals(type))
                {
                    //  The synonym object is ALWAYS an array of objects
                    //  although it may be an empty array
                    foreach (var syn in item.Value)
                    {
                        result.Add((JObject)syn);
                    }
                    continue;
                }

                bool valueIsJsonObject = item.Value.Type.Equals(JTokenType.Object);
                if (valueIsJsonObject)
                {
                    result.AddRange(GetAlternateWords((JObject)item.Value, type));
                    continue;
                }

                bool valueIsArray = item.Value.Type.Equals(JTokenType.Array);
                if (valueIsArray)
                {
                    foreach (var arrayItem in item.Value)
                    {
                        if (arrayItem.Type.Equals(JTokenType.Object))
                        {
                            result.AddRange(GetAlternateWords((JObject)arrayItem, type));
                        }
                        continue;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// tokenName = "text"
        /// </summary>
        /// <param name="source"></param>
        /// <param name="tokenName"></param>
        /// <returns></returns>
        private static string[] JObjectListToStringArray(List<JObject> source, string tokenName)
        {
            List<string> results = new List<string>();

            for (int i = 0; i < source.Count; i++)
            {
                results.AddUnique(source[i].Value<string>(tokenName));
            }
            return results.ToArray();
        }

        /// <summary>
        /// 2017-8-17
        /// </summary>
        private static string decrypt(string source, string key)
        {
            return source.Substring(int.Parse(key));
        }

        #endregion
    }
}
