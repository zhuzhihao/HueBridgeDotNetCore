using HueBridge.Utilities;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocketLite.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace HueBridge
{
    public class GlobalResourceProvider : IGlobalResourceProvider
    {
        private IServiceProvider _serviceProvider;
        private LiteDatabase _database;
        private Authenticator _authenticator;
        private List<IScanner> _scanners;
        private IOptions<AppOptions> _options;
        private CompositeInterfaceInfo _commInterface;

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
        public CompositeInterfaceInfo CommInterface
        {
            get
            {
                if (_commInterface == null)
                {
                    _commInterface = new CompositeInterfaceInfo(_options.Value.NetworkInterface);
                }
                return _commInterface;
            }
        }


        public GlobalResourceProvider(
            IServiceProvider serviceProvider,
            IOptions<AppOptions> optionsAccessor)
        {
            // create db instance
            _database = new LiteDatabase("Filename=BridgeData.db; Mode=Exclusive");
            _authenticator = new Authenticator(_database);
            _serviceProvider = serviceProvider;
            _options = optionsAccessor;
        }
    }

    public class CompositeInterfaceInfo
    {
        private CommunicationsInterface slInfo;
        private NetworkInterface nInfo;
        public CommunicationsInterface SocketLiteInfo { get => slInfo; }
        public NetworkInterface NativeInfo { get => nInfo; }

        public CompositeInterfaceInfo(string IP)
        {
            var allInterfaces = (new CommunicationsInterface()).GetAllInterfaces();
            slInfo = (CommunicationsInterface)allInterfaces.FirstOrDefault(x => x.IpAddress == IP);
            if (slInfo == null)
            {
                // in case we cannot find the interface that matches appsettings.json
                slInfo = (CommunicationsInterface)allInterfaces.FirstOrDefault(x => !x.IsLoopback);
            }

            // find native network interface information
            var allInterfacesNative = NetworkInterface.GetAllNetworkInterfaces();
            nInfo = allInterfacesNative.FirstOrDefault(x => x.Id == slInfo.NativeInterfaceId);
        }
    }
}
