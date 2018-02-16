using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Units
{
    internal class Hero : Unit
    {
        public override int MoveCycle => 3;

        public override int Health { get; set; } = 240;

        public override int Price => 180;

        public override int DamageByCannon => 10;

        public override int DamageByArcher => 40;
    }
}
