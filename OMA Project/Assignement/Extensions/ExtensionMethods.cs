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
        public static int[] DeepClone(this int[] source)
        {
            var destination = new int[source.Length];
            Parallel.For(0, source.Length, i => destination[i] = source[i]);
            return destination;
        }

        public static List<int> DeepClone(this List<int> source)
        {
            var destination = new List<int>(source.Capacity);
            var sCount = source.Count;
            for (var i = 0; i < sCount; ++i)
                destination.Add(source[i]);
            return destination;
        }
    }
}