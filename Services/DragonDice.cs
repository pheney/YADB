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

        private static int delayMillis = 300;

        #region External Access

        private static string[] rules = new string[] {
            "Defeat dragons to earn XP and level up.",
            "Each dragon you defeat during a quest, doubles the XP of previous dragons defeated on the same quest.",
            "Quests can be abandoned, but grant reduced XP.",
            "Battles can be abandoned. This will grant 0 XP for the battle, and abandon the quest.",
            "Dying ends a quest and the warrior loses a level."
        };

        /// <summary>
        /// 2017-8-25
        /// Entry point into the game.
        /// Load (or create) player's character.
        /// Generate a quest based on the character.
        /// Begin the game loop.
        /// </summary>
        public static async Task StartGame(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            
            //  Create (or load) the player's warrior
            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            //  Reset warrior health
            warrior.Ready();

            //  Welcome message
            string welcomeMessage = "Welcome! Go on a quest and "
                + "fight dragons. Don't die.\n\n";

            //  Instructions
            foreach (var c in rules) welcomeMessage += "**\\*** " + c + "\n";

            //  Write current warrior stats
            welcomeMessage += "\nYour warrior is ready! Warrior is level " + warrior.Level + ", and has " + warrior.Health + " health, and " + warrior.Experience + " XP.";
            await Send(context, welcomeMessage);

            //  Create a quest
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);

            await DisplayReadyChoices(context);
        }

        /// <summary>
        /// 2017-8-25
        /// Indicates if the player ID is actively playing a game.
        /// </summary>
        public static bool IsPlaying(ulong playerId)
        {
            //  check quest to see if player is playing a game
            if (playerQuests == null) return false;
            if (!playerQuests.ContainsKey(playerId)) return false;

            return true;                
        }

        /// <summary>
        /// 2017-8-26
        /// Primary input handler. This handles all input for the game and then sends
        /// it to the specific parts as required.
        /// </summary>
        public static async Task HandleInput(ICommandContext context, string input)
        {
            ulong playerId = context.User.Id;            

            Quest quest;
            await GetOrCreateQuest(playerId, out quest);

            if (input.Equals("show dice", StringComparison.OrdinalIgnoreCase)) quest.ShowResults = true;
            if (input.Equals("hide dice", StringComparison.OrdinalIgnoreCase)) quest.ShowResults = false;

            switch (quest.State)
            {
                case Quest.QuestState.Ready:
                    await ParseReadyAction(context, input);
                    break;
                case Quest.QuestState.Hunting:
                    await ParseHuntAction(context, input);
                    break;
                case Quest.QuestState.Complete:
                    Warrior warrior;
                    await GetOrCreateWarrior(playerId, out warrior);
                    if (warrior.ReadyToLevelUp()) await ParseLevelUpInput(context, input);
                    break;
            }
        }

        #endregion
        #region Input Handlers

        /// <summary>
        /// 2017-8-25
        /// Expects the number of the dragon the warrior will fight.
        /// Expects "A" for abort quest.
        /// </summary>
        private static async Task ParseReadyAction(ICommandContext context, string choice)
        {
            Quest quest;
            await GetOrCreateQuest(context.User.Id, out quest);

            //  When the quest is "ready" 
            //  The options are "regenerate quest," "abort," and the number of the dragon to hunt.

            if (choice.Equals(readyCommands[0][0], StringComparison.OrdinalIgnoreCase))
            {
                await ChangeQuest(context.User.Id);
            }

            if (choice.Equals(readyCommands[1][0], StringComparison.OrdinalIgnoreCase))
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

            await DisplayReadyChoices(context);
        }

        private static async Task ParseHuntAction(ICommandContext context, string choice)
        {
            string commands = "";
            foreach (var h in huntCommands) commands += h[0].ToLower();
            if (commands.Contains(choice.ToLower()))
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

            await DisplayHuntChoices(context);
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
        private static async Task ParseLevelUpInput(ICommandContext context, string choice)
        {
            int trainingIndex = 3;
            if (int.TryParse(choice, out trainingIndex))
            {
                //  player is display choices from 1 - 5,
                //  convert this to 0-index selection
                trainingIndex--;
                await LevelUp(context, trainingIndex);
                return;
            }
            else
            {
                await DisplayLevelUpChoices(context);
            }
        }
        
        #endregion
        #region Display Info

        private static string callToAction = "Make your selection!";

        private static string[][] readyCommands = new string[][]
        {
            new string[] {"g", "Generate a new quest"},
            new string[] {"a", "Abandon the quest"}
        };

        /// <summary>
        /// 2017-8-25
        /// Preset players wth remaining dragons and ask which the player
        /// wants to battle, OR if he wants to Abandon the Quest.
        /// </summary>
        /// <param name="playerId"></param>
        private static async Task DisplayReadyChoices(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            int required = quest.Required;
            int choices = quest.GetDragonChoices.Length;

            //  Narrate state
            bool single = quest.GetDragonChoices.Length == 1;
            string questStatus = "Your warrior must slay " + required
                + " "+ (single ? "dragon" : "dragons")
                + " to complete this quest.";

            //  Display quest status
            string remaining = "There " +(single?"is":"are")
                + " "+choices 
                + " " + (single ? "dragon" : "dragons")
                + " remaining on this quest.";
                        
            //  Display user options
            string options = "";
            for (int i = 0; i < choices; i++)
            {
                int choiceId = i + 1;
                Dragon d = quest.GetDragonChoices[i];
                options += "**"+choiceId + "** : a " + d.Description + "\n";
            }
            options += "\n";
            foreach (var c in readyCommands) options += "**" + c[0] + "** : " + c[1] + "\n";

            string message = options + "\n" + callToAction;
            await Send(context, questStatus + " " + remaining+"\n\n"+message);
        }

        private static string[][] huntCommands = new string[][]
        {
            new string[] {"c", "Continue the fight"},
            new string[] {"a", "Abandon the quest"}
        };

        private static async Task DisplayHuntChoices(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            string message = "You are in the middle of a hunt. You may take the following "
                + "actions:\n\n";
            foreach (var c in huntCommands) message += "**"+c[0] + "** : " + c[1] + "\n";
            await Send(context, message);
        }

        private static string[] trainingChoices = new string[]
        {
            "Offense fighter: maximum offense, no defense",
            "Mostly offense",
            "Balanced fighter: equal offensive and defense",
            "Mostly defense",
            "Defensive fighter: no offense, maximum defense"
        };
        
        private static async Task DisplayLevelUpChoices(ICommandContext context)
        {
            string levelUp = "Your warrior has gained a level!";
            string trainingPriority = "Select the type of training your warrior will learn:";
            string options = "";
            for (int i = 0; i < trainingChoices.Length; i++)
            {
                options += (i + 1) + ": " + trainingChoices[i] + "\n";
            }

            string instruction = "Choose a number from 1 through 5.";
            await Send(context, levelUp + " " + trainingPriority + "\n\n" + options + "\n" + instruction);
        }

        #endregion
        #region Actions

        /// <summary>
        /// 2017-8-25
        /// Player selects a dragon from the Pack to battle.
        /// This progressively shows the battle results for each "turn"
        /// until either the Dragon or Warrior is killed.
        /// </summary>
        private static async Task BattleDragon(ICommandContext context, int dragonSelection)
        {
            ulong playerId = context.User.Id;

            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            quest.HuntDragon(dragonSelection - 1);
            Dragon dragon = quest.CurrentDragon;

            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            //  Execute a round of battle

            await Send(context, "_Searching for the " + quest.CurrentDragon.Description + "_\n\n");

            int[] damage = Clash(warrior, dragon);
            int damageToWarrior = damage[0];
            int damageToDragon = damage[1];
            int fire = quest.CurrentDragon.Fires + warrior.Fires;

            //  Show die results
            if (quest.ShowResults)
            {
                await Task.Delay(delayMillis);
                await Send(context, "Warrior rolls: " + warrior.DiceRollsToString() +
                    "\nDragon rolls: " + dragon.DiceRollsToString());
            }

            //  Narrate outcome
            await Task.Delay(delayMillis);
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
                await Send(context, "Warrior defeated the " + quest.CurrentDragon.Description + "!");
                quest.DragonDefeated();

                if (quest.IsComplete)
                {
                    await WinQuest(context);
                    return;
                }
                else
                {
                    await Task.Delay(delayMillis);
                    //  Player must decide to continue or end quest
                    await DisplayReadyChoices(context);
                    return;
                }
            }

            //  When the player loses
            if (GetResult(warrior, dragon).Equals(BattleOutcome.Lose))
            {
                await LoseQuest(context);
                return;
            }

            //  Hunt continues
            await DisplayHuntChoices(context);
        }

        private static async Task WinQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            int xp = quest.XP;
            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);
            warrior.AwardXP(xp);

            string congrats = "\nCongratulations! You completed your quest!";            
            string xpAward = "Your warrior earned " + xp + " experience!";
            await Send(context, congrats + " " + xpAward);
            
            await EndQuest(context);
        }

        /// <summary>
        /// 2017-8-25
        /// Player has elected to end the Quest.
        /// This also occurs when the player disconnects during a battle.
        /// </summary>
        /// <param name="playerId"></param>
        private static async Task AbandonQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Quest quest;
            await GetOrCreateQuest(playerId, out quest);
            quest.State = Quest.QuestState.Complete;

            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            int xp = quest.XP / 2;
            await Send(context, "Your warrior earned " + xp + " experience."); ;
            warrior.AwardXP(xp);
            
            await EndQuest(context);
        }

        private static async Task LoseQuest(ICommandContext context)
        {
            string message = "Your warrior was killed. Resurrecton has a price... ";

            ulong playerId = context.User.Id;
            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);
            if (warrior.Level > 1)
            {
                warrior.LevelDown();
                message += "Warrior drops to level " + warrior.Level;
            }

            await Send(context, message);
            await EndQuest(context);
        }

        private static async Task EndQuest(ICommandContext context)
        {
            ulong playerId = context.User.Id;
            Warrior warrior;
            await GetOrCreateWarrior(playerId, out warrior);

            if (warrior.ReadyToLevelUp())
            {
                await DisplayLevelUpChoices(context);
                return;
            }
            await DeleteQuest(playerId);

            if (DragonDiceData.Get.playerWarriors.ContainsKey(playerId))
            {
                DragonDiceData.Get.playerWarriors.Remove(playerId);
            }
            DragonDiceData.Get.playerWarriors.Add(playerId, warrior);
            await FileOperations.SaveAsJson(DragonDiceData.Get);
            await Send(context, "Your quest has ended.");
        }

        private static async Task LevelUp(ICommandContext context, int training)
        {
            Warrior warrior;
            await GetOrCreateWarrior(context.User.Id, out warrior);
            warrior.LevelUp(training);
            await FileOperations.SaveAsJson(DragonDiceData.Get);
            await Send(context, "Your warrior has advanced to level " + warrior.Level + "!");
            await EndQuest(context);            
        }

        #endregion
        #region Help Displays

        private static string[][] infoCommands = new string[][] {
            new string[] { "i", "Show info"},
            new string[] { "w", "Show _warrior_ status"},
            new string[] { "d", "Show _dragon_ status"},
            new string[] { "r", "Show _remaing_ dragons on the quest"}
        };

        private static async Task HelpInfo(ICommandContext context)
        {
            await Task.Delay(delayMillis);
            string message = "You are in a quest. You may ask for the following "
                + "information:\n\n";
            foreach (var c in huntCommands) message += c[0] + " : " + c[1] + "\n";
            await Send(context, message);
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

        private static Task ChangeQuest(ulong playerId)
        {
            if (playerQuests != null
                && playerQuests.ContainsKey(playerId))
            {
                playerQuests.Remove(playerId);
                Quest q;
                GetOrCreateQuest(playerId, out q);
            }
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
            public bool ShowResults;
            private List<Dragon> Dragons;

            public enum QuestLevel { Normal, Tough, Adventurous, Heroic, Epic }
            private QuestLevel questLevel;

            public enum QuestState { Complete, Hunting, Ready }
            public QuestState State;

            public bool IsHunting { get { return State.Equals(QuestState.Hunting); } }
            public bool IsComplete { get { return State.Equals(QuestState.Complete); } }
            public Dragon CurrentDragon { get { return Dragons[0]; } }
            public Dragon[] GetDragonChoices { get { return Dragons.ToArray(); } }
            public int Required { get; private set; }
            public int XP;

            //  constructor
            public Quest(ulong playerId, int characterLevel)
            {
                this.PlayerId = playerId;
                ShowResults = false;

                questLevel = (QuestLevel)Math.Min(Math.Floor(characterLevel / 3f), 4);

                //  Every quest has 1-3 dragons
                int packSize = 1 + Constants.rnd.Next(3);

                //  Pack size should not exceed character level
                packSize = Math.Min(packSize, characterLevel);

                int questToPart = 5 - (int)questLevel;
                int maxSize = 7 - questToPart;

                List<int> dragonSizes = new List<int>();
                for (int i = 0; i < maxSize; i++) dragonSizes.Add(i);

                //  create dragons
                Dragons = new List<Dragon>();
                for (int i = 0; i < packSize; i++)
                {
                    int size = dragonSizes.Random();
                    dragonSizes.Remove(size);
                    Dragons.Add(new Dragon((int)questLevel, size));
                }

                Required = packSize;
                State = QuestState.Ready;
            }

            /// <summary>
            /// 2017-8-25
            /// Sets the current dragon the warrior is fighting.
            /// If this is not done, the XP calculations will be incorrect.
            /// </summary>
            /// <param name="index"></param>
            public void HuntDragon(int index)
            {
                Dragon inBattle = Dragons[index];
                Dragons.Remove(inBattle);
                Dragons.Insert(0, inBattle);
                State = QuestState.Hunting;
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
                if (Dragons.Count > 0) State = QuestState.Ready;
                else State = QuestState.Complete;
            }

            public string Description { get { return questLevel.ToString(); } }
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
                return XP >= GetRequiredXP(Level + 1);
            }

            /// <summary>
            /// Returns the total XP required to get to the level parameter.
            /// </summary>
            private int GetRequiredXP(int level)
            {
                if (level == 1) return 0;
                if (level == 2) return 4;
                if (level == 3) return 10;
                if (level == 4) return 22;

                //  the value of all previous quests up to the current level
                int required = 4 + 10 + 22;
                for (int i = 4; i < level; i++)
                {
                    required += i + 1;
                    required += i + 1 + i + 2;
                    required += (i + 1) * 2 + i + 3;
                }
                return required;
            }

            public void LevelUp(int warriorType = 2)
            {
                dice.Add(MakeWarriorDie(warriorType));
                types.Add(warriorType);
            }

            /// <summary>
            /// Reduces the warrior's level by 1.
            /// Removes the last Warrior Die added.
            /// Reduces the warrior's XP to the mid point of the previous level.
            /// </summary>
            public void LevelDown()
            {
                if (this.Level == 1) return;
                int currentLevel = this.Level;
                int previousLevel = currentLevel - 1;
                XP = (GetRequiredXP(currentLevel) + GetRequiredXP(previousLevel)) / 2;
                types.RemoveAt(types.Count - 1);
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
            //  Indicates number of parts per die (5-1)
            private static string[] QuestLevel = new string[] {
                "dangerous", "fearsome", "elder", "ancient", "legendary"
            };

            //  Indicates number of fires per die (0-5)
            private static string[] DragonLevel = new string[] {
                "common", "large", "huge", "great", "gigantic", "colossal"
            };
            
            public int Level { get { return dice.Count - 2; } }
            public int DragonPart { get { return dice.Where(x => x.Result.Equals(Faces.Dragon)).Count(); } }
            public int Mountains { get { return dice.Where(x => x.Result.Equals(Faces.Mountain)).Count(); } }
            public int Fires { get { return dice.Where(x => x.Result.Equals(Faces.Fire)).Count(); } }
            public int Health { get { return dice.Count; } }
            public bool IsDead { get { return Health == 0; } }
            public int XP { get; private set; }
            public string Description { get; private set; }

            private List<Die> dice;

            /// <summary>
            /// 2017-8-28
            /// Dragons are created as follows. Each die is generated using the 
            /// following rules.
            /// 
            /// questLevel: 0..4
            ///     Determines the number of "Dragon" parts that appear on each die.
            ///     The number of parts = 5 - questLevel
            ///     
            /// dragonLevel: 0..5
            ///     Determines the number of "Fire" sides on each die.
            ///     This cannot exceed 6 - questLevel.
            ///     This cannot exceed questLevel +1.
            ///     Quests that have multiple dragons should have different dragonLevels
            ///         for each dragon.
            ///         
            /// The remainder of 6 - questLevel - dragonLevel is the number of "Mountain" sides on each die.
            ///     
            /// </summary>
            /// <param name="questLevel">Quest Level, 0..4</param>
            /// <param name="dragonLevel">Dragon level, 0..5 (small, med, large)</param>
            /// <returns></returns>
            public Dragon(int questLevel, int dragonLevel)
            {
                //  How many health the dragon has (health = number of dice)
                int health = 3 + questLevel;

                //  Design the dice

                //  QL 0 -> 5 parts, QL 2 -> 3 parts, QL 4 -> 1 part
                int partPerDie = 5 - questLevel;
                int firePerDie = dragonLevel;
                int mountain = 6 - (partPerDie + firePerDie);
                if (mountain < 0) health += -mountain;

                //  Create the dice
                dice = new List<Die>();
                for (int i = 0; i < health; i++)
                {
                    dice.Add(MakeDragonDie(partPerDie, firePerDie));
                }

                XP = 1 + (questLevel + 1) * (dragonLevel + 1);
                Description = QuestLevel[questLevel] + " " + DragonLevel[dragonLevel] + " dragon";
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
        /// 2017-8-28
        /// No value checking. Dragon + fire MUST be <= 6.
        /// </summary>
        private static Die MakeDragonDie(int dragon, int fire)
        {
            int mountain = 6 - fire - dragon;
            List<Faces> sides = new List<Faces>();

            for (int i = 0; i < fire; i++) sides.Add(Faces.Fire);
            for (int i = 0; i < dragon; i++) sides.Add(Faces.Dragon);
            for (int i = 0; i < mountain; i++) sides.Add(Faces.Mountain);

            return new Die
            {
                Sides = sides.ToArray()
            };
        }

        #endregion
    }
}