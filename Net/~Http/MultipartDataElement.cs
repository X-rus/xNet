using System;
using System.IO;
using System.Text;

namespace xNet.Net
{
    /// <summary>
    /// Представляет элемент Multipart/form данных.
    /// </summary>
    public class MultipartDataElement
    {
        #region Константы (закрытые)

        private const int DataTemplateSize = 43;
        private const int DataFileTemplateSize = 72;
        private const string DataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n";
        private const string DataFileTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

        #endregion


        #region Поля (закрытые)

        private string _textValue;
        private byte[] _bytesValue;
        private string _pathToFile;

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает или задаёт имя элемента.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string Name { get; set; }

        /// <summary>
        /// Возвращает или задаёт имя передаваемого файла.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если свойство заданно, то элемент передаётся как файл.</remarks>
        public string FileName { get; set; }

        /// <summary>
        /// Возвращает или задаёт тип передаваемых данных.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Возвращает или задаёт значение элемента в виде текста.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>При установке значения, обнуляются значения свойств <see cref="BytesValue"/> и <see cref="PathToFile"/>.</remarks>
        public string TextValue
        {
            get
            {
                return _textValue;
            }
            set
            {
                _bytesValue = null;
                _pathToFile = null;

                _textValue = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт значение элемента в виде байтов.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>При установке значения, обнуляются значения свойств <see cref="TextValue"/> и <see cref="PathToFile"/>.</remarks>
        public byte[] BytesValue
        {
            get
            {
                return _bytesValue;
            }
            set
            {
                _textValue = null;
                _pathToFile = null;

                _bytesValue = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт путь к файлу, который нужно передать в виде значения элемента.
        /// </summary>
        /// <value>Значение по умолчанию — <see langword="null"/>.</value>
        /// <remarks>Если использовать данное свойство, то файл будет загружаться блоками во время записи в поток. При установке значения, обнуляются значения свойств <see cref="TextValue"/> и <see cref="BytesValue"/>.</remarks>
        public string PathToFile
        {
            get
            {
                return _pathToFile;
            }
            set
            {
                _textValue = null;
                _bytesValue = null;

                _pathToFile = value;
            }
        }

        #endregion


        #region Методы (внутренние)

        internal int CalculateLength(Encoding encoding)
        {
            int length = 0;

            length += encoding.GetByteCount(Name ?? string.Empty);

            if (string.IsNullOrEmpty(FileName))
            {
                length += DataTemplateSize;
            }
            else
            {
                length += DataFileTemplateSize;
                length += encoding.GetByteCount(FileName ?? string.Empty);
                length += encoding.GetByteCount(ContentType ?? string.Empty);
            }

            if (TextValue != null)
            {
                length += encoding.GetByteCount(TextValue);
            }
            else if (BytesValue != null)
            {
                length += BytesValue.Length;
            }
            else if (!string.IsNullOrEmpty(PathToFile))
            {
                if (!File.Exists(PathToFile))
                {
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_MultipartDataElement_FailedGetFileSize, PathToFile));
                }

                var fileInfo = new FileInfo(PathToFile);
                length += (int)fileInfo.Length;
            }

            return length;
        }

        internal void Send(Action<byte[], int> writeBytesCallback, Encoding encoding)
        {
            string data;

            if (string.IsNullOrEmpty(FileName))
            {
                data = string.Format(DataTemplate, Name);
            }
            else
            {
                data = string.Format(DataFileTemplate, Name, FileName, ContentType);
            }

            byte[] buffer = Encoding.ASCII.GetBytes(data);
            writeBytesCallback(buffer, buffer.Length);

            if (TextValue != null)
            {
                buffer = encoding.GetBytes(TextValue);
                writeBytesCallback(buffer, buffer.Length);
            }
            else if (BytesValue != null)
            {
                writeBytesCallback(BytesValue, BytesValue.Length);
            }
            else if (!string.IsNullOrEmpty(PathToFile))
            {
                #region Передача данных файла в поток

                if (!File.Exists(PathToFile))
                {
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_MultipartDataElement_FailedReadFile, PathToFile));
                }

                using (var fStream = new FileStream(PathToFile, FileMode.Open, FileAccess.Read))
                {
                    buffer = new byte[32768];

                    while (true)
                    {
                        int bytesRead = fStream.Read(buffer, 0, buffer.Length);

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        writeBytesCallback(buffer, bytesRead);
                    }
                }

                #endregion
            }
        }

        #endregion
    }
}