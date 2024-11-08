using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TournamentScheduler;
using static System.Net.Mime.MediaTypeNames;


namespace wpfStadium
{
    public partial class MainWindow : Window
    {

        private CancellationTokenSource cancellationTokenSource;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void DisplayResults(int[,] bestSolution)
        {
            listBoxResults.Items.Clear(); // Очищаем предыдущие результаты

            int R = bestSolution.GetLength(0); // Количество туров
            int N = bestSolution.GetLength(1); // Количество участников
            int numOfCourts = 0;

            // Сначала определим количество площадок
            for (int r = 0; r < R; r++)
            {
                for (int n = 0; n < N; n++)
                {
                    if (bestSolution[r, n] > numOfCourts)
                    {
                        numOfCourts = bestSolution[r, n];
                    }
                }
            }
            var strlen = 12;
            // Добавляем заголовок таблицы
            string header = "Тур \\| " + string.Join("|", Enumerable.Range(1, numOfCourts).Select(c => $" П{c} ".PadLeft(strlen)));
            listBoxResults.Items.Add(header);
            listBoxResults.Items.Add(new string('-', header.Length)); // Разделительная линия


            for (int r = 0; r < R; r++)
            {
                // Создаем строку для текущего тура
                var row = new List<string> { $"Тур {r + 1}" };

                for (int c = 1; c <= numOfCourts; c++)
                {
                    // Ищем участников для текущей площадки
                    var participants = new List<string>();
                    for (int n = 0; n < N; n++)
                    {
                        if (bestSolution[r, n] == c)
                        {
                            participants.Add($" {n + 1} ");
                        }
                    }

                    // Если нет участников, указываем, что площадка пустая
                    if (participants.Count == 0)
                    {
                        row.Add(("  _  ").PadLeft(strlen + 2, ' '));
                    }
                    else
                    {
                        row.Add(string.Join(",", participants).PadLeft(strlen));
                    }
                }

                // Добавляем строку тура в ListBox
                listBoxResults.Items.Add(string.Join("|", row));
            }
        }

        // Метод для центрирования текста с учетом паддинга
        private string CenterText(string text, int width)
        {
            if (text.Length >= width) return text;

            int padding = (width - text.Length) / 2 + 1;
            return text.PadLeft(text.Length + padding).PadRight(width);
        }




        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int N = int.Parse((NRead.Text).ToString());
            int M = int.Parse(((MRead.Text)).ToString());
            int K = int.Parse((KRead.Text).ToString());

            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    RunGeneticAlgorithm(N, M, K, cancellationTokenSource.Token);
                }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
        private void RunGeneticAlgorithm(int N, int R, int K, CancellationToken token)
        {
            Scheduler scheduler = new Scheduler(N, R, K);
            int[,] bestSolution = scheduler.GenerateSchedule(100, token, (generation, bestFitness, bestSolution) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressTextBlock.Text = $"{generation}";
                    BestMetricTextBlock.Text = $"{bestFitness}";
                    listBoxResults.Items.Clear();
                    DisplayResults(bestSolution);
                });
            });
        }


        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            listBoxResults.Items.Clear();
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Получаем ссылку на текущий TextBox
            var textBox = sender as TextBox;

            // Формируем новое значение, которое будет после ввода
            string newText = textBox.Text + e.Text;

            // Проверяем, что введенное значение является целым числом и больше 0
            if (!IsTextAllowed(newText))
            {
                e.Handled = true; // Запрещаем ввод, если значение некорректно
            }
        }

        private static bool IsTextAllowed(string text)
        {
            // Проверяем, что строка является целым числом и больше 0
            return int.TryParse(text, out int number) && number > 0;
        }

    }
}