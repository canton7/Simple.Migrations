using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace SQLiteNet
{
    public class VersionInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public long Version { get; set; }

        public DateTime AppliedOn { get; set; }

        public string Description { get; set; }
    }
}
