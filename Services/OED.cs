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
        #region Homonym API

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

        #endregion
        #region Homonym Helpers and Data Structure

        /// <summary>
        /// List of the most common offenders.
        /// Ref: http://grammarist.com/homophones/
        /// </summary>
        private static DefinedWord[] h = new DefinedWord[]
        {
            new DefinedWord("their", "belonging to them", true),
            new DefinedWord("they're", "they are"),
            new DefinedWord("there", "a location"),
            new DefinedWord("to", "towards", true),
            new DefinedWord("too", "also"),
            new DefinedWord("two", "the number between 1 and 3"),
            new DefinedWord("mold", "fungas that grows in moist damp places, or the act of shaping something", true),
            new DefinedWord("mould", "container used to shape molten liquid like metal or Jello"),
            new DefinedWord("weather", "sun, rain, clouds", true),
            new DefinedWord("whether", "a choice"),
            new DefinedWord("carrot", "vegetable", true),
            new DefinedWord("caret", "a proofreading symbol ^"),
            new DefinedWord("karat", "measure of purity of gold"),
            new DefinedWord("carat", "unit of weight for diamonds"),
            new DefinedWord("affect", "to change or to pretend",true),
            new DefinedWord("effect", "a result"),
            new DefinedWord("laser", "a beam of coherent light", true),
            new DefinedWord("lazer", "a gross mispelling of 'laser'"),
            new DefinedWord("roll", "to turn over, (roll a die), a sequence (on a roll), a list (roll call)", true),
            new DefinedWord("role", "a position or function"),
            new DefinedWord("Bazaar", "place to shop", true),
            new DefinedWord("bizarre", "weird"),
            new DefinedWord("your", "belonging to you", true),
            new DefinedWord("you're", "you are"),
            new DefinedWord("turret", "big thing that rotates a gun, or a tall round tower on a castle wall", true),
            new DefinedWord("turrent", "shameful mispelling of 'turret'"),
            new DefinedWord("Tourette", "neuropsychiatric disorder, or a weak escuse by the uneducated to use excessive profanity"),
            new DefinedWord("here", "a location", true),
            new DefinedWord("hear", "to listen"),
            new DefinedWord("farther", "comparison of physical distance", true),
            new DefinedWord("further", "copmarison of figurative distance"),
            new DefinedWord("bored", "nothing to do, or to have punctured with a hole", true),
            new DefinedWord("board", "a piece of wood"),
            new DefinedWord("canon", "general principle or criteria by which something is judged", true),
            new DefinedWord("cannon", "a big gun"),
            new DefinedWord("buy", "to purchase", true),
            new DefinedWord("by", "atributed to someone"),
            new DefinedWord("where", "a location", true),
            new DefinedWord("wear", "to put on a piece of clothing"),
            new DefinedWord("lie", "you lie down", true),
            new DefinedWord("lay", "you lay _other things_ down"),
            new DefinedWord("than", "compare things, \"10 is more _than_ 8\"", true),
            new DefinedWord("then", "temporal sequence, \"get money, _then_ buy things\""),
            new DefinedWord("patients", "people in a hospital", true),
            new DefinedWord("patience", "toleration for stupidity"),
            new DefinedWord("through", "to penetrate", true),
            new DefinedWord("thru", "horrible mispelling of 'through'"),
            new DefinedWord("threw", "to forcefully project"),
            new DefinedWord("any time", "_always use this; it's never wrong_", true),
            new DefinedWord("anytime", "adverb meaning \"whenever\"; never use with the word \"at\"")
        };

        /// <summary>
        /// 2017-8-22
        /// Returns all words that sound alike, but are spelt different.
        /// </summary>
        private static DefinedWord[] GetAllHomophones(string word)
        {
            DefinedWord found = h.Where(x => x.Equals(word)).FirstOrDefault();
            if (found == null) return null;
            DefinedWord[] result = h.Where(x => x.homophoneId == found.homophoneId).ToArray();
            return result;
        }

        private static EmbedBuilder GetBuilderFor(DefinedWord[] homophones)
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

        private class DefinedWord
        {
            private static int _homophoneId = 0;
            public int homophoneId;
            public string word, definition;

            public DefinedWord(string word, string definition, bool increment = false)
            {
                if (increment) _homophoneId++;
                this.homophoneId = _homophoneId;
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
        #region Dictionary API

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
                .Replace("{word}", lemma.ToLower());
            
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
            definitions = ParseDefinitions(webResponse);
            
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
                .Replace("{word}", lemma.ToLower());

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
            synonyms = ParseThesaurus(webResponse, "synonyms");

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
                .Replace("{word}", lemma.ToLower());

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
            antonyms = ParseThesaurus(webResponse, "antonyms");            
                        
            //  dig out the antonyms from the web response
            return Task.CompletedTask;
        }

        #endregion
        #region Dictionary Helpers

        /// <summary>
        /// 2017-8-22 -- Validated
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
            fullurl += "/grammaticalFeatures=present";

            JObject response;
            GetWebData(fullurl, out response);

            if (response !=null) lemma = ParseLemmas(response);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-22 -- Validated
        /// Makes the WebRequest and returns the result. This may return
        /// null if there was no result.
        /// </summary>
        private static Task GetWebData(string fullurl, out JObject result)
        {
            result = null;

            //  Build the web request
            WebRequest webRequest = WebRequest.Create(fullurl);
            webRequest.ContentType = "application/json";
            webRequest.Headers.Add("app_id", Decrypt(id, de));
            webRequest.Headers.Add("app_key", Decrypt(key, de));

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

        #region deprecated

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
            //  These may be objects or arrays of objects
            JObject[] objectArray = null;
            GetObjectsByKey(json, objectName, out objectArray);

            //  For each of these objects, get all the objects with key of "keyName"
            //  These may be objects or arrays of objects
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

        #endregion deprecated
        #region Specialized JSON parsers

        /// <summary>
        /// 2017-8-23
        /// </summary>
        /// <param name="json">The entire response object from the Lemmatron API call</param>
        /// <returns>the uninflected form of the word, e.g., swimming -> swim</returns>
        private static string ParseLemmas(JObject json)
        {
            JObject results = json["results"][0] as JObject;
            JObject lexicalEntry = results["lexicalEntries"][0] as JObject;
            JObject inflectionOf = lexicalEntry["inflectionOf"][0] as JObject;
            return (string)inflectionOf["text"];
        }

        /// <summary>
        /// 2017-8-23
        /// </summary>
        /// <param name="json">The entire response object from the Lemmatron API call</param>
        /// <param name="types">"synonyms" or "antonyms"</param>
        /// <returns>array of synonyms or antonyms</returns>
        private static string[] ParseThesaurus(JObject json, string types)
        {
            List<string> resultList = new List<string>();

            var results = json["results"][0];
            var lexicalEntry = results["lexicalEntries"][0];
            var entries = lexicalEntry["entries"][0];
            var senses = entries["senses"][0];

            var subsenses = senses["subsenses"];
            if (subsenses != null) foreach (var item in subsenses)
                {
                    var typeArray = item[types];
                    if (typeArray != null) foreach (var typeItem in typeArray)
                        {
                            resultList.Add((string)typeItem["text"]);
                        }
                }
            return resultList.ToArray();
        }

        /// <summary>
        /// 2017-8-23
        /// </summary>
        /// <param name="json">The entire response object from the Lemmatron API call</param>
        /// <returns>an array of definitions</returns>
        private static string[] ParseDefinitions(JObject json)
        {
            List<string> resultList = new List<string>();

            var results = json["results"][0];
            var lexicalEntry = results["lexicalEntries"][0];

            //  iterate each entry of the array
            var entries = lexicalEntry["entries"];
            if (entries != null) foreach (var entry in entries)
                {
                    //  iterate the "senses" of the entry
                    var senses = entry["senses"];
                    if (senses != null) foreach (var sense in senses)
                        {
                            //  each "sense" has an array of definitions
                            var senseDefinitions = sense["definitions"];
                            if (senseDefinitions != null) foreach (var senseDef in senseDefinitions)
                                {
                                    resultList.Add((string)senseDef);
                                }

                            //  each "sense" has an array of subsenses
                            var subsenses = sense["subsenses"];
                            if (subsenses != null) foreach (var subsense in subsenses)
                                {
                                    //  each subsense has an array of definitions
                                    var defArray = subsense["definitions"];
                                    if (defArray != null) foreach (var defItem in defArray)
                                        {
                                            resultList.Add((string)defItem);
                                        }
                                }

                        }

                }

            return resultList.ToArray();
        }

        #endregion
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
        private static string Decrypt(string source, string key)
        {
            return source.Substring(int.Parse(key));
        }

        #endregion
    }
}
