using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using HueBridge.Models;

/// <summary>
/// The engine does the following work:
/// 1. Periodically get all lights' latest state
/// 2. Process rules
/// 3. Process schedules
/// 4. Send actions to lights (e.g. turn lights on command from Hue app)
/// </summary>

namespace HueBridge
{
    public class HueBridgeEngine : IHostedService, IDisposable
    {
        private static List<IDisposable> tasks = new List<IDisposable>();
        private IGlobalResourceProvider grp;

        public HueBridgeEngine(IGlobalResourceProvider grp)
        {
            this.grp = grp;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            tasks.Add(Observable.Interval(TimeSpan.FromSeconds(10))
                                .StartWith(0)
                                .Subscribe(x => SyncLights()));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var t in tasks)
            {
                t?.Dispose();
            }
            return Task.CompletedTask;
        }

        private void SyncLights()
        {
            var lights = grp.DatabaseInstance.GetCollection<Light>("lights");
            var tasks = lights.FindAll()
                              .AsParallel()
                              .Select(l =>
                              {
                                  var handler = grp.LightHandlers.Where(x => x.SupportedModels.Contains(l.ModelId)).FirstOrDefault();
                                  if (handler != null)
                                  {
                                      return handler.GetLightState(l);
                                  }
                                  else
                                  {
                                      Console.WriteLine($"Cannot find handler for {l.ModelId}");
                                      return null;
                                  }
                              })
                              .Where(x => x != null)
                              .ToArray();
            Task.WaitAll(tasks);
            var updatedLights = tasks.Select(x => x.Result);
            foreach (var l in lights.FindAll())
            {
                var updatedLight = updatedLights.FirstOrDefault(x => x.UniqueId == l.UniqueId);
                if (updatedLight != null)
                {
                    l.State = updatedLight.State;
                    lights.Update(l);
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HueBridgeEngine() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
