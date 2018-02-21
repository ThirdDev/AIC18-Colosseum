using System.Linq;

namespace Colosseum.Experiment.TowerStateMakers
{
    public class TowerState
    {
        public int[] Cannons { get; set; }
        public int[] Archers { get; set; }

        public override string ToString()
        {
            var c = Cannons.OrderBy(x => x).ToArray();
            var a = Archers.OrderBy(x => x).ToArray();

            return string.Join('-', c) + "," + string.Join('-', a);
        }
    }
}