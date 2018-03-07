using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Units
{
    internal class Creep : Unit
    {
        public override int MoveCycle => 2;

        public override int Health { get; set; } = 32;

        public override int Price => 40;

        public override int DamageByCannon => 10;

        public override int DamageByArcher => 60;

        public override int DamageToEnemyBase => 1;
    }
}
