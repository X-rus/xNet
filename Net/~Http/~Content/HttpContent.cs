using System.IO;

namespace xNet.Net
{
    /// <summary>
    /// Представляет контент.
    /// </summary>
    public abstract class HttpContent
    {
        /// <summary>MIME-тип контента.</summary>
        protected string _contentType = string.Empty;


        /// <summary>
        /// Возвращает или задаёт MIME-тип контента.
        /// </summary>
        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value ?? string.Empty;
            }
        }


        #region Методы (открытые)

        /// <summary>
        /// Подсчитывает и возвращает длину контента в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        public abstract long CalculateContentLength();

        /// <summary>
        /// Записывает данные контента в поток.
        /// </summary>
        /// <param name="stream">Поток, куда будут записаны данные контента.</param>
        public abstract void WriteTo(Stream stream);

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