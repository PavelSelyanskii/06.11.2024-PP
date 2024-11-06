using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace _06._11._2024_PP
{
    public partial class MainWindow : Window
    {
        private List<(double, double)> data;
        private GradientDescent gradientDescent;
        private GeneticAlgorithm geneticAlgorithm;
        private DispatcherTimer optimizationTimer;
        private int iteration;

        public MainWindow()
        {
            InitializeComponent();
            gradientDescent = new GradientDescent();
            geneticAlgorithm = new GeneticAlgorithm();
            optimizationTimer = new DispatcherTimer();
            optimizationTimer.Interval = TimeSpan.FromMilliseconds(100);
            optimizationTimer.Tick += OptimizationTimer_Tick;
        }

        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            // Открытие диалога для выбора файла
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Загрузка данных из CSV
                var loader = new CsvDataLoader();
                data = loader.LoadData(openFileDialog.FileName);
                var plotter = new Plotter();
                var plotModel = plotter.CreateScatterPlot(data);

                plotView.Model = plotModel;
                txtResults.Text = "Данные загружены. Готовы к оптимизации.";
            }
        }

        private void btnStartOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Сначала загрузите данные.");
                return;
            }

            // Начало оптимизации с использованием градиентного спуска
            progressBar.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            iteration = 0;

            // Параметры алгоритма (для примера)
            double initialGuess = 0;
            double learningRate = 0.01;
            int maxIterations = 100;

            // Запуск в отдельном потоке
            Task.Run(() => StartOptimization(learningRate, initialGuess, maxIterations));
        }

        private async Task StartOptimization(double learningRate, double initialGuess, int maxIterations)
        {
            // Оптимизация с использованием градиентного спуска
            double result = gradientDescent.Learn(
                objectiveFunction: x => Math.Pow(x - 3, 2), // Пример функции: f(x) = (x - 3)^2
                derivative: x => 2 * (x - 3),
                initialGuess,
                learningRate,
                maxIterations
            );

            // Обновляем UI после завершения
            Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtResults.Text = $"Оптимизация завершена. Лучшее значение: {result:F4}";
            });
        }

        private void OptimizationTimer_Tick(object sender, EventArgs e)
        {
            iteration++;
            progressBar.Value = (double)iteration / 100 * 100; // Псевдо-обновление
            if (iteration >= 100)
            {
                optimizationTimer.Stop();
            }
        }
    }

    // Класс для загрузки данных из CSV
    public class CsvDataLoader
    {
        public List<(double, double)> LoadData(string filePath)
        {
            var data = new List<(double, double)>();
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    var values = line.Split(',');
                    if (values.Length == 2 &&
                        double.TryParse(values[0], out var x) &&
                        double.TryParse(values[1], out var y))
                    {
                        data.Add((x, y));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}");
            }
            return data;
        }
    }

    // Класс для визуализации данных
    public class Plotter
    {
        public PlotModel CreateScatterPlot(List<(double, double)> data)
        {
            var plotModel = new PlotModel { Title = "График данных" };
            var scatterSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                MarkerFill = OxyColor.FromRgb(0, 0, 255)
            };

            foreach (var point in data)
            {
                scatterSeries.Points.Add(new ScatterPoint(point.Item1, point.Item2));
            }

            plotModel.Series.Add(scatterSeries);
            return plotModel;
        }
    }

    // Алгоритм градиентного спуска
    public class GradientDescent
    {
        public double Learn(Func<double, double> objectiveFunction, Func<double, double> derivative, double initialGuess, double learningRate, int maxIterations)
        {
            double currentValue = initialGuess;
            for (int i = 0; i < maxIterations; i++)
            {
                var gradient = derivative(currentValue);
                currentValue = currentValue - learningRate * gradient;
            }
            return currentValue;
        }
    }

    // Генетический алгоритм
    public class GeneticAlgorithm
    {
        public double Optimize(Func<double, double> objectiveFunction, double lowerBound, double upperBound, int populationSize, int generations, double mutationRate)
        {
            var random = new Random();
            List<double> population = new List<double>();

            // Инициализация случайной популяции
            for (int i = 0; i < populationSize; i++)
            {
                population.Add(random.NextDouble() * (upperBound - lowerBound) + lowerBound);
            }

            for (int generation = 0; generation < generations; generation++)
            {
                // Этап селекции, скрещивания и мутации
                var fitness = population.Select(individual => objectiveFunction(individual)).ToList();
                var selected = population.Zip(fitness, (x, f) => new { x, f })
                                         .OrderBy(pair => pair.f)
                                         .Take(populationSize / 2)
                                         .Select(pair => pair.x)
                                         .ToList();

                population.Clear();
                for (int i = 0; i < populationSize / 2; i++)
                {
                    var parent1 = selected[random.Next(selected.Count)];
                    var parent2 = selected[random.Next(selected.Count)];
                    var child = (parent1 + parent2) / 2 + (random.NextDouble() - 0.5) * mutationRate;
                    population.Add(child);
                }
            }

            return population.Min(); // Возвращаем лучший результат
        }
    }
}
