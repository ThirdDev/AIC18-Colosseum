using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class FourTowers : ITowerStateMaker
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
                        for (var l = k; l < pathLength; l++)
                        {
                            states.AddRange(States(i, j, k, l));
                        }
                    }
                }
            }

            return states;
        }

        private List<TowerState> States(int a, int b, int c, int d)
        {
            var states = new List<TowerState>();
            var s = new HashSet<string>();

            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    for (var k = 0; k < 2; k++)
                    {
                        for (var l = 0; l < 2; l++)
                        {
                            var archers = new List<int>();
                            var cannons = new List<int>();
                            Add(a, i, archers, cannons);
                            Add(b, j, archers, cannons);
                            Add(c, k, archers, cannons);
                            Add(d, l, archers, cannons);

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
