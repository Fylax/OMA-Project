using System;
using System.Collections.Generic;

namespace OMA_Project.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Creates a Deep Clone of an array of integers
        /// </summary>
        /// <param name="source">Source array</param>
        /// <returns>Deep copy</returns>
        public static int[] DeepClone(this int[] source)
        {
            var destination = new int[source.Length];
            Buffer.BlockCopy(source, 0, destination, 0, source.Length * 4);
            return destination;
        }

        /// <summary>
        ///     Creates a Deep Clone of a list of integers
        /// </summary>
        /// <param name="source">Source list</param>
        /// <returns>Deep copy</returns>
        public static List<int> DeepClone(this List<int> source)
        {
            var destination = new List<int>(source.Capacity);
            destination.AddRange(source);
            return destination;
        }
    }
}