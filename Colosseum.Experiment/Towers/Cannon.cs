using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Towers
{
    internal class Cannon : Tower
    {
        public override int RechargeTime => 4;

        public Cannon(int position)
        {
            Position = position;
        }
    }
}
