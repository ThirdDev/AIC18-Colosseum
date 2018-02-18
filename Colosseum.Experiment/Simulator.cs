using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.Towers;
using Colosseum.Experiment.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment
{
    class Simulator
    {
        readonly int _pathLength;
        private readonly int _maximumTurns;
        readonly Cannon[] _cannons;
        readonly Archer[] _archers;

        public Simulator(int pathLength, int maximumTurns, IEnumerable<int> cannons, IEnumerable<int> archers)
        {
            _pathLength = pathLength;
            _maximumTurns = maximumTurns;
            _cannons = cannons.Select(x => new Cannon(x)).ToArray();
            _archers = archers.Select(x => new Archer(x)).ToArray();
        }

        public SimulationResult Simulate(IGeneParser parser, bool print = false)
        {
            var units = new List<Unit>();
            var deadUnits = new List<Unit>();
            var survivorUnits = new List<Unit>();

            var elapsedTurns = 0;

            foreach (var item in _cannons)
                item.Reset();
            foreach (var item in _archers)
                item.Reset();

            for (var i = 0; i < _maximumTurns; i++)
            {
                ProcessTowers(units);
                deadUnits.AddRange(ProcessDeadUnits(units));

                units.ForEach(x => x.GoForward());
                survivorUnits.AddRange(ProcessSurvivedUnits(units));

                var action = parser.Parse(i);
                for (var j = 0; j < action.CountOfCreeps; j++)
                    units.Add(new Creep());
                for (var j = 0; j < action.CountOfHeros; j++)
                    units.Add(new Hero());

                if (print)
                    PrintState(units, survivorUnits.Count);

                if ((units.Count == 0) && (i > parser.Gene.GenomesList.Count / 2))
                    break;

                elapsedTurns++;
            }

            var creepPrice = new Creep().Price * (units.Count(x => x is Creep) + deadUnits.Count(x => x is Creep) + survivorUnits.Count(x => x is Creep));
            var heroPrice = new Hero().Price * (units.Count(x => x is Hero) + deadUnits.Count(x => x is Hero) + survivorUnits.Count(x => x is Hero));

            return new SimulationResult
            {
                ReachedToTheEnd = survivorUnits.Count,
                DamagesToEnemyBase = survivorUnits.Sum(x => x.DamageToEnemyBase),
                DeadPositions = deadUnits.Select(x => x.Position).ToArray(),
                Length = _pathLength,
                Turns = elapsedTurns,
                TotalPrice = creepPrice + heroPrice,
            };
        }

        private void PrintState(List<Unit> units, int survivorUnitsCount)
        {
            //return;
            var creeps = units.Where(x => x is Creep);
            var heros = units.Where(x => x is Hero);
            Console.Write("<");
            for (var i = 0; i < _pathLength; i++)
            {
                var count = creeps.Count(x => x.Position == i);
                Console.Write((count == 0 ? " " : count.ToString() + ",").PadLeft(3));
            }
            Console.WriteLine(">");
            Console.Write("<");
            for (var i = 0; i < _pathLength; i++)
            {
                var count = heros.Count(x => x.Position == i);
                Console.Write((count == 0 ? " " : count.ToString() + ",").PadLeft(3));
            }
            Console.Write("> ");

            for (var i = 0; i < survivorUnitsCount; i++)
                Console.Write("*");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        private List<Unit> ProcessSurvivedUnits(List<Unit> units)
        {
            var survivedUnits = units.Where(x => x.Position >= _pathLength).ToList();
            units.RemoveAll(x => x.Position >= _pathLength);

            return survivedUnits;
        }

        private List<Unit> ProcessDeadUnits(List<Unit> units)
        {
            var deadUnits = units.Where(x => x.Health <= 0).ToList();
            units.RemoveAll(x => x.Health <= 0);

            return deadUnits;
        }

        private void ProcessTowers(List<Unit> units)
        {
            foreach (var item in _cannons)
            {
                if (!item.CanAttack())
                    continue;

                var probablyAffectedUnits = units.Where(x => item.IsInRange(x.Position)).OrderByDescending(x => x.Position);
                var affectedUnits = probablyAffectedUnits.Where(x => x.Position == probablyAffectedUnits.First().Position).ToList();
                if (affectedUnits.Count > 0)
                {
                    item.Attack();
                    affectedUnits.ForEach(x => x.GetAttackedByCannon());
                }
            }

            foreach (var item in _archers)
            {
                if (!item.CanAttack())
                    continue;

                var affectedUnit = units.Where(x => item.IsInRange(x.Position)).OrderByDescending(x => x.Position).FirstOrDefault();

                if (affectedUnit != null)
                {
                    item.Attack();
                    affectedUnit.GetAttackedByArcher();
                }
            }

            foreach (var item in _cannons)
                item.TurnPassed();
            foreach (var item in _archers)
                item.TurnPassed();
        }
    }
}
