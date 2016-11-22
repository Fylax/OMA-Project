using System;
using System.Collections.Generic;

namespace OMA_Project.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Crea una copia profonda di un array di interi a tre dimensioni
        /// </summary>
        /// <param name="source">Array sorgente</param>
        /// <returns>Copia profonda</returns>
        public static int[][][] DeepClone(this int[][][] source)
        {
            int[][][] destination = new int[source.Length][][];

            for (int i = 0; i < source.Length; ++i)
            {
                destination[i] = new int[source[i].Length][];
                for (int j = 0; j < source[i].Length; ++j)
                {
                    destination[i][j] = (int[])source[i][j].Clone();
                }
            }
            return destination;
        }

        public static SortedSet<int[]> DeepClone(this SortedSet<int[]> source)
        {
            SortedSet<int[]> destination = new SortedSet<int[]>(source.Comparer);
            foreach (int[] elem in source)
            {
                destination.Add((int[])elem.Clone());
            }
            return destination;
        }
    }
}
