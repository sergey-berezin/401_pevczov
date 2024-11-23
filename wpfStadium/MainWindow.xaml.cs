using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;


namespace wpfStadium
{

    public class Population
    {
        public int Id { get; set; } // Идентификатор популяции
        public int Generation { get; set; } // Номер поколения
        public ICollection<Individual> Individuals { get; set; } // Связь с особями
    }

    public class Individual
    {
        public int Id { get; set; } // Идентификатор особи
        public int PopulationId { get; set; } // Внешний ключ на популяцию
        public Population Population { get; set; } 
        public ICollection<Gene> Genes { get; set; } 
    }

    public class Gene
    {
        public int Id { get; set; } // Идентификатор
        public int IndividualId { get; set; } // Внешний ключ на особь
        public Individual Individual { get; set; } // Навигационное свойство
        public int Row { get; set; } // Индекс строки
        public int Column { get; set; } // Индекс столбца
        public int Value { get; set; } // Значение гена
    }

    public class GeneticAlgorithmContext : DbContext
    {
        public DbSet<Population> Populations { get; set; }
        public DbSet<Individual> Individuals { get; set; }
        public DbSet<Gene> Genes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=genetic_algorithm.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Population>()
                .HasMany(p => p.Individuals)
                .WithOne(i => i.Population)
                .HasForeignKey(i => i.PopulationId);

            modelBuilder.Entity<Individual>()
                .HasMany(i => i.Genes)
                .WithOne(g => g.Individual)
                .HasForeignKey(g => g.IndividualId);
        }
    }

    public partial class MainWindow : Window
    {

        private CancellationTokenSource cancellationTokenSource;
        private int generation;
        private int[][,] population;
        public MainWindow()
        {
            InitializeComponent();
            using (var context = new GeneticAlgorithmContext())
            {
                context.Database.EnsureCreated();
            }
            LoadSavedPopulations();
        }

        private void LoadSavedPopulations()
        {
            using (var context = new GeneticAlgorithmContext())
            {
         
                var populations = context.Populations.ToList();

                
                SaveComboBox.ItemsSource = populations;
                SaveComboBox.DisplayMemberPath = "Generation"; // Отображаем поколение
                SaveComboBox.SelectedValuePath = "PopulationId"; // Используем ID как значение
            }
        }

        private void SaveComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SaveComboBox.SelectedItem is Population selectedPopulation)
            {
                LoadPopulation(selectedPopulation);
            }
        }

        private void LoadPopulation(Population population)
        {
            using (var context = new GeneticAlgorithmContext())
            {
                var loadedPopulation = context.Populations
                    .Where(p => p.Id == population.Id)
                    .Select(p => new
                    {
                        p.Generation,
                        Individuals = p.Individuals.Select(i => i.Genes.ToList())
                    })
                    .FirstOrDefault();

                if (loadedPopulation != null)
                {
                    // Установка generation
                    generation = loadedPopulation.Generation;

                    // Преобразование Individuals в int[][,]
                    this.population = loadedPopulation.Individuals
                        .Select(genes => ConvertGenesToArray(genes))
                        .ToArray();

                    MessageBox.Show($"Сохранение {generation} загружено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить сохранение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // Преобразование списка генов в двумерный массив
        private int[,] ConvertGenesToArray(List<Gene> genes)
        {
            int rows = genes.Max(g => g.Row) + 1;
            int cols = genes.Max(g => g.Column) + 1;

            int[,] array = new int[rows, cols];
            foreach (var gene in genes)
            {
                array[gene.Row, gene.Column] = gene.Value;
            }

            return array;
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
            int[,] bestSolution = scheduler.GenerateSchedule(100, token, (generation_get, bestFitness, bestSolution, population_get) =>
            {
                Dispatcher.Invoke(() =>
                {
                    generation = generation_get;
                    population = population_get;
                    ProgressTextBlock.Text = $"{generation_get}";
                    BestMetricTextBlock.Text = $"{bestFitness}";
                    listBoxResults.Items.Clear();
                    DisplayResults(bestSolution);

                    
                    //SavePopulation(generation, population);
                });
            });
        }

        public void SavePopulation(int generation, int[][,] population)
        {
            using (var context = new GeneticAlgorithmContext())
            {
                // Создаем запись для текущей популяции
                var dbPopulation = new Population
                {
                    Generation = generation,
                    Individuals = new List<Individual>()
                };

                // Добавляем особей в популяцию
                for (int individualIndex = 0; individualIndex < population.Length; individualIndex++)
                {
                    var individual = population[individualIndex];
                    var dbIndividual = new Individual
                    {
                        Genes = new List<Gene>()
                    };

                    // Добавляем гены для текущей особи
                    for (int row = 0; row < individual.GetLength(0); row++)
                    {
                        for (int col = 0; col < individual.GetLength(1); col++)
                        {
                            dbIndividual.Genes.Add(new Gene
                            {
                                Row = row,
                                Column = col,
                                Value = individual[row, col]
                            });
                        }
                    }

                    dbPopulation.Individuals.Add(dbIndividual);
                }

                // Сохраняем в базу данных
                context.Populations.Add(dbPopulation);
                context.SaveChanges();
            }
            LoadSavedPopulations();
        }

        private void ClearDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Подтверждение действия
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить все сохранённые популяции?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                using (var context = new GeneticAlgorithmContext())
                {
                    try
                    {
                        // Удаляем все популяции
                        context.Populations.RemoveRange(context.Populations);

                        // Сохраняем изменения
                        context.SaveChanges();

                        // Обновляем ComboBox
                        LoadSavedPopulations();

                        MessageBox.Show("Все сохранения успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при очистке базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            LoadSavedPopulations();
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                // Вызов метода сохранения
                SavePopulation(generation, population);

                MessageBox.Show("Популяция успешно сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;

            string newText = textBox.Text + e.Text;

           
            if (!IsTextAllowed(newText))
            {
                e.Handled = true; 
            }
        }

        private static bool IsTextAllowed(string text)
        {
            // Проверяем, что строка является целым числом и больше 0
            return int.TryParse(text, out int number) && number > 0;
        }

    }
}