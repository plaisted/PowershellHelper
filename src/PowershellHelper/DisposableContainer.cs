using System;
using System.Collections.Generic;
using System.Text;

namespace Plaisted.PowershellHelper
{
    internal class DisposableContainer : IDisposable, IDisposableContainer
    {
        private List<IDisposable> disposables;

        public DisposableContainer()
        {
            disposables = new List<IDisposable>();
        }
        public void Add(IDisposable disposable)
        {
            disposables.Add(disposable);
        }
        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                finally
                {
                    
                }
            }
        }
    }
}
