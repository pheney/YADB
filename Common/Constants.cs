using Discord;
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
            endIndex = endIndex ?? source.Length;

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
