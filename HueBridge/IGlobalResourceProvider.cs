using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace HueBridge
{
    public interface IGlobalResourceProvider
    {
        LiteDatabase DatabaseInstance { get; }
    }
}
