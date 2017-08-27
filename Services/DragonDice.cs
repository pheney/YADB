using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Services
{
    /// <summary>
    /// 2017-8-125
    /// 
    /// Warrior - the player's character. This goes up 1 level after the character
    /// defeats a certain amount of dragons (determined by a formula). Formula TBD.
    /// 
    /// Dragon - the warrior's opponent. These have different "difficulty" values.
    /// 
    /// Pack - A group of dragons. Initially, there are 3 dragons, a weak, medium and strong.
    ///     As the warrior advances in level, the dragons become stronger and more numerous.
    ///     
    /// Battle - the conflict between a warrior and a single dragon. Possible results: win (when the
    ///     warrior kills the dragon), Lose (when warrior dies), Continue (when both dragon
    ///     and warrior are alive). 
    ///     
    ///     Battle continues until either the warrior or dragon dies. After each Battle, the 
    ///     warrior has the option of starting a Battle with any remaining dragon from the Pack,
    ///     or the warrior may end the Quest. The warrior does not recover damage between Battles.
    ///     
    /// Quest - is the process of Battling an entire dragon Pack. The warrior faces the dragons
    ///     one at a time, in the order of his choosing. The warrior may end the Quest after 
    ///     any Battle completes.    
    ///     
    /// Score - Warrior earns XP for the following:
    ///     Incomplete Quest: half XP awarded
    ///     Complete Quest: full XP awarded
    ///     Each dragon defeated: base XP = dragon difficulty + dragon level
    ///     Each dragon defeated doubles the XP awarded for previous dragons defeated 
    ///         in the same Quest.
    ///     Warrior is killed: no XP
    /// </summary>
    public static class DragonDice
    {
        [Serializable]
        private class DragonDiceData
        {
            [JsonIgnore]
            public static DragonDiceData Get;

            #region Data

            public Dictionary<ulong, Warrior> playerWarriors;

            #endregion

            public static void Init()
            {
                string FileName = new DragonDiceData().GetFilename();
                string file = FileOperations.PathToFile(FileName);

                // When the file does NOT exists, create it
                if (!FileOperations.Exists(FileName))
                {
                    //  Create a new configuration object
                    var data = new DragonDiceData();
                    data.playerWarriors = new Dictionary<ulong, Warrior>();

                    //  Save the configuration object
                    FileOperations.SaveAsJson(data);
                }

                DragonDiceData.Get = FileOperations.Load<DragonDiceData>();
                DragonDice.playerWarriors = DragonDiceData.Get.playerWarriors;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(FileName + " loaded");
                Console.ResetColor();
            }
        }

        #region Messages and Constants

        private static int delayMillis = 300;

        private static string callToAction = "Make your selection!";
        private static string[] trainingChoices = new string[]
        {
            "Offense fighter: maximum offense improvement, no protection",
            "Mostly offense",
            "Balanced fighter: equal offensive and protection improvement",
            "Mostly defense",
            "Defensive fighter: no offense improvement, maximum protection"
        };

        #endregion
        #region Manage Game-State on a per-user basis

        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Generate a dragon Pack, based on player's Warrior.
        /// Present the dragons and ask the player to select one to battle.
        /// </summary>
        public static async Task StartQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            Warrior warrior;

            await GetOrCreateQuest(playerId, out quest);
            await GetOrCreateWarrior(playerId, out warrior);
            warrior.Ready();

            await Send(context, "Your warrior is ready!");
            await ParseInfoAction(context, "w");

            await ContinueOrAbandonQuest(context);
        }

        /// <summary>
        /// 2017-8-25
        /// Preset players wth remaining dragons and ask which the player
        /// wants to battle, OR if he wants to Abandon the Quest.
        /// </summary>
        /// <param name="playerId"></param>
        private static async Task ContinueOrAbandonQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            int choices = quest.GetDragonChoices.Length;
            string questStatus = "Your warrior must slay " + choices + " dragons to complete this quest.";
            string instructions = "Select which dragon to battle, or you can abort the quest.";
            await Send(context, questStatus + " " + instructions);

            await ParseInfoAction(context, "r");
            await Task.Delay(delayMillis);

            string options = "";
            for (int i = 0; i < choices; i++)
            {
                int choiceId = i + 1;
                Dragon d = quest.GetDragonChoices[i];
                options += choiceId + " : the " + d.Info() + "\n";
            }
            options += "\nA : Abandon quest";

            string message = options + "\n\n" + callToAction;
            await Send(context, message);
        }

        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Player selects a dragon from the Pack to battle.
        /// This progressively shows the battle results for each "turn"
        /// until either the Dragon or Warrior is killed.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="dragonSelection"></param>
        private static async Task BattleDragon(ICommandContext context, int dragonSelection)
        {
            ulong playerId = context.User.Id;

            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            quest.SelectDragonIndex(dragonSelection - 1);
            Dragon dragon = quest.CurrentDragon;

            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            //  Execute a round of battle

            await Send(context, "_Searching for the "+quest.CurrentDragon.Info()+"_\n\n");

            int[] damage = Clash(warrior, dragon);
            int damageToWarrior = damage[0];
            int damageToDragon = damage[1];
            int fire = quest.CurrentDragon.Fires + warrior.Fires;

            //  Show die results
            await Task.Delay(delayMillis);
            await Send(context, "Warrior rolls: " + warrior.DiceRollsToString() +
                "\nDragon rolls: " + dragon.DiceRollsToString());
            await Task.Delay(delayMillis);

            //  Narrate outcome
            string narration;
            if (damageToDragon > 0 || fire > 0)
            {
                narration = "\n_The warrior finds the dragon and attacks!_\n\n";
                if (damageToDragon > 0)
                {
                    narration += "Warrior hits for " + damageToDragon + "! ";
                }
                if (fire > 0)
                {
                    narration += "The dragon breathes fire for " + fire + ". ";
                    if (warrior.Shields > 0)
                    {
                        narration += "But the warrior's shield blocks " + Math.Min(fire, warrior.Shields) + ". ";
                    }
                    narration += "The warrior is injured for " + damageToWarrior + ".";
                }
            }
            else
            {
                narration = "\n_Can't find dragon..._";
            }
            await Send(context, narration);

            //  Apply the results
            warrior.Damage(damageToWarrior);
            dragon.Damage(damageToDragon);

            //  Show results of the round to the player
            await Task.Delay(delayMillis);
            string status = "\nWarrior has " + warrior.Health + " health, and dragon has " + dragon.Health + " health.";
            await Send(context, status);

            await Task.Delay(delayMillis);

            //  When the player wins
            if (GetResult(warrior, dragon).Equals(BattleOutcome.Win))
            {
                await Send(context, "Warrior defeated the " + quest.CurrentDragon.Info() + "!");
                quest.DragonDefeated();
                await Task.Delay(delayMillis);

                if (quest.IsComplete)
                {
                    await WinQuest(context);
                    return;
                }
                else
                {
                    //  Player must decide to continue or end quest
                    await ContinueOrAbandonQuest(context);
                    return;
                }
            }

            //  When the player loses
            if (GetResult(warrior, dragon).Equals(BattleOutcome.Lose))
            {
                await LoseQuest(context);
                return;
            }

            await HelpHunting(context);
        }

        private static async Task WinQuest(ICommandContext context)
        {
            await Send(context, "Congratulations! You completed your quest!");
            ulong playerId = context.User.Id;

            Quest quest;
            await GetOrCreateQuest(playerId, out quest);

            await Task.Delay(delayMillis);

            //  Award Full Quest XP
            int xp = quest.XP;
            await Send(context, "Your warrior earned " + xp + " experience!"); ;

            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);
            warrior.AwardXP(xp);

            await Task.Delay(delayMillis);
            await EndQuest(context);
        }

        private static async Task WarriorLevelUp(ICommandContext context)
        {
            string levelUp = "Your warrior has gained a level!";
            string trainingPriority = "Select the type of training your warrior will learn:";
            string options = "";
            for (int i = 0; i < trainingChoices.Length; i++)
            {
                options += (i + 1) + ": " + trainingChoices[i] + "\n";
            }

            await Send(context, levelUp + " " + trainingPriority + "\n" + options + "\n" + callToAction);
        }

        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Player has elected to end the Quest.
        /// This also occurs when the player disconnects during a battle.
        /// </summary>
        /// <param name="playerId"></param>
        private static async Task AbandonQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);

            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            int xp = quest.XP / 2;
            await Send(context, "Your warrior earned " + xp + " experience."); ;
            warrior.AwardXP(xp);

            await Task.Delay(delayMillis);
            await EndQuest(context);
        }

        private static async Task LoseQuest(ICommandContext context)
        {
            await Send(context, "Your warrior was killed.");
            await Task.Delay(delayMillis);
            await EndQuest(context);
        }

        private static async Task EndQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);
            await DeleteQuest(playerId);

            playerQuests.Remove(playerId);
            if (warrior.ReadyToLevelUp())
            {
                await WarriorLevelUp(context);
                return;
            }

            if (DragonDiceData.Get.playerWarriors.ContainsKey(playerId))
            {
                DragonDiceData.Get.playerWarriors.Remove(playerId);
            }
            DragonDiceData.Get.playerWarriors.Add(playerId, warrior);
            await FileOperations.SaveAsJson(DragonDiceData.Get);
            await Send(context, "Your quest has ended.");
        }

        /// <summary>
        /// 2017-8-25
        /// Indicates if the player ID is actively playing
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static bool IsPlaying(ulong playerId)
        {
            return playerQuests != null && playerQuests.ContainsKey(playerId);
        }

        #endregion
        #region Input Handlers

        /// <summary>
        /// 2017-8-26
        /// Primary input handler. This handles all input for the game and then sends
        /// it to the specific parts as required.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="input">anything the user types</param>
        /// <returns></returns>
        public static async Task ParseUserInput(ICommandContext context, string input)
        {
            //  Determine which input handler to use

            //  When in a quest
            if (playerQuests.ContainsKey(context.User.Id))
            {
                if (playerQuests[context.User.Id].IsHunting)
                {
                    //  currently hunting
                    await ParseHuntAction(context, input);
                }
                else
                {
                    //  before or after a hunt 
                    await ParseQuestAction(context, input);
                }
                return;
            }
            else
            {
                //  Quest just ended
                await ParseLevelUpAction(context, input);
                await EndQuest(context);
                return;
            }
        }

        private static string[][] infoCommands = new string[][] {
            new string[] { "i", "Show info"},
            new string[] { "w", "Show _warrior_ status"},
            new string[] { "d", "Show _dragon_ status"},
            new string[] { "r", "Show _remaing_ dragons on the quest"}
        };

        private static async Task ParseInfoAction(ICommandContext context, string choice)
        {
            string commands = "";
            foreach (var h in infoCommands) commands += h[0];
            if (commands.Contains(choice))
            {
                Warrior warrior;
                Quest quest;

                switch (choice)
                {
                    case "i":
                        await HelpInfo(context);
                        break;
                    case "w":
                        await GetOrCreateWarrior(context.User.Id, out warrior);
                        await Send(context, "Warrior is level " + warrior.Level + ", and has " + warrior.Health + " health, and " + warrior.Experience + " XP.");
                        break;
                    case "d":
                        await GetOrCreateQuest(context.User.Id, out quest);
                        await Send(context, "Currently fighting a " + quest.CurrentDragon.Info());
                        break;
                    case "r":
                        await GetOrCreateQuest(context.User.Id, out quest);
                        await Send(context, "There are " + quest.GetDragonChoices.Length + " dragons remaining on this quest.");
                        break;
                }
            }
        }

        /// <summary>
        /// 2017-8-25
        /// Handles user input during a quest, typically to prompts from
        /// ContinueOrAbandonQuest()
        /// </summary>
        /// <param name="context"></param>
        /// <param name="choice"></param>
        /// <returns></returns>
        private static async Task ParseQuestAction(ICommandContext context, string choice)
        {
            Quest quest;
            await GetOrCreateQuest(context.User.Id, out quest);

            if (quest.IsHunting)
            {
                await ParseHuntAction(context, choice);
                return;
            }
            else
            {
                //  When NOT hunting, i.e., at the start of the quest,
                //  and after a dragon is killed, the only options
                //  are "abort" and the number of the dragon to hunt.

                if (choice.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    await AbandonQuest(context);
                    return;
                }

                int selection;
                if (int.TryParse(choice, out selection))
                {
                    if (selection > 0 && selection <= quest.GetDragonChoices.Length)
                    {
                        await BattleDragon(context, selection);
                        return;
                    }
                }

                await HelpQuesting(context);
            }
        }

        private static string[][] huntCommands = new string[][]
        {
            new string[] {"s", "Search for the dragon"},
            new string[] {"a","Abandon the quest"}
        };

        private static async Task ParseHuntAction(ICommandContext context, string choice)
        {
            string commands = "";
            foreach (var h in huntCommands) commands += h[0];
            if (commands.Contains(choice))
            {
                if (choice.Equals(huntCommands[0][0], StringComparison.OrdinalIgnoreCase))
                {
                    await BattleDragon(context, 1);
                    return;
                }

                if (choice.Equals(huntCommands[1][0], StringComparison.OrdinalIgnoreCase))
                {
                    await AbandonQuest(context);
                    return;
                }
            }
            else
            {
                await HelpHunting(context);
            }
        }

        /// <summary>
        /// 2017-8-25
        /// Handles user input after a quest, typically to prompts from 
        /// LevelUp() which occurs after the quest awards XP and is destroyed.
        /// 
        /// Player's warrior has advanced a level, and the player was prompted
        /// to level up the character. The player has selected how to "train"
        /// his character on a scale of 0-4, where 0 means train maximum defense,
        /// 4 means train maximum offense, and 2 means train equal defense and offense.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="warriorType"></param>
        private static async Task ParseLevelUpAction(ICommandContext context, string choice)
        {
            int trainingIndex = 3;
            if (int.TryParse(choice, out trainingIndex))
            {
                //  player is display choices from 1 - 5,
                //  convert this to 0-index selection
                trainingIndex--;

                Warrior warrior;
                await GetOrCreateWarrior(context.User.Id, out warrior);
                warrior.LevelUp(trainingIndex);
                await FileOperations.SaveAsJson(DragonDiceData.Get);
                return;
            }
            else
            {
                await HelpLevelUp(context);
            }
        }

        private static async Task HelpInfo(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            string message = "You are in a quest. You may ask for the following "
                + "information:\n\n";
            foreach (var c in huntCommands) message += c[0] + " : " + c[1] + "\n";
            await Send(context, message);
        }

        private static async Task HelpHunting(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            string message = "You are in the middle of a hunt. You may take the following "
                + "actions:\n\n";
            foreach (var c in huntCommands) message += c[0] + " : " + c[1] + "\n";
            await Send(context, message);
        }

        private static async Task HelpQuesting(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            string message = "You must choose which dragon to hunt by selecting "
                + "the number next to its listing ({numbers}). You may also choose "
                + "\"a\" to _abandon_ the quest.";

            Quest quest;
            await GetOrCreateQuest(context.User.Id, out quest);
            int dragons = quest.GetDragonChoices.Length;
            string options = "";
            for (int i = 0; i < dragons; i++) options += "_" + (i + 1).ToString() + "_, ";
            options = options.Substring(0, options.Length - 2);

            await Send(context, message.Replace("{numbers}", options));
        }

        private static async Task HelpLevelUp(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            await Send(context, "You must choose a number from 1 through 5.");
        }

        #endregion
        #region Message Helpers

        private static async Task Send(ICommandContext context, string message)
        {
            await context.Channel.SendMessageAsync(message);
        }

        #endregion
        #region Helpers

        private static Dictionary<ulong, Quest> playerQuests;

        private static Task DeleteQuest(ulong playerId)
        {
            if (playerQuests.ContainsKey(playerId))
            {
                playerQuests.Remove(playerId);
            }
            return Task.CompletedTask;
        }

        private static Task GetOrCreateQuest(ulong playerId, out Quest quest)
        {
            if (playerQuests == null) playerQuests = new Dictionary<ulong, Quest>();
            if (!playerQuests.ContainsKey(playerId))
            {
                Warrior warrior = null;
                GetOrCreateWarrior(playerId, out warrior);
                Quest q = new Quest(playerId, warrior.Level);
                playerQuests.Add(playerId, q);
            }
            quest = playerQuests[playerId];
            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-25
        /// Key: player Id
        /// Value: Warrior object
        /// </summary>
        private static Dictionary<ulong, Warrior> playerWarriors;

        /// <summary>
        /// 2017-8-25
        /// Gets the player's warrior from the dictionary.
        /// If the player does not have a warrior created, this
        /// creates a new one, inserts it in the dictionary, and returns it;
        /// </summary>
        private static Task GetOrCreateWarrior(ulong playerId, out Warrior warrior)
        {
            if (playerWarriors == null)
            {
                DragonDiceData.Init();
                playerWarriors = DragonDiceData.Get.playerWarriors;
            }

            if (playerWarriors == null) playerWarriors = new Dictionary<ulong, Warrior>();

            if (!playerWarriors.ContainsKey(playerId))
            {
                playerWarriors.Add(playerId, new Warrior(playerId));
            }

            warrior = playerWarriors[playerId];
            return Task.CompletedTask;
        }

        /// <summary>
        /// 2017-8-25
        /// Conducts a round of battle. This modifies both the Warrior and Dragon objects.
        /// </summary>
        private static int[] Clash(Warrior warrior, Dragon dragon)
        {
            //  Randomize both characters
            warrior.Roll();
            dragon.Roll();

            //  Temporary storage for the damage each does
            int damageToDragon = 0, damageToWarrior = 0;

            //  Warrior keeps any parts as long as a weapon is exposed
            if (warrior.Swords > 0 && dragon.DragonPart > 0)
            {
                //  damage the dragon receives
                damageToDragon = dragon.DragonPart;
            }

            //  Dragon breathes fire on warrior
            if (warrior.Fires > 0 || dragon.Fires > 0)
            {
                //  damage the warrior receives
                damageToWarrior = warrior.Fires + dragon.Fires - warrior.Shields;
                damageToWarrior = Math.Max(0, damageToWarrior);
            }

            return new int[] { damageToWarrior, damageToDragon };
        }

        private enum BattleOutcome { Win, Lose, Continue }
        private static BattleOutcome GetResult(Warrior warrior, Dragon dragon)
        {
            if (warrior.IsDead) return BattleOutcome.Lose;
            if (dragon.IsDead) return BattleOutcome.Win;
            return BattleOutcome.Continue;
        }

        #endregion
        #region Data structures

        private class Quest
        {
            private ulong PlayerId;
            private List<Dragon> Dragons;

            public bool IsHunting;
            public bool IsComplete { get { return Dragons.Count == 0; } }
            public Dragon CurrentDragon { get { return Dragons[0]; } }
            public Dragon[] GetDragonChoices { get { return Dragons.ToArray(); } }
            public int XP;

            //  constructor
            public Quest(ulong playerId, int characterLevel)
            {
                this.PlayerId = playerId;
                IsHunting = false;

                //  Every 3rd level, pack size increases by 1
                int packSize = Constants.rnd.Next(3) + (int)Math.Floor(characterLevel / 3f);

                //  Pack size must be at least 1
                packSize = Math.Max(1, packSize);

                //  Pack size should not exceed character level
                packSize = Math.Min(packSize, characterLevel);

                //  0 is easy, 2 is medium, 4 is hard
                int easy = 0;
                int medium = 2;
                int hard = 4;

                //  Goal values for remainder:
                //      character level 1 -> 0
                //      character level 2 -> 1
                //      character level 3 -> 2
                int remainder = (characterLevel - 1) % 3;
                switch (remainder)
                {
                    case 0: //  do nothing
                        break;
                    case 1:
                        if (Constants.rnd.NextDouble() < 0.5) easy++;
                        else medium++;
                        break;
                    case 2:
                        easy++;
                        if (Constants.rnd.NextDouble() < 0.5) medium++;
                        else hard++;
                        break;
                }
                int dragonLevel = (int)Math.Floor(characterLevel / 2f) + 1;

                //  all packs have 3 dragons
                Dragons = new List<Dragon>();                
                Dragons.Add(new Dragon(characterLevel, easy));
                if (packSize>1)Dragons.Add(new Dragon(characterLevel, medium));
                if (packSize>2)Dragons.Add(new Dragon(characterLevel, hard));

                //  extra dragons for large packs
                for (int i = 3; i < packSize; i++)
                {
                    Dragons.Add(new Dragon(characterLevel, Constants.rnd.Next(6)));
                }
            }

            /// <summary>
            /// 2017-8-25
            /// Sets the current dragon the warrior is fighting.
            /// If this is not done, the XP calculations will be incorrect.
            /// </summary>
            /// <param name="index"></param>
            public void SelectDragonIndex(int index)
            {
                Dragon inBattle = Dragons[index];
                Dragons.Remove(inBattle);
                Dragons.Insert(0, inBattle);
                IsHunting = true;
            }

            /// <summary>
            /// 2017-8-25
            /// Doubles the XP of previously defeated dragons.
            /// Adds the XP of the current dragon.
            /// </summary>
            public void DragonDefeated()
            {
                XP = XP * 2 + Dragons[0].XP;
                Dragons.RemoveAt(0);
                IsHunting = false;
            }
        }

        private enum Faces { Shield, Sword, Fire, Dragon, Mountain }

        private class Die
        {
            public Faces[] Sides;
            public Faces Result;

            public void Roll()
            {
                Result = Sides.Random();
            }
        }

        [System.Serializable]
        private class Warrior
        {
            [JsonIgnore]
            public int Experience { get { return XP; } }
            [JsonIgnore]
            public int Level { get { return types.Count - 2; } }
            [JsonIgnore]
            public int Swords { get { return dice.Where(x => x.Result.Equals(Faces.Sword)).Count(); } }
            [JsonIgnore]
            public int Shields { get { return dice.Where(x => x.Result.Equals(Faces.Shield)).Count(); } }
            [JsonIgnore]
            public int Fires { get { return dice.Where(x => x.Result.Equals(Faces.Fire)).Count(); } }
            [JsonIgnore]
            public int Health { get { return dice.Count; } }
            [JsonIgnore]
            public bool IsDead { get { return Health == 0; } }

            [JsonRequired]
            private ulong playerId;
            [JsonRequired]
            private int XP;
            [JsonRequired]
            private List<int> types;
            [NonSerialized]
            private List<Die> dice;

            //  constructor
            public Warrior(ulong playerId)
            {
                this.playerId = playerId;
                dice = new List<Die>();
                types = new List<int>();
                LevelUp();
                LevelUp();
                LevelUp();
                XP = 0;
            }

            public void Damage(int damage)
            {
                for (int i = 0; i < damage && dice.Count > 0; i++)
                {
                    dice.Remove(dice.Random());
                }
            }

            public void AwardXP(int XP)
            {
                this.XP += XP;
            }

            public bool ReadyToLevelUp()
            {
                if (Level == 1) return XP >= 4;
                if (Level == 2) return XP >= 10;
                if (Level == 3) return XP >= 22;

                //  the value of all previous quests up to the current level
                int required = 4 + 10 + 22;
                for (int i = 3; i < Level; i++)
                {
                    required += i + 1;
                    required += i + 1 + i + 2;
                    required += (i + 1) * 2 + i + 3;
                }
                
                return XP >= required;
            }

            public void LevelUp(int warriorType = 2)
            {
                dice.Add(MakeWarriorDie(warriorType));
                types.Add(warriorType);
            }

            /// <summary>
            /// Reset the warrior's health by regenerating it's dice.
            /// </summary>
            public void Ready()
            {
                dice.Clear();
                foreach (int warriorType in types)
                {
                    dice.Add(MakeWarriorDie(warriorType));
                }
            }

            public void Roll()
            {
                foreach (var d in dice) d.Roll();
            }

            public string DiceRollsToString()
            {
                string result = "";
                for (int i = 0; i < dice.Count; i++)
                {
                    result += dice[i].Result.ToString();
                    if (i < dice.Count - 1) result += ", ";
                }
                return result;
            }
        }

        private class Dragon
        {
            private static string[] DangerToSize = new string[] {
                "small", "medium", "large", "huge","gigantic", "colossal"
            };

            public int Level { get { return dice.Count - 2; } }
            public int DragonPart { get { return dice.Where(x => x.Result.Equals(Faces.Dragon)).Count(); } }
            public int Mountains { get { return dice.Where(x => x.Result.Equals(Faces.Mountain)).Count(); } }
            public int Fires { get { return dice.Where(x => x.Result.Equals(Faces.Fire)).Count(); } }
            public int Health { get { return dice.Count; } }
            public bool IsDead { get { return Health == 0; } }
            public int XP { get; private set; }

            private List<Die> dice = new List<Die>();

            //  constructor
            public Dragon(int level, int danger)
            {
                level += 2;
                for (int i = 0; i < level; i++)
                {
                    dice.Add(MakeDragonDie(danger));
                }
                XP = level + danger;
            }

            public void Damage(int damage)
            {
                for (int i = 0; i < damage && dice.Count > 0; i++)
                {
                    dice.Remove(dice.Random());
                }
            }

            public void Roll()
            {
                foreach (var d in dice) d.Roll();
            }

            public string Info()
            {
                return DangerToSize[XP - Level - 2] + " dragon";
            }

            public string DiceRollsToString()
            {
                string result = "";
                for (int i = 0; i < dice.Count; i++)
                {
                    result += dice[i].Result.ToString();
                    if (i < dice.Count - 1) result += ", ";
                }
                return result;
            }
        }

        /// <summary>
        /// Standard Warrior Die: 2 swords, 2 shields, 2 fires
        /// Optional: 1-4 swords, (4-swords) shields, 2 fires
        /// </summary>
        private static Die MakeWarriorDie(int warriorType)
        {
            List<Faces> sides = new List<Faces>();
            int shields = warriorType.Clamp(0, 4);
            int swords = 4 - shields;
            int fire = 6 - swords - shields;

            for (int i = 0; i < swords; i++) sides.Add(Faces.Sword);
            for (int i = 0; i < shields; i++) sides.Add(Faces.Shield);
            for (int i = 0; i < fire; i++) sides.Add(Faces.Fire);

            return new Die
            {
                Sides = sides.ToArray()
            };
        }

        /// <summary>
        /// Easy Dragon Die: 2 parts, 4 mountains
        /// Easy-Med Dragon Die: 2 parts, 3 mountains, 1 fire
        /// Medium Dragon Die: 2 parts, 2 mountains, 2 fires
        /// Medium-Hard Dragon Die: 2 parts, 1 mountain, 3 fires
        /// Hard Dragon Die: 2 parts, 4 fires
        /// Master Dragon Die: 1 part, 5 fires
        /// </summary>
        private static Die MakeDragonDie(int danger)
        {
            List<Faces> sides = new List<Faces>();
            int fires = danger.Clamp(0, 5);
            int parts = (6 - fires).Clamp(2, 1);
            int mountains = 6 - fires - parts;

            for (int i = 0; i < fires; i++) sides.Add(Faces.Fire);
            for (int i = 0; i < parts; i++) sides.Add(Faces.Dragon);
            for (int i = 0; i < mountains; i++) sides.Add(Faces.Mountain);

            return new Die
            {
                Sides = sides.ToArray()
            };
        }

        #endregion
    }
}