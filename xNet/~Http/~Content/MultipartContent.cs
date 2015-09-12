using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace xNet
{
    /// <summary>
    /// Представляет тело запроса в виде состовного содержимого.
    /// </summary>
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        private sealed class Element
        {
            #region Поля (открытые)

            public string Name;
            public string FileName;

            public HttpContent Content;

            #endregion


            public bool IsFieldFile()
            {
                return FileName != null;
            }
        }


        #region Константы (закрытые)

        private const int FieldTemplateSize = 43;
        private const int FieldFileTemplateSize = 72;
        private const string FieldTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n";
        private const string FieldFileTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

        #endregion


        #region Статические поля (закрытые)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        #region Поля (закрытые)

        private string _boundary;
        private List<Element> _elements = new List<Element>();

        #endregion


        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultipartContent"/>.
        /// </summary>
        public MultipartContent()
            : this("----------------" + GetRandomString(16)) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultipartContent"/>.
        /// </summary>
        /// <param name="boundary">Граница для отделения составных частей содержимого.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="boundary"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="boundary"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="boundary"/> имеет длину более 70 символов.</exception>
        public MultipartContent(string boundary)
        {
            #region Проверка параметров

            if (boundary == null)
            {
                throw new ArgumentNullException("boundary");
            }

            if (boundary.Length == 0)
            {
                throw ExceptionHelper.EmptyString("boundary");
            }

            if (boundary.Length > 70)
            {
                throw ExceptionHelper.CanNotBeGreater("boundary", 70);
            }

            #endregion

            _boundary = boundary;

            _contentType = string.Format("multipart/form-data; boundary={0}", _boundary);
        }

        #endregion


        #region Методы (открытые)

        /// <summary>
        /// Добавляет новый элемент состовного содержимого тела запроса.
        /// </summary>
        /// <param name="content">Значение элемента.</param>
        /// <param name="name">Имя элемента.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        public void Add(HttpContent content, string name)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            var element = new Element()
            {
                Name = name,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// Добавляет новый элемент состовного содержимого тела запроса.
        /// </summary>
        /// <param name="content">Значение элемента.</param>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя файла элемента.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        public void Add(HttpContent content, string name, string fileName)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            content.ContentType = Http.DetermineMediaType(
                Path.GetExtension(fileName));

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// Добавляет новый элемент состовного содержимого тела запроса.
        /// </summary>
        /// <param name="content">Значение элемента.</param>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя файла элемента.</param>
        /// <param name="contentType">MIME-тип контента.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="content"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="name"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="contentType"/> равно <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="name"/> является пустой строкой.</exception>
        public void Add(HttpContent content, string name, string fileName, string contentType)
        {
            #region Проверка параметров

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            content.ContentType = contentType;

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// Подсчитывает и возвращает длину тела запроса в байтах.
        /// </summary>
        /// <returns>Длина тела запроса в байтах.</returns>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            long length = 0;

            foreach (var element in _elements)
            {
                length += element.Content.CalculateContentLength();

                if (element.IsFieldFile())
                {
                    length += FieldFileTemplateSize;
                    length += element.Name.Length;
                    length += element.FileName.Length;
                    length += element.Content.ContentType.Length;
                }
                else
                {
                    length += FieldTemplateSize;
                    length += element.Name.Length;
                }

                // 2 (--) + x (boundary) + 2 (\r\n) ...элемент данных... + 2 (\r\n).
                length += _boundary.Length + 6;
            }

            // 2 (--) + x (boundary) + 2 (--) + 2 (\r\n).
            length += _boundary.Length + 6;

            return length;
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

            byte[] newLineBytes = Encoding.ASCII.GetBytes("\r\n");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "\r\n");

            foreach (var element in _elements)
            {
                stream.Write(boundaryBytes, 0, boundaryBytes.Length);

                string field;

                if (element.IsFieldFile())
                {
                    field = string.Format(
                        FieldFileTemplate, element.Name, element.FileName, element.Content.ContentType);
                }
                else
                {
                    field = string.Format(
                        FieldTemplate, element.Name);
                }

                byte[] fieldBytes = Encoding.ASCII.GetBytes(field);
                stream.Write(fieldBytes, 0, fieldBytes.Length);

                element.Content.WriteTo(stream);
                stream.Write(newLineBytes, 0, newLineBytes.Length);
            }

            boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "--\r\n");
            stream.Write(boundaryBytes, 0, boundaryBytes.Length);
        }

        /// <summary>
        /// Возвращает перечеслитель элементов составного содержимого.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public IEnumerator<HttpContent> GetEnumerator()
        {
            ThrowIfDisposed();

            return _elements.Select(e => e.Content).GetEnumerator();
        }

        #endregion


        /// <summary>
        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _elements != null)
            {
                foreach (var element in _elements)
                {
                    element.Content.Dispose();
                }

                _elements = null;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();

            return GetEnumerator();
        }


        #region Методы (закрытые)

        public static string GetRandomString(int length)
        {
            var strBuilder = new StringBuilder(length);

            for (int i = 0; i < length; ++i)
            {
                switch (Rand.Next(3))
                {
                    case 0:
                        strBuilder.Append((char)Rand.Next(48, 58));
                        break;

                    case 1:
                        strBuilder.Append((char)Rand.Next(97, 123));
                        break;

                    case 2:
                        strBuilder.Append((char)Rand.Next(65, 91));
                        break;
                }
            }

            return strBuilder.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_elements == null)
            {
                throw new ObjectDisposedException("MultipartContent");
            }
        }

        #endregion
    }
}