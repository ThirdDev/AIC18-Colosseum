using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Towers
{
    internal class Archer : Tower
    {
        public override int RechargeTime => 4;

        public Archer(int position)
        {
            Position = position;
        }
    }
}
