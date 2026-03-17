using OsterhasenProgramm.Models;

namespace OsterhasenProgramm.Helpers
{
    public static class RouteHelper
    {
        private static double Distance(double lon1, double lat1, double lon2, double lat2)
        {
            double dLon = lon1 - lon2;
            double dLat = lat1 - lat2;
            return Math.Sqrt(dLon * dLon + dLat * dLat);
        }

        /// <summary>
        /// Berechnet die kürzeste Route für einen Helfer ausgehend von einem Startpunkt
        /// mit dem Nearest-Neighbor-Algorithmus (Greedy TSP).
        /// </summary>
        public static List<Person> NearestNeighbor(
            List<Person> persons,
            double startLon,
            double startLat)
        {
            var route = new List<Person>();
            var remaining = persons.ToList();

            double curLon = startLon;
            double curLat = startLat;

            while (remaining.Count > 0)
            {
                // Nächste unbesuchte Person finden
                var nearest = remaining
                    .OrderBy(p => Distance(curLon, curLat, p.Longitude, p.Latitude))
                    .First();

                route.Add(nearest);
                remaining.Remove(nearest);
                curLon = nearest.Longitude;
                curLat = nearest.Latitude;
            }

            return route;
        }
    }
}
