using System;
using System.Collections.Generic;
using System.Linq;
using YADB.Common;

namespace YADB.Services
{
    public static class Insult
    {
        #region Public API

        public static string GetInsult(int vulgarity = 2)
        {
            string first = SelectFirstWord(vulgarity);
            string second = SelectSecondWord(vulgarity, first);
            string third = SelectThirdWord(vulgarity, second);
            return first + " " + second + " " + third;
        }

        private static string vowels = "aeiou";

        public static string GetIndefiniteArticle(string word)
        {
            char firstLetter = word[0];
            if (vowels.Contains(firstLetter)) return "an";
            else return "a";
        }

        public static string GetDefiniteArticle(string word)
        {
            return "the";
        }

        public static string Capitalize(this string source)
        {
            return source.Substring(0, 1).ToUpper() + source.Substring(1).ToLower();
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
        private static string[] vulgarityOptions = new string[] {
            "Child", "American", "British", "Australian" };

        private static string[] ColumnAbasic = new string[] {
          "fat", "useless", "sloppy", "soft", "flabby", "stale"
          , "disgusting", "slimy", "gross", "lazy"
          , "stupid", "idiotic", "ignorant", "brainless", "mindless"
          , "dense", "smelly", "rotten", "damp", "mildewy", "moldy"
          , "stuck up", "bland", "boring", "empty", "ugly", "pie-eating"
          , "creepy", "greasy", "filthy", "dirty", "rude", "whiny"
          , "spoiled", "basic", "drooling"
          , "grouchy"
          , "crabby"
          , "childis"
        };

        private static string[] ColumnA = new string[] {
          "pathetic", "piteous", "wreched", "miserable", "lamentable", "appalling", "deplorable"
          , "engorged", "turgid"
          , "enervated", "impotent", "frigid", "tepid"
          , "uninspired", "tame", "flaccid", "lifeless", "pointless", "lackluster"
          , "slacking", "drooping", "torpid"
          , "putrid"
          , "shiftless", "lethargic", "sluggish", "lifeless", "slothful"
          , "incompetent", "myopic", "moronic", "tone deaf"
          , "foolish", "witless"
          , "dull-witted", "feebleminded", "obtuse", "tedious"
          , "insecure", "loose"
          , "slutty", "trashy", "bawdy", "tawdry", "sordid", "goatish", "cheap", "disgraceful"
          , "malodorous", "fetid", "foul-smelling", "effluvient"
          , "toxic", "hostile", "antisocial", "cancerous", "caustic"
          , "pompous"
          , "insipid", "unimaginative", "uninspired", "blathering"
          , "uninteresting", "lackluster", "oblivious", "vapid"
          , "frothy", "shallow", "superficial", "insubstantial", "dim-witted"
          , "racist", "fascist", "elitist"
          , "vacuous", "mindless", "deluded"
          , "white trash", "drug-using"
          , "butterface", "wilted", "dick-nosed"
          , "creepy", "greasy", "filthy", "dirty"
          , "guzzling"
          , "churlish", "boorish"
          , "cockered", "pampered"
          , "currish", "dishonorable", "boorish", "ignoble", "contemptible"
          , "vile", "despicable", "degenerate"
          , "bombastic", "self-aggrandizing"
          , "fawning", "obsequious", "ass-kissing", "sychophantic"
          , "droning", "prattling"
          , "fobbing", "pretentious", "foppish", "hipster"
          , "froward", "contrary"
          , "slack-jawed", "crusty"
          , "plebeian", "common", "used", "mediocre", "pedestrian"
          , "irritable"
          , "infected", "diseased"
          , "short-tempered"
          , "fractious", "irritating", "quarrelsome", "pettish", "prickly"
          , "waspish", "peppery", "peevish", "petulant", "testy", "tetchy"
          , "bad-tempered", "nettlesome", "ill-natured"
          , "uptight", "solicitous", "mouthy"
          , "hateful", "vitriolic", "bitter", "horrendous"
        };
        private static string[] ColumnB = new string[] {
          "butt", "rump", "bum"
          , "poo"
          , "pee"
          , "ball", "sack", "egg"
          , "melon" };

        private static string[] ColumnBvulgar = new string[] {
          "douche"
          , "ass", "rectum", "sphincter", "anal"
          , "cock", "prick", "dick", "boner", "penis", "dong", "shaft", "sausage", "meat"
          , "turd", "shit", "shart", "stool", "crap"
          , "crotch", "taint"
          , "slut", "whore", "bint", "tart", "ho", "trollop"
          , "fuck"
          , "piss", "urine"
          , "vagina", "twat", "pussy", "cunt", "queef", "clam", "gash", "taco", "pocket", "clit"
          , "testicle", "nut", "scrotum"
          , "tit", "boob", "titty"
          , "bitch", "bastard"
          , "mouth", "oral"
          , "vomit", "bile", "filth"
        };
        private static string[] ColumnC = new string[] {
          "canoe", "balloon"
          , "clown", "chef", "dancer", "juggler"
          , "waffle", "biscuit", "salad"
          , "blossom", "wallet"
          , "goblin", "monster", "dragon", "troll", "hobgoblin", "beast", "monkey"
          , "weasel", "hound", "pony", "frog", "hawk", "bird", "pig", "swine"
          , "cow", "sloth", "rat"
          , "smear", "mess", "jumble"
          , "handler", "wiper"
        };
        private static string[] ColumnCvulgar = new string[] {
          "pilot", "captain", "pirate", "jockey", "raider", "diver", "loader"
          , "knob", "box", "socket", "hole", "dumpster", "bag", "sack"
          , "nazi", "tramp"
          , "needle"
          , "parasite"
          , "stain", "cluster"
          , "stick", "tool", "drill", "hammer"
          , "grinder", "chugger", "licker", "molester", "tosser", "biter"
          , "fondler", "slapper", "wanker", "grabber", "polisher", "holder"
          , "swallower", "gobbler", "guzzler", "sniffer", "hopper", "wipe"
          , "fucker", "choker", "taster"
        };
        private static string[] Australian = new string[] {
          "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt"
          , "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt", "cunt"
        };
        private static string[] Teen = new string[] {
          "shit", "shit", "shit", "shit", "shit", "shit", "shit"
          , "fuck", "fuck", "fuck", "fuck", "fuck", "fuck", "fuck", "fuck", "fuck"
          , "mother", "mother", "mother"
        };
        private static string[] Teening = new string[] {
          "fucking", "fucking", "fucking", "fucking", "fucking", "fucking", "fucking", "fucking"
          , "fucking", "fucking", "fucking", "fucking", "fucking", "fucking", "fucking", "fucking"
        };


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
                    result = Select(ColumnAbasic, "");
                    break;
                case 1:
                    //  Angsty Teen (basic vocabulary, Teen-ing)
                    result = Select(Combine(ColumnAbasic, Teening), "");
                    break;
                default:
                    //  British, Australian (full vocabulary)
                    result = Select(Combine(ColumnAbasic, ColumnA), "");
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
                    result = Select(ColumnB, firstWord);
                    break;
                case 1:
                    //  Angsty Teen (yes profanity, Teen language)
                    result = Select(Combine(ColumnB, ColumnBvulgar, Teen), firstWord);
                    break;
                case 2:
                    //  British (yes profanity)
                    result = Select(Combine(ColumnB, ColumnBvulgar), firstWord);
                    break;
                default:
                    //  Australian (yes profanity, Australian language)
                    result = Select(Combine(ColumnBvulgar, Australian), firstWord);
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
                    result = Select(ColumnC, secondWord);
                    break;
                case 1:

                    //  Angsty Teen (use profanity, Teen language)
                    result = Select(Combine(ColumnC, ColumnCvulgar, Teen), secondWord);
                    break;
                case 2:
                    //  British (use profanity)
                    result = Select(Combine(ColumnC, ColumnCvulgar), secondWord);
                    break;
                default:
                    //  Australian (use profanity, Australian language)
                    result = Select(Combine(ColumnC, ColumnCvulgar, Australian), secondWord);
                    break;
            }
            return result;
        }
    }
}
