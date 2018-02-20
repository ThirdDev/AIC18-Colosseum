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
            List<TowerState> states = new List<TowerState>();

            for (int i = 0; i < pathLength; i++)
            {
                for (int j = i; j < pathLength; j++)
                {
                    for (int k = j; k < pathLength; k++)
                    {
                        states.AddRange(States(i, j, k));
                    }
                }
            }

            return states;
        }

        private List<TowerState> States(int a, int b, int c)
        {
            List<TowerState> states = new List<TowerState>();
            HashSet<string> s = new HashSet<string>();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        List<int> archers = new List<int>();
                        List<int> cannons = new List<int>();
                        Add(a, i, archers, cannons);
                        Add(b, j, archers, cannons);
                        Add(c, k, archers, cannons);

                        string key = String.Join('-', archers) + "," + String.Join('-', cannons);
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
