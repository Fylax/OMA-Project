using System;

namespace OMA_Project.Extensions
{
    /// <summary>
    ///     Exposes effcient deep cloning methods.
    /// </summary>
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
    }
}