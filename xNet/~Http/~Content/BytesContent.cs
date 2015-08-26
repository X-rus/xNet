using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// Представляет тело запроса в виде байтов.
    /// </summary>
    public class BytesContent : HttpContent
    {
        #region Поля (защищённые)

        /// <summary>Содержимое тела запроса.</summary>
        protected byte[] _content;
        /// <summary>Смещение в байтах содержимого тела запроса.</summary>
        protected int _offset;
        /// <summary>Число отправляемых байтов содержимого.</summary>
        protected int _count;

        #endregion


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">Содержимое тела запроса.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="content"/> равно <see langword="null"/>.</exception>
        /// <remarks>По умолчанию используется тип контента - 'application/octet-stream'.</remarks>
        public BytesContent(byte[] content)
            : this(content, 0, content.Length) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">Содержимое тела запроса.</param>
        /// <param name="offset">Смещение в байтах для контента.</param>
        /// <param name="count">Число байтов отправляемых из контента.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="content"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Значение параметра <paramref name="offset"/> меньше 0.
        /// -или-
        /// Значение параметра <paramref name="offset"/> больше длины содержимого.
        /// -или-
        /// Значение параметра <paramref name="count"/> меньше 0.
        /// -или-
        /// Значение параметра <paramref name="count"/> больше (длина содержимого - смещение).</exception>
        /// <remarks>По умолчанию используется тип контента - 'application/octet-stream'.</remarks>
        public BytesContent(byte[] content, int offset, int count)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (offset < 0)
            {
                throw ExceptionHelper.CanNotBeLess("offset", 0);
            }

            if (offset > content.Length)
            {
                throw ExceptionHelper.CanNotBeGreater("offset", content.Length);
            }

            if (count < 0)
            {
                throw ExceptionHelper.CanNotBeLess("count", 0);
            }

            if (count > (content.Length - offset))
            {
                throw ExceptionHelper.CanNotBeGreater("count", content.Length - offset);
            }

            #endregion

            _content = content;
            _offset = offset;
            _count = count;

            _contentType = "application/octet-stream";
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BytesContent"/>.
        /// </summary>
        protected BytesContent() { }


        #region Методы (открытые)

        /// <summary>
        /// Подсчитывает и возвращает длину тела запроса в байтах.
        /// </summary>
        /// <returns>Длина тела запроса в байтах.</returns>
        public override long CalculateContentLength()
        {
            return _content.LongLength;
        }

        /// <summary>
        /// Записывает данные тела запроса в поток.
        /// </summary>
        /// <param name="stream">Поток, куда будут записаны данные тела запроса.</param>
        public override void WriteTo(Stream stream)
        {
            stream.Write(_content, _offset, _count);
        }

        #endregion
    }
}