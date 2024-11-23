using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TournamentScheduler
{

    public class Scheduler
    {
        private readonly int N; // Количество участников
        private readonly int R; // Количество туров
        private readonly int K; // Количество площадок
        private readonly Random random;

        public Scheduler(int n, int r, int k)
        {
            N = n;
            R = r;
            K = k;
            random = new Random();
        }



        public int[,] GenerateSchedule(
            int maxGenerations,
            CancellationToken token,
            Action<int, int, int[,], int[][,]> curSterItems)
        {
            // Создание начальной популяции
            int[][,] population = new int[100][,];

            Parallel.For(0, 100, i =>
            {
                population[i] = CreateRandomSchedule();
            });

            int best_fit = 0;
            int[,] bestSolution = null;

            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Оценка приспособленности
                var fitnessScores = new int[population.Length];
                Parallel.For(0, population.Length, i =>
                {
                    fitnessScores[i] = CalculateFitness(population[i]);
                });

                // Селекция
                var selected = SelectBestSchedules(population, fitnessScores);
                bestSolution = selected.OrderByDescending(CalculateFitness).First();

                if (token.IsCancellationRequested)
                {
                    return bestSolution;
                }

                var fit = CalculateFitness(bestSolution);

                // Передаем текущую популяцию и лучшую особь в обработчик
                curSterItems(generation + 1, fit, bestSolution, population);

                if (fit > best_fit)
                {
                    best_fit = fit;
                }

                // Кроссовер и мутация
                population = GenerateNewPopulation(selected);
            }

            return bestSolution;
        }
        private int[,] CreateRandomSchedule()
        {
            var schedule = new int[R, N];
            for (int r = 0; r < R; r++)
            {
                var participants = Enumerable.Range(0, N).ToList();
                Shuffle(participants);
                var availableVenues = Enumerable.Range(1, K + 1).ToList();

                for (int i = 0; i < N / 2; i++)
                {
                    int player1 = participants[2 * i];
                    int player2 = participants[2 * i + 1];

                    int venueIndex = random.Next(availableVenues.Count);
                    int venue = availableVenues[venueIndex];

                    schedule[r, player1] = venue;
                    schedule[r, player2] = venue;

                    availableVenues.RemoveAt(venueIndex);
                }

                if (N % 2 == 1)
                {
                    int restingPlayer = participants.Last();
                    schedule[r, restingPlayer] = 0;
                }
            }

            return schedule;
        }

        private int CalculateFitness(int[,] schedule)
        {
            var opponents = new Dictionary<int, HashSet<int>>();
            var venues = new Dictionary<int, HashSet<int>>();

            for (int n = 0; n < N; n++)
            {
                opponents[n] = new HashSet<int>();
                venues[n] = new HashSet<int>();
            }

            for (int r = 0; r < R; r++)
            {
                var venueToPlayers = new Dictionary<int, List<int>>();

                for (int n = 0; n < N; n++)
                {
                    int venue = schedule[r, n];
                    if (venue == 0)
                        continue;

                    if (!venueToPlayers.ContainsKey(venue))
                        venueToPlayers[venue] = new List<int>();

                    venueToPlayers[venue].Add(n);
                }

                foreach (var playersOnVenue in venueToPlayers.Values)
                {
                    foreach (var player in playersOnVenue)
                    {
                        foreach (var opponent in playersOnVenue)
                        {
                            if (player != opponent)
                            {
                                opponents[player].Add(opponent);
                            }
                        }
                    }
                }

                foreach (var kvp in venueToPlayers)
                {
                    int venue = kvp.Key;
                    foreach (var player in kvp.Value)
                    {
                        venues[player].Add(venue);
                    }
                }
            }

            int minOpponents = opponents.Values.Min(x => x.Count);
            int minVenues = venues.Values.Min(x => x.Count);

            return minOpponents * 1000 + minVenues;
        }

        private int[][,] SelectBestSchedules(int[][,] population, int[] fitnessScores)
        {
            var ordered = population.Zip(fitnessScores, (schedule, fitness) => new { schedule, fitness })
                                    .OrderByDescending(x => x.fitness).Take(20).Select(x => x.schedule).ToArray();
            return ordered;
        }

        private int[][,] GenerateNewPopulation(int[][,] selected)
        {
            var newPopulation = new int[100][,];
            Array.Copy(selected, newPopulation, selected.Length);

            for (int i = selected.Length; i < 100; i++)
            {
                var parent1 = selected[random.Next(selected.Length)];
                var parent2 = selected[random.Next(selected.Length)];
                var child = Crossover(parent1, parent2);
                Mutate(child);
                newPopulation[i] = child;
            }

            return newPopulation;
        }

        private int[,] Crossover(int[,] parent1, int[,] parent2)
        {
            int[,] child = new int[R, N];

            for (int r = 0; r < R; r++)
            {
                int randNum = random.Next(2);
                int[,] sourceParent = (randNum == 0) ? parent1 : parent2;
                for (int i = 0; i < N; i++)
                {
                    child[r, i] = sourceParent[r, i];
                }
            }

            return child;
        }

        private void Mutate(int[,] child)
        {
            for (int r = 0; r < R; r++)
            {
                if (random.NextDouble() < 0.1)
                {
                    int i1 = random.Next(N);
                    int i2 = random.Next(N);

                    (child[r, i1], child[r, i2]) = (child[r, i2], child[r, i1]);
                }
            }
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
