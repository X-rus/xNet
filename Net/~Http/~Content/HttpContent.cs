using System;

namespace xNet.Net
{
    /// <summary>
    /// Представляет контент, который отправляется HTTP-серверу.
    /// </summary>
    /// <remarks>Вы можете реализовать свой класс на основе этого и передать объект этого класса в качестве параметра методу <see cref="HttpRequest.Raw"/>.</remarks>
    public abstract class HttpContent : IDisposable
    {
        #region Поля (защищённые)

        protected HttpRequest _request;
        protected Action<byte[], int> _writeBytesCallback; 

        #endregion


        #region Методы (открытые)

        /// <summary>
        /// Вызывается перед отправкой данных.
        /// </summary>
        /// <param name="request">Запрос, который отсылает текущий контент.</param>
        /// <param name="writeBytesCallback">Метод обратного вызова для записи байтов контента в поток.</param>
        public virtual void Init(HttpRequest request, Action<byte[], int> writeBytesCallback)
        {
            _request = request;
            _writeBytesCallback = writeBytesCallback;
        }

        /// <summary>
        /// Возвращает MIME-тип контента.
        /// </summary>
        /// <returns>MIME-тип контента.</returns>
        public virtual string GetType()
        {
            return "";
        }

        /// <summary>
        /// Возвращает длину контента в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        public virtual long GetLength()
        {
            return 0;
        }

        /// <summary>
        /// Вызывается, когда требуется отправить данные.
        /// </summary>
        public virtual void Send() { }

        /// <summary>
        /// Освобождает все ресурсы, используемые текущим экземпляром класса <see cref="HttpContent"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion


        /// <summary>
        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing) { }
    }
}