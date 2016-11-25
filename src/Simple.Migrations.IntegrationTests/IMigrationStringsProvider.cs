using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Migrations.IntegrationTests
{
    public interface IMigrationStringsProvider
    {
        string CreateUsersTableDown { get; }
        string CreateUsersTableUp { get; }
    }
}
