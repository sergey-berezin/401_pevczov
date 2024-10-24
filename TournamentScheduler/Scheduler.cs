using System;
using System.Collections.Generic;
using System.Linq;

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

        public int[,] GenerateSchedule(int maxGenerations, CancellationToken token, Action<int, int, int[,]> curSterItems)
        {
            // Создание начальной популяции
            var population = new List<int[,]>();
            var lockObj = new object(); // Объект для блокировки

            Parallel.For(0, 100, i =>
            {
                var schedule = CreateRandomSchedule();

                // Блокировка для потокобезопасного добавления в список
                lock (lockObj)
                {
                    population.Add(schedule);
                }
            });
            var best_fit = 0;

            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Оценка приспособленности каждой хромосомы (расписания) с использованием параллелизма
                var fitnessScores = new int[population.Count];
                Parallel.For(0, population.Count, i =>
                {
                    fitnessScores[i] = CalculateFitness(population[i]);
                });

                // Селекция: отбор лучших решений
                var selected = SelectBestSchedules(population, fitnessScores);

                var bestSolution = selected.OrderByDescending(CalculateFitness).First();

                if (token.IsCancellationRequested)
                {
                    return bestSolution;
                }

                var fit = CalculateFitness(bestSolution);
                curSterItems(generation + 1, fit, bestSolution);

                if (fit > best_fit) { best_fit = fit; }

                // Кроссовер и мутации для создания новой популяции
                var newPopulation = GenerateNewPopulation(selected);

                // Обновляем популяцию
                population = newPopulation;
            }

            // Возвращаем лучшее расписание
            return population.OrderByDescending(CalculateFitness).First();
        }


        private int[,] CreateRandomSchedule()
        {
            var schedule = new int[R, N];
            for (int r = 0; r < R; r++)
            {
                // Список участников
                var participants = Enumerable.Range(0, N).ToList();
                Shuffle(participants); // Перемешиваем участников для случайного распределения по парам
                var availableVenues = Enumerable.Range(1, K + 1).ToList();
                // Распределяем пары по площадкам
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

                // Если участников нечётное количество, один отдыхает
                if (N % 2 == 1)
                {
                    int restingPlayer = participants.Last();
                    schedule[r, restingPlayer] = 0; // 0 означает, что игрок не играет в этом туре
                }
            }

            return schedule;
        }

        private int CalculateFitness(int[,] schedule)
        {
            // Считаем уникальных соперников и посещённые площадки для каждого участника
            var opponents = new Dictionary<int, HashSet<int>>();
            var venues = new Dictionary<int, HashSet<int>>();

            // Инициализация словарей для каждого участника
            for (int n = 0; n < N; n++)
            {
                opponents[n] = new HashSet<int>();
                venues[n] = new HashSet<int>();
            }

            // Перебор туров
            for (int r = 0; r < R; r++)
            {
                // Словарь для отслеживания того, кто играет на какой площадке в данном туре
                var venueToPlayers = new Dictionary<int, List<int>>();

                // Заполняем, кто на какой площадке играет
                for (int n = 0; n < N; n++)
                {
                    int venue = schedule[r, n];
                    if (venue == 0)
                        continue; // Игрок отдыхает в этом туре, пропускаем его

                    // Если площадка уже есть в словаре, добавляем игрока к списку
                    if (!venueToPlayers.ContainsKey(venue))
                        venueToPlayers[venue] = new List<int>();

                    venueToPlayers[venue].Add(n);
                }

                // Теперь добавляем соперников для всех игроков, которые играют на одной площадке
                foreach (var playersOnVenue in venueToPlayers.Values)
                {
                    // Для каждого игрока на площадке добавляем всех остальных игроков с этой площадки в соперники

                    foreach (var player in playersOnVenue)
                    {
                        foreach (var opponent in playersOnVenue)
                        {
                            if (player != opponent) // Не добавляем себя в соперники
                            {
                                opponents[player].Add(opponent);
                            }
                        }
                    }
                }

                // Добавляем посещённые площадки для каждого игрока
                foreach (var kvp in venueToPlayers)
                {
                    int venue = kvp.Key;
                    foreach (var player in kvp.Value)
                    {
                        venues[player].Add(venue);
                    }
                }
            }

            // Находим минимальные количества уникальных соперников и посещённых площадок для всех участников
            int minOpponents = opponents.Values.Min(x => x.Count);
            int minVenues = venues.Values.Min(x => x.Count);

            // Фитнес-функция: максимизация минимума соперников и площадок
            return minOpponents * 1000 + minVenues;
        }


        private List<int[,]> SelectBestSchedules(List<int[,]> population, int[] fitnessScores)
        {
            var bestSchedules = new List<int[,]>();
            var ordered = population.Zip(fitnessScores, (schedule, fitness) => new { schedule, fitness })
                                    .OrderByDescending(x => x.fitness).Take(20).ToList();
            bestSchedules.AddRange(ordered.Select(x => x.schedule));
            return bestSchedules;
        }

        private List<int[,]> GenerateNewPopulation(List<int[,]> selected)
        {
            var newPopulation = new List<int[,]>();
            newPopulation.AddRange(selected);

            for (int i = 0; i < 80; i++)
            {
                var parent1 = selected[random.Next(selected.Count)];
                var parent2 = selected[random.Next(selected.Count)];
                var child = Crossover(parent1, parent2);
                Mutate(child);
                newPopulation.Add(child);
            }

            return newPopulation;
        }

        private int[,] Crossover(int[,] parent1, int[,] parent2)
        {
            int[,] child = new int[R, N];

            // Чередуем родителей по турам
            for (int r = 0; r < R; r++)
            {
                // Если cлучайное число 0, то parent1, если 1 — от parent2
                int randNum = random.Next(2);
                int[,] sourceParent = (randNum == 0) ? parent1 : parent2;
                for (int i = 0; i < N; i++)
                {
                    // Копируем номера площадок для игроков
                    child[r, i] = sourceParent[r, i];
                }
            }

            return child;
        }


        private void Mutate(int[,] child)
        {
            for (int r = 0; r < R; r++)
            {
                if (random.NextDouble() < 0.1) // 10% шанс мутации поменять площадки местами
                {
                    int i1 = random.Next(N);
                    int i2 = 0;
                    while (child[r, i1] != 0)
                    {
                        i1 = random.Next(N);
                    }
                    for (int j = 0; j < N; j++)
                    {
                        if (child[r, j] == child[r, i1])
                        {
                            i2 = j; break;
                        }
                    }
                    int i3 = random.Next(N);
                    int i4 = 0;
                    while (child[r, i3] != 0 && child[r, i3] != child[r, i1]) i3 = random.Next(N);
                    for (int j = 0; j < N; ++j)
                    {
                        if (child[r, j] == child[r, i3])
                        {
                            i4 = j; break;
                        }
                    }
                    int t = child[r, i1];
                    child[r, i1] = child[r, i3];
                    child[r, i2] = child[r, i4];

                    child[r, i4] = t;
                    child[r, i3] = t;
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
