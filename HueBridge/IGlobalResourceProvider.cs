using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using HueBridge.Utilities;

namespace HueBridge
{
    public interface IGlobalResourceProvider
    {
        LiteDatabase DatabaseInstance { get; }
        Authenticator AuthenticatorInstance { get; }
        IEnumerable<ILightHandlerContract> LightHandlers { get; }
        IEnumerable<ISensorHandlerContract> SensorHandlers { get; }
        CompositeInterfaceInfo CommInterface { get; }
    }
}
