using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbt.notification.worker.Database
{
    public class DbDataEntity
    {
        public ParameterDirection direction { get; set; }
        public DbType dbType { get; set; }
        public string parameterName { get; set; }
        public object value;
    }
    public class DbEntity
    {

    }
}
