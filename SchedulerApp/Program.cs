using TournamentScheduler;
class Program
{
    static void Main(string[] args)
    {
        int N = 5; // Количество участников (нечётное для примера)
        int R = 3;  // Количество туров
        int K = 6; // Количество площадок

        Scheduler scheduler = new Scheduler(N, R, K);
        int[,] bestSchedule = scheduler.GenerateSchedule();

        // Вывод расписания
        Console.WriteLine("\n\n\nЛучшее расписание:");
        for (int r = 0; r < R; r++)
        {
            Console.Write($"----Тур {r + 1}----\n");
            for (int n = 0; n < N; n++)
            {
                if (bestSchedule[r, n] == 0)
                {
                    Console.Write($"Участник {n + 1} отдыхает\n");
                }
                else
                {
                    Console.Write($"Участник {n + 1} на площадке {bestSchedule[r, n]}\n");
                }
            }
            Console.WriteLine();
        }
    }
}