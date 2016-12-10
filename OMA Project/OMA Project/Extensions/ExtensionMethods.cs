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
                        // Buffer.BlockCopy instead of Array.Copy as it looks at data as a byte stream
                        // ignoring types and occasional exception. Plus it relies on a C/C++
                        // underlying implementation that makes it run way faster.
                        // 4 * length as each int is 4 byte long
                        Buffer.BlockCopy(source[i][j], 0, destination[i][j], 0, 4 * source[i][j].Length);
                    }
                });
            return destination;
        }

        public static List<int> DeepClone(this List<int> source)
        {
            var destination = new List<int>(source.Capacity);
            var sCount = source.Count;
            for (int i = 0; i < sCount; ++i)
            {
                destination.Add(source[i]);
            }
            return destination;
        }
    }
}