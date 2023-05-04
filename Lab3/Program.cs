using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int N = 5000; // кількість точок
            List<(double, double)> points = GeneratePoints(N);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Введіть кількість кластерів = ");
            int k = Convert.ToInt32(Console.ReadLine()); // кількість кластерів
            var clusters = KMeans(points, k);

            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            PlotPoints(clusters, projectDirectory + "\\chart1.png");

            var cMeansClusters = CMeans(points, k);
            PlotPoints(cMeansClusters, projectDirectory + "\\chart2.png");
        }
        static List<(double, double)> GeneratePoints(int N)
        {
            var random = new Random();
            var points = new List<(double, double)>(N);

            for (int i = 0; i < N; i++)
            {
                double x = random.NextDouble();
                double y = random.NextDouble();
                points.Add((x, y));
            }

            return points;
        }
        static double Distance((double, double) p1, (double, double) p2)
        {
            double dx = p1.Item1 - p2.Item1;
            double dy = p1.Item2 - p2.Item2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        static List<(double, double,int)> KMeans(List<(double, double)> points, int k)
        {
            var clusters = new List<List<(double, double)>>(k);
            var centroids = new List<(double, double)>(k);
            var random = new Random();
            int maxIterations = 1000000;
            List<(double, double, int)> pointsWithClusterIndex = new List<(double, double, int)>();

            // вибираємо випадкові центроїди
            for (int i = 0; i < k; i++)
            {
                int index = random.Next(points.Count);
                centroids.Add(points[index]);
            }

            for (int i = 0; i < maxIterations; i++)
            {
                // Для кожної точки визначаємо до якого кластера вона належить
                for (int j = 0; j < points.Count; j++)
                {
                    var point = points[j];
                    var clusterIndex = GetClusterIndex(centroids, point);
                    pointsWithClusterIndex.Add((point.Item1, point.Item2, clusterIndex));
                }

                // Для кожного кластера обчислюємо новий центр
                for (int j = 0; j < k; j++)
                {
                    var clusterPoints = pointsWithClusterIndex.Where(p => p.Item3 == j).ToList();
                    if (clusterPoints.Count > 0)
                    {
                        var centroid = ComputeCentroid(clusterPoints,j);
                        centroids[j] = centroid;
                    }
                }

                // Якщо точки більше не змінюють кластери, закінчуємо ітерації
                if (pointsWithClusterIndex.All(p => p.Item3 == GetClusterIndex(centroids, (p.Item1,p.Item2))))
                {
                    break;
                }

                // Якщо досягнуто максимальну кількість ітерацій, закінчуємо цикл
                if (i == maxIterations - 1)
                {
                    break;
                }

                // Очищуємо список точок з номером кластеру перед наступною ітерацією
                pointsWithClusterIndex.Clear();
            }
            return pointsWithClusterIndex;
        }
        public static List<(double, double, int)> CMeans(List<(double, double)> points, int k)
        {
            List<(double, double, int)> clusters = new List<(double, double, int)>();

            // Випадково ініціалізуємо центроїди кластерів
            List<(double, double)> centroids = GeneratePoints(k);

            while (true)
            {
                // Ініціалізуємо список кластерів для поточної ітерації
                List<List<(double, double)>> currClusters = new List<List<(double, double)>>();

                for (int i = 0; i < k; i++)
                {
                    currClusters.Add(new List<(double, double)>());
                }

                // Призначаємо кожну точку до найближчого центроїда
                foreach ((double, double) point in points)
                {
                    int clusterIndex = GetClusterIndex(centroids, point);
                    currClusters[clusterIndex].Add(point);
                }

                // Обчислюємо нові координати центроїдів
                List<(double, double)> newCentroids = new List<(double, double)>();

                for (int i = 0; i < k; i++)
                {
                    if (currClusters[i].Count == 0)
                    {
                        // Якщо кластер порожній, то залишаємо старі координати центроїда
                        newCentroids.Add(centroids[i]);
                    }
                    else
                    {
                        // Обчислюємо середнє арифметичне точок у кластері для нового центроїда
                        double sumX = 0;
                        double sumY = 0;

                        foreach ((double, double) point in currClusters[i])
                        {
                            sumX += point.Item1;
                            sumY += point.Item2;
                        }

                        double avgX = sumX / currClusters[i].Count;
                        double avgY = sumY / currClusters[i].Count;

                        newCentroids.Add((avgX, avgY));
                    }
                }

                // Якщо центроїди збігаються з попередньою ітерацією, то завершуємо процес
                if (newCentroids.SequenceEqual(centroids))
                {
                    break;
                }

                centroids = newCentroids;
            }

            // Призначаємо кожну точку до підходящого кластеру
            foreach ((double, double) point in points)
            {
                int clusterIndex = GetClusterIndex(centroids, point);
                clusters.Add((point.Item1, point.Item2, clusterIndex));
            }

            return clusters;
        }
        public static int GetClusterIndex(List<(double, double)> centroids, (double, double) point)
        {
            // Знаходимо відстані від точки до центрів кожного кластера
            var distances = centroids.Select(c => Distance(point, c)).ToList();

            // Знаходимо індекс кластера з найближчим центром до точки
            var clusterIndex = distances.IndexOf(distances.Min());

            return clusterIndex;
        }
        private static (double, double) ComputeCentroid(List<(double, double, int)> clusters, int clusterID)
        {
            var pointsInCluster = clusters.Where(c => c.Item3 == clusterID).ToList();
            double sumX = pointsInCluster.Sum(p => p.Item1);
            double sumY = pointsInCluster.Sum(p => p.Item2);
            double centroidX = sumX / pointsInCluster.Count;
            double centroidY = sumY / pointsInCluster.Count;
            return (centroidX, centroidY);
        }

        // Функція для побудови графіку точок з розділенням за кольорами
        public static void PlotPoints(List<(double, double, int)> points, string filePath)
        {
            // Створюємо новий об'єкт графіку
            var chart = new Chart();

            // Створюємо новий об'єкт для координатної площини
            var chartArea = new ChartArea("ChartArea");
            chart.ChartAreas.Add(chartArea);

            // Додаємо точки на графік
            var series = new Series("Points");
            series.ChartType = SeriesChartType.Point;
            series.MarkerStyle = MarkerStyle.Circle;
            foreach (var point in points)
            {
                var dataPoint = new DataPoint(point.Item1, point.Item2);
                dataPoint.Color = point.Item3 switch
                {
                    0 => Color.Red,
                    1 => Color.Blue,
                    2 => Color.Green,
                    3 => Color.Purple,
                    4 => Color.Yellow,
                    5 => Color.Orange,
                    6 => Color.Gray,
                    7 => Color.Pink,
                    8 => Color.Coral,
                    9 => Color.Turquoise,
                    10 => Color.DarkSalmon,
                    _ => Color.Black // Якщо кількість кластерів більша 10
                };
                series.Points.Add(dataPoint);
            }
            chart.Series.Add(series);

            // Налаштовуємо відображення графіку
            chart.Size = new Size(1000, 1000);
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = 1;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = 1;

            // Відображаємо графік на у зображення
            chart.SaveImage(filePath, ChartImageFormat.Png);

        }
    }
}
