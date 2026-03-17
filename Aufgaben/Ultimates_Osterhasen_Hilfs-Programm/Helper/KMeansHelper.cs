using OsterhasenProgramm.Models;

namespace OsterhasenProgramm.Helpers
{
    public static class KMeansHelper
    {
        private static double Distance(double lon1, double lat1, double lon2, double lat2)
        {
            double dLon = lon1 - lon2;
            double dLat = lat1 - lat2;
            return Math.Sqrt(dLon * dLon + dLat * dLat);
        }

        /// <summary>
        /// Führt K-Means Clustering durch und gibt für jede Person den Index des Clusters zurück.
        /// </summary>
        public static int[] Cluster(List<Person> persons, int k)
        {
            if (persons.Count == 0) return Array.Empty<int>();
            k = Math.Min(k, persons.Count);

            var random = new Random(42);
            // Zufällige Startpunkte (Centroids) aus den Personen wählen
            var centroidIndices = Enumerable.Range(0, persons.Count)
                .OrderBy(_ => random.Next())
                .Take(k)
                .ToList();

            var centroids = centroidIndices
                .Select(i => (Lon: persons[i].Longitude, Lat: persons[i].Latitude))
                .ToList();

            int[] assignments = new int[persons.Count];
            bool changed = true;
            int maxIterations = 100;

            while (changed && maxIterations-- > 0)
            {
                changed = false;

                // Schritt 1: Jede Person dem nächsten Centroid zuweisen
                for (int i = 0; i < persons.Count; i++)
                {
                    int nearest = 0;
                    double minDist = double.MaxValue;

                    for (int j = 0; j < k; j++)
                    {
                        double dist = Distance(
                            persons[i].Longitude, persons[i].Latitude,
                            centroids[j].Lon, centroids[j].Lat);

                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearest = j;
                        }
                    }

                    if (assignments[i] != nearest)
                    {
                        assignments[i] = nearest;
                        changed = true;
                    }
                }

                // Schritt 2: Centroids neu berechnen (Mittelpunkt jeder Gruppe)
                for (int j = 0; j < k; j++)
                {
                    var group = persons
                        .Where((_, i) => assignments[i] == j)
                        .ToList();

                    if (group.Count > 0)
                    {
                        centroids[j] = (
                            Lon: group.Average(p => p.Longitude),
                            Lat: group.Average(p => p.Latitude)
                        );
                    }
                }
            }

            return assignments;
        }
    }
}
