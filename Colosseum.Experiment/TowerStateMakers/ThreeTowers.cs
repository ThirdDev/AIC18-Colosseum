using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class ThreeTowers : ITowerStateMaker
    {
        public List<TowerState> GetTowerStates(int pathLength)
        {
            var states = new List<TowerState>();

            for (var i = 0; i < pathLength; i++)
            {
                for (var j = i; j < pathLength; j++)
                {
                    for (var k = j; k < pathLength; k++)
                    {
                        states.AddRange(States(i, j, k));
                    }
                }
            }

            return states;
        }

        private List<TowerState> States(int a, int b, int c)
        {
            var states = new List<TowerState>();
            var s = new HashSet<string>();

            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    for (var k = 0; k < 2; k++)
                    {
                        var archers = new List<int>();
                        var cannons = new List<int>();
                        Add(a, i, archers, cannons);
                        Add(b, j, archers, cannons);
                        Add(c, k, archers, cannons);

                        var key = String.Join('-', archers) + "," + String.Join('-', cannons);
                        if (!s.Contains(key))
                        {
                            states.Add(new TowerState
                            {
                                Archers = archers.ToArray(),
                                Cannons = cannons.ToArray(),
                            });
                            s.Add(key);
                        }
                    }
                }
            }

            return states;
        }

        private static void Add(int a, int i, List<int> archers, List<int> cannons)
        {
            if (i == 0)
                archers.Add(a);
            else
                cannons.Add(a);
        }
    }
}
