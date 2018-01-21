using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HueBridge.Utilities;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace HueBridge
{
    public class GlobalResourceProvider : IGlobalResourceProvider
    {
        private IServiceProvider _serviceProvider;
        private LiteDatabase _database;
        private Authenticator _authenticator;
        private List<IScanner> _scanners;

        public LiteDatabase DatabaseInstance => _database;
        public Authenticator AuthenticatorInstance => _authenticator;
        public IEnumerable<IScanner> ScannerInstances
        {
            get
            {
                if (_scanners == null)
                {
                    _scanners = _serviceProvider.GetServices<IScanner>().ToList();
                }
                return _scanners;
            }
        }

        public GlobalResourceProvider(
            IServiceProvider serviceProvider)
        {
            // create db instance
            _database = new LiteDatabase("Filename=BridgeData.db; Mode=Exclusive");
            _authenticator = new Authenticator(_database);
            _serviceProvider = serviceProvider;
            //_scanners = _serviceProvider.GetServices<IScanner>().ToList(); // causes stackoverflow
        }
    }
}
