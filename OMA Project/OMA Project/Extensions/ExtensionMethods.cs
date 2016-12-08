using System.Collections.Generic;

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

            for (var i = source.Length; i-- > 0;)
            {
                destination[i] = new int[source[i].Length][];
                for (var j = source[i].Length; j-- > 0;)
                {
                    destination[i][j] = new int[source[i][j].Length];
                    for (var k = source[i][j].Length; k-- > 0;)
                        destination[i][j][k] = source[i][j][k];
                }
            }
            return destination;
        }

        public static List<int[]> DeepClone(this List<int[]> source)
        {
            var destination = new List<int[]>(source.Count);
            for (var i = 0; i < source.Count; ++i)
            {
                destination.Add(new int[source[i].Length]);
                for (var j = source[i].Length; j-- > 0;)
                    destination[i][j] = source[i][j];
            }
            return destination;
        }
    }
}