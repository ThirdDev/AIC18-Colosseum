using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.Towers
{
    internal abstract class Tower
    {
        public abstract int RechargeTime { get; }
        public int RechargeCounter { get; private set; } = -1;
        public int Position { get; internal set; }

        public bool CanAttack()
        {
            return (RechargeCounter == -1) || (RechargeCounter % RechargeTime == 0);
        }

        public void Attack()
        {
            RechargeCounter = 0;
        }

        public void TurnPassed()
        {
            if (RechargeCounter == -1)
                return;

            RechargeCounter++;
        }

        public bool IsInRange(int position)
        {
            return position == Position - 1 || position == Position || position == Position + 1;
        }
    }
}
