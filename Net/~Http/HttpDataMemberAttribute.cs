using System;

namespace xNet.Net
{
    /// <summary>
    /// Отмечает поля и свойства, которые представляют параметр запроса.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        Inherited = false)]
    public sealed class HttpDataMemberAttribute : Attribute
    {
        /// <summary>
        /// Возвращает имя параметра, указываемое в запросе.
        /// </summary>
        public string Name { get; private set; }


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpDataMemberAttribute"/>.
        /// </summary>
        public HttpDataMemberAttribute() { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HttpDataMemberAttribute"/>.
        /// </summary>
        /// <param name="name">Имя параметра, указываемое в запросе.</param>
        public HttpDataMemberAttribute(string name)
        {
            Name = name;
        }

        #endregion
    }
}