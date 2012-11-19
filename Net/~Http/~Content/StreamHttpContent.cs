using System;
using System.IO;

namespace xNet.Net
{
    /// <summary>
    /// Представляет контент в виде потока, данные из которого отправляется HTTP-серверу.
    /// </summary>
    public class StreamHttpContent : HttpContent
    {
        private Stream _stream;


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StreamHttpContent"/>.
        /// </summary>
        /// <param name="stream">Поток представляющий данные контента.</param>
        public StreamHttpContent(Stream stream)
        {
            _stream = stream;
        }


        #region Методы (открытые)

        /// <summary>
        /// Возвращает MIME-тип контента.
        /// </summary>
        /// <returns>MIME-тип контента.</returns>
        override public string GetType()
        {
            return "application/octet-stream";
        }

        /// <summary>
        /// Возвращает длину контента в байтах.
        /// </summary>
        /// <returns>Длина контента в байтах.</returns>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        override public long GetLength()
        {
            ThrowIfDisposed();

            return _stream.Length;
        }

        /// <summary>
        /// Вызывается, когда требуется отправить данные.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        override public void Send()
        {
            ThrowIfDisposed();

            if (_stream.CanSeek)
            {
                _stream.Position = 0;
            }

            var buffer = new byte[32768];

            while (true)
            {
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                _writeBytesCallback(buffer, bytesRead);
            }
        }

        #endregion


        /// <summary>
        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }


        private void ThrowIfDisposed()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("StreamHttpContent");
            }
        }
    }
}