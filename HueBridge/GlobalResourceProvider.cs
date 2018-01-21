using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HueBridge.Utilities;
using LiteDB;

namespace HueBridge
{
    public class GlobalResourceProvider : IGlobalResourceProvider
    {
        private LiteDatabase _database;
        private Authenticator _authenticator;
            
        public LiteDatabase DatabaseInstance => _database;
        public Authenticator AuthenticatorInstance => _authenticator;

        public GlobalResourceProvider()
        {
            // create db instance
            _database = new LiteDatabase("Filename=BridgeData.db; Mode=Exclusive");
            _authenticator = new Authenticator(_database);
        }
    }
}
