using System;

namespace SimpleMigrations.Console
{
    /// <summary>
    /// Throw this from a <see cref="SubCommand"/> to show help to the user
    /// </summary>
    public class HelpNeededException : Exception
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="HelpNeededException"/> class
        /// </summary>
        public HelpNeededException()
        {
        }
    }
}