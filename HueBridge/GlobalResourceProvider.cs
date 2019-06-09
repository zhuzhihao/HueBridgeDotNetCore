using HueBridge.Utilities;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocketLite.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.IO;
using System.Composition.Convention;
using System.Runtime.Loader;
using System.Composition.Hosting;
using System.Net.Http;
using System.Composition;
using System.Composition.Hosting.Core;

namespace HueBridge
{
    public class GlobalResourceProvider : IGlobalResourceProvider, IDisposable
    {
        private IServiceProvider _serviceProvider;
        private LiteDatabase _database;
        private Authenticator _authenticator;
        private IEnumerable<ILightHandlerContract> _lighthandlers;
        private IEnumerable<ISensorHandlerContract> _sensorhandlers;
        private IOptions<AppOptions> _options;
        private CompositeInterfaceInfo _commInterface;
        private CompositionHost _lighthandlerContainer;
        private CompositionHost _sensorhandlerContainer;

        public LiteDatabase DatabaseInstance => _database;
        public Authenticator AuthenticatorInstance => _authenticator;
        public IEnumerable<ILightHandlerContract> LightHandlers
        {
            get
            {
                if (_lighthandlers == null)
                {
                    LoadLightHandlers();
                }
                return _lighthandlers;
            }
        }
        public IEnumerable<ISensorHandlerContract> SensorHandlers
        {
            get
            {
                if (_sensorhandlers == null)
                {
                    LoadSensorHandlers();
                }
                return _sensorhandlers;
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

        private void LoadLightHandlers()
        {
            var assemblies = Directory
                        .GetFiles("./", "MEF.*.dll", SearchOption.AllDirectories)
                        .Select(x => Path.Combine(Directory.GetCurrentDirectory(), x))
                        .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                        .ToList();

            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<ILightHandlerContract>()
                        .Export<ILightHandlerContract>()
                        .Shared();
            var configuration = new ContainerConfiguration()
                        .WithExport<IHttpClientFactory>(_serviceProvider.GetService<IHttpClientFactory>())
                        .WithAssemblies(assemblies, conventions);

            _lighthandlerContainer = configuration.CreateContainer();
            _lighthandlerContainer.SatisfyImports(this);
            _lighthandlers = _lighthandlerContainer.GetExports<ILightHandlerContract>();
        }

        private void LoadSensorHandlers()
        {
            var assemblies = Directory
                        .GetFiles("./", "MEF.*.dll", SearchOption.AllDirectories)
                        .Select(x => Path.Combine(Directory.GetCurrentDirectory(), x))
                        .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                        .ToList();

            var conventions = new ConventionBuilder();
            conventions.ForTypesDerivedFrom<ISensorHandlerContract>()
                        .Export<ISensorHandlerContract>()
                        .Shared();
            var configuration = new ContainerConfiguration()
                        .WithAssemblies(assemblies, conventions);

            _sensorhandlerContainer = configuration.CreateContainer();
            _sensorhandlers = _sensorhandlerContainer.GetExports<ISensorHandlerContract>();
        }

        public void Dispose()
        {
            _lighthandlerContainer?.Dispose();
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


    static class ContainerConfigurationExtensions
    {
        public static ContainerConfiguration WithExport<T>(this ContainerConfiguration configuration, T exportedInstance, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return WithExport(configuration, exportedInstance, typeof(T), contractName, metadata);
        }

        public static ContainerConfiguration WithExport(this ContainerConfiguration configuration, object exportedInstance, Type contractType, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return configuration.WithProvider(new InstanceExportDescriptorProvider(
                exportedInstance, contractType, contractName, metadata));
        }
    }

    // This one-instance-per-provider design is not efficient for more than a few instances;
    // we're just aiming to show the mechanics here.
    class InstanceExportDescriptorProvider : SinglePartExportDescriptorProvider
    {
        object _exportedInstance;

        public InstanceExportDescriptorProvider(object exportedInstance, Type contractType, string contractName, IDictionary<string, object> metadata)
            : base(contractType, contractName, metadata)
        {
            if (exportedInstance == null) throw new ArgumentNullException("exportedInstance");
            _exportedInstance = exportedInstance;
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (IsSupportedContract(contract))
                yield return new ExportDescriptorPromise(contract, _exportedInstance.ToString(), true, NoDependencies, _ =>
                    ExportDescriptor.Create((c, o) => _exportedInstance, Metadata));
        }
    }

    abstract class SinglePartExportDescriptorProvider : ExportDescriptorProvider
    {
        readonly Type _contractType;
        readonly string _contractName;
        readonly IDictionary<string, object> _metadata;

        protected SinglePartExportDescriptorProvider(Type contractType, string contractName, IDictionary<string, object> metadata)
        {
            if (contractType == null) throw new ArgumentNullException("contractType");

            _contractType = contractType;
            _contractName = contractName;
            _metadata = metadata ?? new Dictionary<string, object>();
        }

        protected bool IsSupportedContract(CompositionContract contract)
        {
            if (contract.ContractType != _contractType ||
                contract.ContractName != _contractName)
                return false;

            if (contract.MetadataConstraints != null)
            {
                var subsetOfConstraints = contract.MetadataConstraints.Where(c => _metadata.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => _metadata[c.Key]);
                var constrainedSubset = new CompositionContract(contract.ContractType, contract.ContractName,
                    subsetOfConstraints.Count == 0 ? null : subsetOfConstraints);

                if (!contract.Equals(constrainedSubset))
                    return false;
            }

            return true;
        }

        protected IDictionary<string, object> Metadata { get { return _metadata; } }
    }
}
