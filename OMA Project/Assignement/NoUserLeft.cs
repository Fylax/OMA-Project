using System;

namespace OMA_Project
{
    /// <summary>
    ///     Thrown when there are no user left in current computation
    ///     (most likely is a ST-like instance)
    /// </summary>
    internal class NoUserLeft : Exception
    {
    }
}