using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Towers
{
    internal class Archer : Tower
    {
        public override int RechargeTime => 3;

        public Archer(int position)
        {
            Position = position;
        }
    }
}
