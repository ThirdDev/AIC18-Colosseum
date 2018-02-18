using System;

namespace Colosseum.Experiment.Units
{
    internal abstract class Unit
    {
        public int Turn { get; protected set; }
        public int Position { get; protected set; }

        public abstract int MoveCycle { get; }
        public abstract int Health { get; set; }
        public abstract int DamageByCannon { get; }
        public abstract int DamageByArcher { get; }
        public abstract int Price { get; }
        public abstract int DamageToEnemyBase { get; }


        /// <summary>
        /// Must be called each turn
        /// </summary>
        /// <returns>New position</returns>
        public int GoForward()
        {
            Turn++;
            if (Turn % MoveCycle == 0)
                Position++;
            return Position;
        }

        internal void GetAttackedByCannon()
        {
            Health -= DamageByCannon;
        }

        internal void GetAttackedByArcher()
        {
            Health -= DamageByArcher;
        }
    }
}