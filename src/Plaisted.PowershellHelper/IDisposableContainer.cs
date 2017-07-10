using System;

namespace Plaisted.PowershellHelper
{
    public interface IDisposableContainer
    {
        void Add(IDisposable disposable);
        void Dispose();
    }
}