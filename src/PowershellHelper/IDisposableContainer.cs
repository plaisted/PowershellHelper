using System;

namespace Plaisted.PowershellHelper
{
    internal interface IDisposableContainer
    {
        void Add(IDisposable disposable);
        void Dispose();
    }
}