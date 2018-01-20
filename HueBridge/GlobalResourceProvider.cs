using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace HueBridge
{
    public class GlobalResourceProvider : IGlobalResourceProvider
    {
        public LiteDatabase DatabaseInstance => _database;

        private LiteDatabase _database;

        public GlobalResourceProvider()
        {
            // create db instance
            _database = new LiteDatabase("Filename=BridgeData.db; Mode=Exclusive");
        }
    }
}
