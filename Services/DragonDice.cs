using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Services
{
    /// <summary>
    /// 2017-8-125
    /// </summary>
    public static class DragonDice
    {
        #region Manage Game-State on a per-user basis
        #endregion
        #region Data structures

        public enum Faces { Shield, Sword, Fire, Head, Body, Tail, Mountain }
        private class Die
        {
            public string Name;
            public Faces[] Sides;
            public Faces Result;
        }

        private class WarriorDie : Die { }

        private class DragonHead : Die { }

        private class Dragon
        {
            //  Dragon is 3 Dice
        }

        private class Warrior
        {
            //  Standard (level 1) Warrior has 3 Dice
            public int Level { get { return dice.Count - 2; } }

            public int Swords { get { return -1; } }
            public int Shields { get { return -1; } }
            public int Fires { get { return -1; } }

            private int health;
            private List<Die> dice;
            
            public Warrior()
            {
                dice = new List<Die>();
                LevelUp();
                LevelUp();
                LevelUp();
            }

            public void LevelUp(int warriorType = 2)
            {
                dice.Add(MakeWarriorDie(warriorType));
            }

            public void Ready()
            {
                health = dice.Count;
            }
        }

        #endregion
        #region API


        #endregion
        #region Helpers
        
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
        private static Die MakeDragonDie(Faces part, int danger)
        {
            List<Faces> sides = new List<Faces>();
            int fires = danger.Clamp(0, 5);
            int parts = (6 - fires).Clamp(2, 1);
            int mountains = 6 - fires - parts;

            for (int i = 0; i < fires; i++) sides.Add(Faces.Fire);
            for (int i = 0; i < parts; i++) sides.Add(part);
            for (int i = 0; i < mountains; i++) sides.Add(Faces.Mountain);

            return new Die
            {
                Sides = sides.ToArray()
            };
        }

        private static void Roll(Die die)
        {
            die.Result = die.Sides.Random();
        }

        #endregion
    }
}