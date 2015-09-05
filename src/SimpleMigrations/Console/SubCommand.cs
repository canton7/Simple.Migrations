using System;
using System.Collections.Generic;

namespace SimpleMigrations.Console
{
    /// <summary>
    /// Signature for the actino of a <see cref="SubCommand"/>
    /// </summary>
    /// <param name="migrator">Migrator to use to perform the action</param>
    /// <param name="args">Additional command-line arguments</param>
    public delegate void SubCommandAction(SimpleMigrator migrator, List<string> args);

    /// <summary>
    /// A SubCommand, used by the <see cref="ConsoleRunner"/> class
    /// </summary>
    public class SubCommand
    {
        /// <summary>
        /// Gets and sets the sub-command, which when matched will cause this subcommand to run
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets and sets the description of the sub-command, which is shown to the user
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets and sets the action to invoke when the user executes the sub-command
        /// </summary>
        public SubCommandAction Action { get; set; }
    }
}
