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

        public static Color SlateYellow = new Color(218, 218, 100);

        public static string[] Greetings = new string[] {
            "hi", "hello", "greetings", "good morning", "good afternoon",
            "hey", "yo", "oi", "salutations", "howdy", "welcome", "hi-ya"
        };
    }

    public static class Extensions
    {
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
    }
}
