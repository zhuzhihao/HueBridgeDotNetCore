using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Utilities
{
    public enum ScannerState
    {
        IDLE,
        SCANNING,
        STOPPING
    }

    public interface IScanner
    {
        void Begin();
        void Stop();
        ScannerState State { get; }
    }
}
