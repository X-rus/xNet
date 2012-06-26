using System;

namespace xNet.Threading
{
    /// <summary>
    /// Исключение, которое выбрасывается, в случае отмены выполнения асинхронной операции.
    /// </summary>
    public sealed class MultiThreadingCanceledException : Exception
    {
        internal MultiThreadingCanceledException() { }
    }
}