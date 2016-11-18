using System;
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
                for (int j = 0; j < source[i].Length; ++i)
                {
                    source.CopyTo(destination[i][j], 0);
                }
            }

            return destination;
        }
    }
}
