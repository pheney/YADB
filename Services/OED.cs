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
        
        //  Definition: https://od-api.oxforddictionaries.com:443/api/v1/entries/en/swim
        //  Synonym: https://od-api.oxforddictionaries.com:443/api/v1/entries/en/swim/synonyms
        static string defTemplate = "{protocol}://{endpoint}:{port}/api/{version}/entries/{lang}/{word}";

        //  Lemma: https://od-api.oxforddictionaries.com:443/api/v1/inflections/en/swimming
        static string lemmaTemplate = "{protocol}://{endpoint}:{port}/api/{version}/inflections/{lang}/{word}";

        static string id = "4a62e7ee76";
        static string key = "c72e5c078cbd8202e07d6cee6f95efac4e";
        static string de = "2";

        #endregion
        #region API

        public static Task GetDefinition(string word, out string[] definitions)
        {
            //  Get lemma for word
            string lemma = "";
            GetLemmas(word, out lemma);

            //  Build url
            string fullurl = defTemplate
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());
            
            //  Get web response
            JObject webResponse;
            GetWebData(fullurl, out webResponse);

            //  Check for 'No result'
            if (webResponse == null)
            {
                definitions = null;
                return Task.CompletedTask;
            }

            //  Find synonyms or antonyms
            GetElements(webResponse, "senses", "definitions", out definitions);
            
            return Task.CompletedTask;
        }

        public static Task GetSynonym(string word, out string[] synonyms)
        {
            //  Get lemma for word
            string lemma = "";
            GetLemmas(word, out lemma);

            //  Build url
            string fullurl = defTemplate
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());

            //  alternate word type
            fullurl += "/synonyms";

            //  Get web response
            JObject webResponse;
            GetWebData(fullurl, out webResponse);

            //  Check for 'No result'
            if (webResponse == null)
            {
                synonyms = null;
                return Task.CompletedTask;
            }

            //  Find synonyms or antonyms
            GetElements(webResponse, "synonyms", "id", out synonyms);

            //  dig out the antonyms from the web response
            return Task.CompletedTask;
        }

        public static Task GetAntonym(string word, out string[] antonyms)
        {
            //  Get lemma for word
            string lemma = "";
            GetLemmas(word, out lemma);

            //  Build url            
            string fullurl = defTemplate
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());

            //  alternate word type
            fullurl += "/antonyms";

            //  Get web response
            JObject webResponse;
            GetWebData(fullurl, out webResponse);

            //  Check for 'No result'
            if (webResponse == null)
            {
                antonyms = null;
                return Task.CompletedTask;
            }

            //  Find synonyms or antonyms
            GetElements(webResponse, "antonyms", "id", out antonyms);
                        
            //  dig out the antonyms from the web response
            return Task.CompletedTask;
        }

        #endregion
        #region JSON classes - Lemmatron

        private class Lemmatron
        {
            //  Additional Information provided by OUP
            public object metadata;

            //  A list of inflections matching a given word
            public HeadwordLemmatron[] results;
        }

        private class HeadwordLemmatron
        {
            //  The identifier of a word
            public string id;

            //  IANA language code
            public string language;

            //  A grouping of various senses in a specific language, and a lexical category that relates to a word
            public LemmatronLexicalEntry[] lexicalEntries;

            //  The json object type.Could be 'headword', 'inflection' or 'phrase'
            public string type;

            //  A given written or spoken realisation of a an entry, lowercased
            public string word;
        }

        private class LemmatronLexicalEntry
        {
            //  optional
            public GrammaticalFeaturesList gramaticalFeatures;

            //  The canonical form of words for which the entry is an inflection
            public InflectionsList inflectionOf;

            //  IANA language code
            public string language;

            //  A linguistic category of words(or more precisely lexical items), generally defined by the syntactic or morphological behaviour of the lexical item in question, such as noun or verb
            public string lexicalCategory;

            //  A given written or spoken realisation of a an entry
            public string text;
        }

        private class GrammaticalFeaturesList
        {
            public Model1 inline;
        }

        private class InflectionsList
        {
            public Model2 inline;
        }

        private class Model1
        {
            public string text;
            public string type;
        }

        private class Model2
        {
            //  identify of the word
            public string id;
            public string text;
        }

        #endregion
        #region Helpers

        /// <summary>
        /// 2017-8-22
        /// Gets the "root" word, e.g., swimming -> swim
        /// </summary>
        private static Task GetLemmas(string word, out string lemma)
        {
            lemma = word;

            //  query string
            string fullurl = lemmaTemplate
                .Replace("{protocol}", protocol)
                .Replace("{endpoint}", endpoint)
                .Replace("{port}", port)
                .Replace("{version}", version)
                .Replace("{lang}", lang)
                .Replace("{word}", word.ToLower());

            JObject response;
            GetWebData(fullurl, out response);

            //  Alternate method
            Lemmatron lemmatron = JsonConvert.DeserializeObject<Lemmatron>(response.ToString());
            lemma = lemmatron.results[0].lexicalEntries[0].inflectionOf.inline.id;
            //  TODO -- return here if you use the above method

            //  get the lemma / root word
            string[] results = null;
            GetElements(response, "inflectionOf", "id", out results);
            if (results != null) lemma = results[0];

            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-22
        /// Makes the WebRequest and returns the result. This may return
        /// null if there was no result.
        /// </summary>
        private static Task GetWebData(string fullurl, out JObject result)
        {
            result = null;

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(fullurl);
            webRequest.ContentType = "application/json";
            webRequest.Headers.Add("app_id", decrypt(id, de));
            webRequest.Headers.Add("app_key", decrypt(key, de));

            try
            {
                //  Execute the request and put the result into response
                WebResponse webResp = webRequest.GetResponse();
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(webResp.GetResponseStream(), encoding))
                {
                    //  Convert the json string to a json object
                    result = (JObject)JsonConvert.DeserializeObject(reader.ReadToEnd());
                }
            }
            catch (WebException)
            {
                //  404	: No entry is found matching supplied id and source_lang or filters are not recognized
                //  500 : Internal Error. An error occurred while processing the data.
                //  Do nothing -- by default this returns the original word if
                //  no alternates are found.
                result = null;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 20-17-8-22
        /// This is used to get
        ///     lemmas (objectName: "inflectionOf", keyName: "id") --> return string[]
        ///     synonyms (objectName: "synonyms", keyName: "id") --> return string[]
        ///     antonyms (objectName: "antonyms", keyName: "id") --> return string[]
        ///     definitions (objectName: "senses", keyName: "definitions") --> return string[]
        /// 
        /// </summary>
        /// <param name="json">The object returned from the GetWebData() request</param>
        /// <param name="objectName">The object of interest, e.g., "synonyms", "definition", etc</param>
        /// <param name="keyName">Object key that holds the data, e.g., "id" or "text"</param>
        /// <param name="result">The string result found</param>
        /// <returns></returns>
        private static Task GetElements(JObject json, string objectName, string keyName, out string[] results)
        {
            #region Sample Result -> Lemmas
            /*  
                "inflectionOf": [
                    {
                        "id": "swim",
                        "text": "swim"
                    }
                ]
            */
            #endregion
            #region Sample Result -> Synonym
            /*
                "synonyms": [
                    {
                        "id": "wunderkind",
                        "language": "en",
                        "text": "wunderkind"
                    }
                ]
             */
            #endregion
            #region Sample Result -> Definitions
            /*
                "senses": [
                    {
                        "definitions": [
                            "a playing card with a single spot on it, ranked as the highest card in its suit in most card games"
                        ]
                    }
                ]
             */
            #endregion

            //  Get all the objects with key of "objectName"
            JObject[] objectArray = null;
            GetObjectsByKey(json, objectName, out objectArray);

            //  For each of these objects, get all the objects with key of "keyName"
            JObject[] keyArray = null;
            List<JObject> keyList = new List<JObject>();
            foreach (JObject item in objectArray)
            {
                GetObjectsByKey(item, keyName, out keyArray);
                keyList.AddRange(keyArray.ToList());
            }

            //  For each of these objects, get the values of every object
            List<string> resultList = new List<string>();
            foreach (var item in keyArray)
            {
                resultList.AddUnique(item.Value<string>());
            }

            results = resultList.ToArray();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-22
        /// This creates an array "results" of json objects. The contents of the list
        /// are all objects in the source object "json" with a key that matches the
        /// parameter "key".
        /// </summary>
        /// <param name="json">the object to search</param>
        /// <param name="key">the key to find</param>
        /// <param name="results">an array of string values of all the keys found</param>
        /// <returns></returns>
        private static Task GetObjectsByKey(JObject json, string key, out JObject[] results)
        {
            List<JObject> resultList = new List<JObject>();

            foreach (var item in json)
            {
                string itemKey = item.Key;
                if (itemKey.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    //  Key DOES match

                    //  when the value is an array
                    if (item.Value.Type.Equals(JTokenType.Array))
                    {
                        foreach (var arrayitem in item.Value)
                        {
                            resultList.Add((JObject)arrayitem);
                        }
                        continue;
                    }

                    //  when the value is an object
                    resultList.Add((JObject)item.Value);

                } else
                {
                    //  Key does NOT match
                    JObject[] subResults;

                    //  when the value is an array
                    if (item.Value.Type.Equals(JTokenType.Array))
                    {
                        foreach (var arrayitem in item.Value)
                        {
                            GetObjectsByKey((JObject)arrayitem, key, out subResults);
                            resultList.AddRange(subResults.ToList());
                        }
                        continue;
                    }

                    //  when the value is an object
                    GetObjectsByKey((JObject)item.Value, key, out subResults);
                    resultList.AddRange(subResults.ToList());
                }
            }

            results = resultList.ToArray();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-22
        /// This creates an array "results" of strings. The contents of the array
        /// are all strings in the source object "json" with a key that matches the
        /// parameter "key".
        /// </summary>
        /// <param name="json">the object to search</param>
        /// <param name="key">the key to find</param>
        /// <param name="results">an array of string values of all the keys found</param>
        /// <returns></returns>
        private static Task GetStringsByKey(JObject json, string key, out string[] results)
        {
            List<string> resultList = new List<string>();

            foreach (var item in json)
            {
                string itemKey = item.Key;
                if (itemKey.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                {
                    //  Key DOES match

                    if (item.Value.Type.Equals(JTokenType.String))
                    {
                        resultList.Add((string)item.Value);
                        continue;
                    } else
                    {
                        string[] subResults;
                        GetStringsByKey((JObject)item.Value, key, out subResults);
                        resultList.AddRange(subResults.ToList());
                    }                    
                } else
                {
                    //  Key does NOT match
                
                    //  Descend when value is an object
                    if (!item.Value.Type.Equals(JTokenType.String))
                    {
                        string[] subResults;
                        GetStringsByKey((JObject)item.Value, key, out subResults);
                        resultList.AddRange(subResults.ToList());
                    }
                }
            }

            results = resultList.ToArray();
            return Task.CompletedTask;
        }

        #region deprecated

        /// <summary>
        /// 2017-8-22 DEPRECATED
        /// Original JSON search method
        /// 
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

        #endregion

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
