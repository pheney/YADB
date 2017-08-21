﻿using Discord;
using System;
using System.Collections.Generic;

namespace YADB.Common
{
    public static class Constants
    {
        public static Random rnd = new Random();
        public static Color SlateRed = new Color(218, 100, 100);
        public static Color SlateGreen = new Color(100, 218, 100);
        public static Color SlateBlue = new Color(100, 100, 218);
        public static Color SlateWhite = new Color(200, 200, 200);
        public static Color Magenta = new Color(200, 0, 200);
        public static Color SlateYellow = new Color(218, 218, 100);

        public static string[] Greetings = new string[] {
            "hi", "hello", "greetings", "good morning", "good afternoon",
            "hey", "yo", "oi", "salutations", "howdy", "welcome", "hi-ya"
        };

        public enum MessageSeverity { Info, Success, Warning, CriticalOrFailure }

        public static Color SeverityToColor(MessageSeverity severity)
        {
            Color response = Magenta;
            switch (severity)
            {
                case MessageSeverity.Info:
                    response = SlateBlue;
                    break;
                case MessageSeverity.Success:
                    response = SlateGreen;
                    break;
                case MessageSeverity.Warning:
                    response = SlateYellow;
                    break;
                case MessageSeverity.CriticalOrFailure:
                    response = SlateRed;
                    break;
                default:
                    break;
            }
            return response;
        }

        private static string GetLeetName(string name) {

            string result = name;            
            int substitutionCount = rnd.Next(name.Length) + 1;

            for (int i = 0; i< substitutionCount; i++)
            {
                switch (rnd.Next(6))
                {
                    case 0:
                        result = result.Replace("o", "0");
                        break;
                    case 1:
                        result = result.Replace("e", "3");
                        break;
                    case 2:
                        result = result.Replace("l", "1");
                        break;
                    case 3:
                        result = result.Replace("s", "5");
                        break;
                    case 4:
                        result = result.Replace("a", "4");
                        break;
                    case 5:
                        result = result.Replace("g", "6");
                        break;
                }
            }
            return result;
        }

        private static string[] FancyNames = new string[]
        {
            "{name}{j}{#}", "{j}{name}{j}{be}", "{#}{j}{name}", "{?}{?}{j}{#}",
            "{?}{#}"
        };

        // {s}
        private static string[] FancySymbols = new string[]
        {
            "x","X", "o", "O", ".", ":", "~", "<>","><","=", "-","_","/\\","\\/","|"
        };

        // {j}
        private static string[] FancyJoiner = new string[]
        {
            ".", "-", "=", "_", "*", "~", ":"
        };
        
        /// <summary>
        /// 2017-8-20
        /// Returns a random set of book-end symbols,
        /// e.g., << >>, xo ox, <~ ~>
        /// </summary>
        /// <returns></returns>
        private static string[] GetBookends()
        {
            string[] result = new string[] { "", "" };
            
            for (int i = rnd.Next(4) + 1; i > 0; i--)
            {
                string symbol = FancySymbols.Random();
                result[0] += symbol;
                result[1] = symbol + result[1];
            }

            return result;
        }

        // {name} username
        // {#} any number
        // {?} any letter
        // {be} bookend wrapper
        // {le} leet substitution
        public static string ToFancyName(string name)
        {
            string result = "";

            //  get data to build a new name
            string joiner = FancyJoiner.Random();
            string[] bookends = GetBookends();
            string template = FancyNames.Random();

            //  do a "leet" substitution of the original name
            if (rnd.Next(4) == 0) name = GetLeetName(name);
            
            //  replace the joiner symbols
            result = template.Replace("{j}", joiner);

            //  replace number symbols
            result = result.Replace("{#}", rnd.Next(1000).ToString());

            //  replace random character symbols
            int num = rnd.Next(0, 26);
            char letter = (char)('a' + num);
            result = result.Replace("{?}", letter.ToString());

            //  add bookends
            bool useBookend = result.IndexOf("{be}") > -1;
            if (useBookend)
            {
                //  remove bookend symbol
                result = result.Replace("{be}", "");

                //  add bookends
                result = bookends[0] + result + bookends[1];
            }

            //  replace the name
            result = result.Replace("{name}", name);

            return result;
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// 2017-8-18
        /// </summary>
        public static float Range(this Random source, float min, float max)
        {
            return min + (float)source.NextDouble() * (max - min);
        }

        /// <summary>
        /// 2017-8-18
        /// Returns a random object from an array of objects.
        /// </summary>
        public static T Random<T>(this T[] source)
        {
            return source[Constants.rnd.Next(source.Length)];
        }

        /// <summary>
        /// 2017-8-18
        /// Returns a random object from a list of objects.
        /// </summary>
        public static T Random<T>(this List<T> source)
        {
            return source[Constants.rnd.Next(source.Count)];
        }

        /// <summary>
        /// 2017-8-20
        /// Takes a string array and returns a single string containing all 
        /// the elements.
        /// 
        /// For example, given: string[] words = new string[] { "This" "was" "fun" }
        /// Use words.JoinWith(" ") --> "This was fun"
        /// use words.JoinWith(" ", 1) --> "was fun"
        /// Use words.JoinWith(1) --> "wasfun"
        /// use words.JoinWith(" ", endIndex:1) --> "This was"
        /// </summary>
        /// <param name="source">an array of strings</param>
        /// <param name="conjunction">placed between each string</param>
        /// <returns>The array concatenated as a single string</returns>
        public static string JoinWith(this string[] source, string conjunction = null, int? startIndex = null, int? endIndex = null)
        {
            conjunction = conjunction ?? "";
            startIndex = startIndex ?? 0;
            endIndex = endIndex == null ? source.Length : Math.Min((int)endIndex + 1, source.Length);

            string result = "";
            for (int i = (int)startIndex; i < endIndex; i++)
            {
                result += source[i];
                if (i < endIndex - 1) result += conjunction;
            }
            return result;
        }
    }
}
