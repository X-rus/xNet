using System;

namespace xNet.Net
{
    /// <summary>
    /// Отмечает типы, которые представляют параметры запроса.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = false)]
    public sealed class HttpDataAttribute : Attribute
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpDataAttribute"/>.
        /// </summary>
        public HttpDataAttribute() { }
    }
}