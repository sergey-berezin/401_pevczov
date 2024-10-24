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


namespace wpfStadium
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


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

            for (int r = 0; r < R; r++)
            {
                listBoxResults.Items.Add($"-------------------------------------------- Тур {r + 1} --------------------------------------------");

                for (int n = 0; n < N; n++)
                {
                    if (bestSolution[r, n] == 0)
                    {
                        listBoxResults.Items.Add($"Участник {n + 1} отдыхает");
                    }
                    else
                    {
                        listBoxResults.Items.Add($"Участник {n + 1} на площадке {bestSolution[r, n]}");
                    }
                }

                listBoxResults.Items.Add(""); // Добавляем пустую строку для разделения туров
            }
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
            //int[,] bestSchedule = scheduler.GenerateSchedule();
            //var geneticAlgorithm = new GeneticAlgorithm();
            // Инициализация популяции
            //geneticAlgorithm.InitializePopulation(100, side1, side2, side3);
            // Ограничение на число поколений
            int[,] bestSolution = scheduler.GenerateSchedule(10000, token, (generation, bestFitness, bestSolution) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressTextBlock.Text = $"{generation}";
                    BestMetricTextBlock.Text = $"{bestFitness}";
                    //listBoxResults.Items.Add($"-------------------------------------------- Тур {1} --------------------------------------------");
                    /*listBoxResults.Items.Clear(); // Очищаем предыдущие результаты

                    int R = bestSolution.GetLength(0); // Количество туров
                    int N = bestSolution.GetLength(1); // Количество участников

                    for (int r = 0; r < R; r++)
                    {
                        listBoxResults.Items.Add($"-------------------------------------------- Тур {r + 1} --------------------------------------------");

                        for (int n = 0; n < N; n++)
                        {
                            if (bestSolution[r, n] == 0)
                            {
                                listBoxResults.Items.Add($"Участник {n + 1} отдыхает");
                            }
                            else
                            {
                                listBoxResults.Items.Add($"Участник {n + 1} на площадке {bestSolution[r, n]}");
                            }
                        }

                        listBoxResults.Items.Add(""); // Добавляем пустую строку для разделения туров
                    } */
                    listBoxResults.Items.Clear();
                    DisplayResults(bestSolution);
                });
            });
        }
        

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
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