using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// Представляет тело запроса в виде потока.
    /// </summary>
    public class StreamContent : HttpContent
    {
        #region Поля (защищённые электромагнитным излучением)

        /// <summary>Содержимое тела запроса.</summary>
        protected Stream _content;
        /// <summary>Размер буфера в байтах для потока.</summary>
        protected int _bufferSize;
        /// <summary>Позиция в байтах, с которой начинается считывание данных из потока.</summary>
        protected long _initialStreamPosition;

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StreamContent"/>.
        /// </summary>
        /// <param name="content">Содержимое тела запроса.</param>
        /// <param name="bufferSize">Размер буфера в байтах для потока.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="content"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Поток <paramref name="content"/> не поддерживает чтение или перемещение позиции.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> Значение параметра <paramref name="bufferSize"/> меньше 1.</exception>
        /// <remarks>По умолчанию используется тип контента - 'application/octet-stream'.</remarks>
        public StreamContent(Stream content, int bufferSize = 32768)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (!content.CanRead || !content.CanSeek)
            {
                throw new ArgumentException(Resources.ArgumentException_CanNotReadOrSeek, "content");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = content;
            _bufferSize = bufferSize;
            _initialStreamPosition = _content.Position;

            _contentType = "application/octet-stream";
        }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StreamContent"/>.
        /// </summary>
        protected StreamContent() { }


        #region Методы (открытые)

        /// <summary>
        /// Подсчитывает и возвращает длину тела запроса в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            return _content.Length;
        }

        /// <summary>
        /// Записывает данные тела запроса в поток.
        /// </summary>
        /// <param name="stream">Поток, куда будут записаны данные тела запроса.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="stream"/> равно <see langword="null"/>.</exception>
        public override void WriteTo(Stream stream)
        {
            ThrowIfDisposed();

            #region Проверка параметров

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            _content.Position = _initialStreamPosition;

            var buffer = new byte[_bufferSize];

            while (true)
            {
                int bytesRead = _content.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                stream.Write(buffer, 0, bytesRead);
            }
        }

        #endregion


        /// <summary>
        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _content != null)
            {
                _content.Dispose();
                _content = null;
            }
        }


        private void ThrowIfDisposed()
        {
            if (_content == null)
            {
                throw new ObjectDisposedException("StreamContent");
            }
        }
    }
}