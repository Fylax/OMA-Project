using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OMA_Project.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Crea una copia profonda di un array di interi a tre dimensioni
        /// </summary>
        /// <param name="source">Array sorgente</param>
        /// <returns>Copia profonda</returns>
        public static int[][][] DeepClone(this int[][][] source)
        {
            var destination = new int[source.Length][][];
            Parallel.For(0, source.Length,
                i =>
                {
                    destination[i] = new int[source[i].Length][];
                    for (var j = source[i].Length; j-- > 0;)
                    {
                        destination[i][j] = new int[source[i][j].Length];
                        source[i][j].CopyTo(destination[i][j], 0);
                    }
                });
            return destination;
        }

        public static List<int[]> DeepClone(this List<int[]> source)
        {
            var destination = new List<int[]>(source.Count);
            Parallel.For(0, source.Count, i =>
            {
                int[] d = new int[source[i].Length];
                source[i].CopyTo(d, 0);
                lock (destination)
                {
                    destination.Add(d);
                }
            });
            return destination;
        }
    }
}