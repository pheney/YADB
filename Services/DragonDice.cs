using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Manage Game-State on a per-user basis
                
        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Generate a dragon Pack, based on player's Warrior.
        /// Present the dragons and ask the player to select one to battle.
        /// </summary>
        public static void StartQuest(ulong playerId)
        {
            Quest quest = GetOrCreateQuest(playerId);
            Warrior warrior = GetOrCreateWarrior(playerId);
            warrior.Ready();

            ContinueOrAbandonQuest(playerId);
        }

        /// <summary>
        /// 2017-8-25
        /// Preset players wth remaining dragons and ask which the player
        /// wants to battle, OR if he wants to Abandon the Quest.
        /// </summary>
        /// <param name="playerId"></param>
        public static void ContinueOrAbandonQuest(ulong playerId)
        {
            //  Present dragon pack and ask player which to fight
            //  This selection goes to BattleDragon()
            //  When Continue...
            int dragonSelection = 0;
            BattleDragon(playerId, dragonSelection);
            //  When End...
            AbandonQuest(playerId);
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
        public static void BattleDragon(ulong playerId, int dragonSelection)
        {
            Quest quest = GetOrCreateQuest(playerId);
            quest.SelectDragonIndex(dragonSelection);
            Dragon dragon = quest.CurrentDragon;
            Warrior warrior = GetOrCreateWarrior(playerId);

            //  Execute the battle, display each round
            do
            {
                Clash(warrior, dragon);
                //  Show results of the round to the player
                //  TODO

            } while (GetResult(warrior, dragon).Equals(BattleOutcome.Continue));
            
            //  Player either won or lost
            if (GetResult(warrior, dragon).Equals(BattleOutcome.Win))
            {
                //  Player won
                if (quest.IsComplete)
                {
                    WinQuest(playerId);
                } else
                {
                    //  Player must decide to continue or end quest
                    ContinueOrAbandonQuest(playerId);
                }
            }else
            {
                //  Player lost
                LoseQuest(playerId);
            }
        }

        private static void WinQuest(ulong playerId)
        {
            Quest quest = GetOrCreateQuest(playerId);
            Warrior warrior = GetOrCreateWarrior(playerId);
            //  Award Full Quest XP
            warrior.AwardXP(quest.XP);
            //  Player may have the option to level up
            if (warrior.ReadyToLevelUp())
            {
                //  Player may decide to level up
                //  TODO

                //  Get player LevelUp training priority: 
                //  focus on attack (4) vs defense (0), or some where in between?
                //  TODO
                warrior.LevelUp(2);
            }
            playerQuests.Remove(playerId);
        }

        private static void LoseQuest(ulong playerId)
        {
            playerQuests.Remove(playerId);
        }

        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Player has elected to end the Quest.
        /// This also occurs when the player disconnects during a battle.
        /// </summary>
        /// <param name="playerId"></param>
        public static void AbandonQuest(ulong playerId)
        {
            Quest quest = GetOrCreateQuest(playerId);
            Warrior warrior = GetOrCreateWarrior(playerId);
            warrior.AwardXP(quest.XP / 2);
            playerQuests.Remove(playerId);
        }
        
        /// <summary>
        /// 2017-8-25
        /// PLAYER ACTION
        /// Player's warrior has adanvced a level, and the player was prompted
        /// to level up the character. The player has selected how to "train"
        /// his character on a scale of 0-4, where 0 means train maximum defense,
        /// 4 means train maximum offense, and 2 means train equal defense and offense.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="warriorType"></param>
        public static void LevelUp(ulong playerId, int training)
        {
            GetOrCreateWarrior(playerId).LevelUp(training);
        }

        #endregion
        #region Helpers

        private static Dictionary<ulong, Quest> playerQuests;
        private static Quest GetOrCreateQuest(ulong playerId)
        {
            if (!playerQuests.ContainsKey(playerId))
            {
                Warrior warrior = GetOrCreateWarrior(playerId);
                Quest quest = new Quest(playerId, warrior.Level);
                playerQuests.Add(playerId, quest);
            }
            return playerQuests[playerId];
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
        private static Warrior GetOrCreateWarrior(ulong playerId)
        {
            if (!playerWarriors.ContainsKey(playerId))
            {
                playerWarriors.Add(playerId, new Warrior(playerId));
            }
            return playerWarriors[playerId];
        }

        /// <summary>
        /// 2017-8-25
        /// Conducts a round of battle. This modifies both the Warrior and Dragon objects.
        /// </summary>
        private static void Clash(Warrior warrior, Dragon dragon)
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

            //  Apply damage
            warrior.Damage(damageToWarrior);
            dragon.Damage(damageToDragon);
        }

        private enum BattleOutcome { Win, Lose, Continue }
        private static BattleOutcome GetResult(Warrior warrior, Dragon dragon) {

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

            public bool IsComplete { get { return Dragons.Count == 0; } }
            public Dragon CurrentDragon { get { return Dragons[0]; } }
            public Dragon[] GetDragonChoices { get { return Dragons.ToArray(); } }
            public int XP;

            public Quest(ulong playerId, int characterLevel)
            {
                //  Every 3rd level, packSize increases by 1
                int packSize = 2 + (int)Math.Floor(characterLevel / 3f);

                //  0 is easy, 2 is medium, 4 is hard
                int easy = 0;
                int medium = 2;
                int hard = 4;

                int remainder = characterLevel % 3;
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
                Dragons.Add(new Dragon(characterLevel, easy));
                Dragons.Add(new Dragon(characterLevel, medium));
                Dragons.Add(new Dragon(characterLevel, hard));

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
            }
        }

        public enum Faces { Shield, Sword, Fire, Dragon, Mountain }
        private class Die
        {
            public Faces[] Sides;
            public Faces Result;

            public void Roll()
            {
                Result = Sides.Random();
            }
        }
        
        public class Warrior
        {
            public int Level { get { return dice.Count - 2; } }
            public int Swords { get { return dice.Where(x => x.Result.Equals(Faces.Sword)).Count(); } }
            public int Shields { get { return dice.Where(x => x.Result.Equals(Faces.Shield)).Count(); } }
            public int Fires { get { return dice.Where(x => x.Result.Equals(Faces.Fire)).Count(); } }
            public int Health { get { return dice.Count; } }
            public bool IsDead { get { return Health == 0; } }

            private ulong playerId;
            private int XP;
            private List<Die> dice;
            private List<int> types;

            //  ctr
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
                for(int i = 0; i < damage && dice.Count>0; i++)
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
                //  the value of three quests at the current level
                int threeQuestXP = (Level + 6 + Level + 7 + Level + 8);

                //  the value of all previous quests up to the current level
                int previousXP = 0;
                for (int i = 0; i <=Level; i++) previousXP += 3+ (i + 6 + i + 7 + i + 8);

                //  the necessary XP to advance
                int requiredXP = previousXP + threeQuestXP;

                return XP >= requiredXP;
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
        }

        public class Dragon
        {
            public int Level { get { return dice.Count - 2; } }
            public int DragonPart { get { return dice.Where(x => x.Result.Equals(Faces.Dragon)).Count(); } }
            public int Mountains { get { return dice.Where(x => x.Result.Equals(Faces.Mountain)).Count(); } }
            public int Fires { get { return dice.Where(x => x.Result.Equals(Faces.Fire)).Count(); } }
            public int Health { get { return dice.Count; } }
            public bool IsDead { get { return Health == 0; } }
            public int XP { get; private set; }

            private List<Die> dice = new List<Die>();

            //  ctr
            public Dragon(int level, int danger)
            {
                for (int i = 0; i < level; i++)
                {
                    dice.Add(MakeDragonDie(danger));
                }
                XP = level + danger;
            }

            public void Damage(int damage)
            {
                for (int i = 0; i < damage&&dice.Count>0; i++)
                {
                    dice.Remove(dice.Random());
                }
            }

            public void Roll()
            {
                foreach (var d in dice) d.Roll();
            }
        }
        
        /// <summary>
        /// Standard Warrior Die: 2 swords, 2 shields, 2 fires
        /// Optional: 1-4 swords, (4-swords) shields, 2 fires
        /// </summary>
        private static Die MakeWarriorDie(int warriorType)
        {
            List<Faces> sides = new List<Faces>();
            int swords = warriorType.Clamp(0, 4);
            int shields = 4 - swords;
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