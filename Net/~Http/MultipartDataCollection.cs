using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace xNet.Net
{
    /// <summary>
    /// Представляет коллекцию Multipart/form данных.
    /// </summary>
    public class MultipartDataCollection : List<MultipartDataElement>
    {
        private string _boundary;


        /// <summary>
        /// Возвращает или задаёт префикс устанавливаемый в 'boundary'.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public static string BoundaryPrefix { get; set; }


        #region Методы (открытые)

        /// <summary>
        /// Добавляет элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента.</param>
        public void AddField(string name, string value)
        {
            var element = new MultipartDataElement()
            {
                Name = name,
                TextValue = value
            };

            Add(element);
        }

        /// <summary>
        /// Добавляет элемент Multipart/form данных.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="value">Значение элемента.</param>
        public void AddField(string name, byte[] value)
        {
            var element = new MultipartDataElement()
            {
                Name = name,
                BytesValue = value
            };

            Add(element);
        }

        /// <summary>
        /// Добавляет элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="fileName">Имя передаваемого файла.</param>
        /// <param name="contentType">Тип передаваемых данных.</param>
        /// <param name="value">Значение элемента.</param>
        public void AddFile(string name, string fileName, string contentType, byte[] value)
        {
            var element = new MultipartDataElement()
            {
                Name = name,
                FileName = fileName,
                ContentType = contentType,
                BytesValue = value
            };

            Add(element);
        }

        /// <summary>
        /// Добавляет элемент Multipart/form данных, представляющий файл.
        /// </summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="doPreLoading">Указывает, нужно ли делать предварительную загрузку файла.</param>
        /// <param name="contentType">Тип передаваемых данных, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Если использовать предварительную загрузку файла, то файл будет сразу загружен в память. Если файл имеет большой размер, либо нет необходимости, чтобы файл находился в памяти, то не используйте предварительную загрузку. В этом случае, файл будет загружаться блоками во время записи в поток.
        /// 
        /// Если не задать тип передаваемых данных, то он будет определяться по расширению файла. Если тип не удастся определить, то будет использовано значение ‘application/unknown‘.</remarks>
        public void AddFile(string name, string path, bool doPreLoading = false, string contentType = null)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/unknown";

                try
                {
                    using (RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(path)))
                    {
                        if (regKey != null)
                        {
                            object keyValue = regKey.GetValue("Content Type");

                            if (keyValue != null)
                            {
                                contentType = keyValue.ToString();
                            }
                        }
                    }
                }
                #region Catch's

                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }

                #endregion
            }

            var element = new MultipartDataElement()
            {
                Name = name,
                FileName = Path.GetFileName(path),
                ContentType = contentType
            };

            if (doPreLoading)
            {
                try
                {
                    element.BytesValue = File.ReadAllBytes(path);
                }
                #region Catch's

                catch (ArgumentException ex)
                {
                    throw ExceptionHelper.WrongPath("path", ex);
                }
                catch (NotSupportedException ex)
                {
                    throw ExceptionHelper.WrongPath("path", ex);
                }

                #endregion
            }
            else
            {
                element.PathToFile = path;
            }

            Add(element);
        }

        #endregion


        #region Методы (внутренние)

        internal string GenerateContentType()
        {
            _boundary = string.Format("{0}{1}", BoundaryPrefix, Rand.NextString(16));

            return string.Format("multipart/form-data; boundary={0}", _boundary);
        }

        internal int CalculateLength(Encoding encoding)
        {
            int length = 0;

            foreach (MultipartDataElement element in this)
            {
                length += element.CalculateLength(encoding);
            }

            int boundaryLength = 16 + (BoundaryPrefix ?? string.Empty).Length;

            // 2 (--) + x (boundary) + 2 (\r\n) ...n-й элемент данных... + 2 (\r\n).
            length += (boundaryLength + 6) * Count;

            // 2 (--) + x (boundary) + 2 (--).
            length += boundaryLength + 4;

            return length;
        }

        internal void Send(Action<byte[], int> writeBytesCallback, Encoding encoding)
        {
            byte[] newLineBytes = Encoding.ASCII.GetBytes("\r\n");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "\r\n");

            foreach (MultipartDataElement element in this)
            {
                writeBytesCallback(boundaryBytes, boundaryBytes.Length);
                element.Send(writeBytesCallback, encoding);
                writeBytesCallback(newLineBytes, newLineBytes.Length);
            }

            boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "--");
            writeBytesCallback(boundaryBytes, boundaryBytes.Length);
        }

        #endregion
    }
}