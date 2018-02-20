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
            List<TowerState> states = new List<TowerState>();

            for (int i = 0; i < pathLength; i++)
            {
                for (int j = i; j < pathLength; j++)
                {
                    for (int k = j; k < pathLength; k++)
                    {
                        for (int l = k; l < pathLength; l++)
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
            List<TowerState> states = new List<TowerState>();
            HashSet<string> s = new HashSet<string>();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < 2; l++)
                        {
                            List<int> archers = new List<int>();
                            List<int> cannons = new List<int>();
                            Add(a, i, archers, cannons);
                            Add(b, j, archers, cannons);
                            Add(c, k, archers, cannons);
                            Add(d, l, archers, cannons);

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
