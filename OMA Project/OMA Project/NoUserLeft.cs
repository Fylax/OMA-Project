using System;

namespace OMA_Project
{
    /// <summary>
    ///     Thrown when there are no user left in current computation
    ///     (most likely is a ST-like instance)
    /// </summary>
    /// <seealso cref="System.Exception" />
    internal class NoUserLeft : Exception
    {
    }
}