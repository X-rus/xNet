using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// Представляет тело запроса в виде потока данных из определённого файла.
    /// </summary>
    public class FileContent : StreamContent
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FileContent"/> и открывает поток файла.
        /// </summary>
        /// <param name="pathToContent">Путь к файлу, который станет содержимым тела запроса.</param>
        /// <param name="bufferSize">Размер буфера в байтах для потока.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="pathToContent"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="pathToContent"/> является пустой строкой.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> Значение параметра <paramref name="bufferSize"/> меньше 1.</exception>
        /// <exception cref="System.IO.PathTooLongException">Указанный путь, имя файла или и то и другое превышает наибольшую возможную длину, определенную системой. Например, для платформ на основе Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="pathToContent"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="pathToContent"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// Операция чтения файла не поддерживается на текущей платформе.
        /// -или-
        /// Значение параметра <paramref name="pathToContent"/> определяет каталог.
        /// -или-
        /// Вызывающий оператор не имеет необходимого разрешения.
        /// </exception>
        /// <remarks>Тип контента определяется автоматически на основе расширения файла.</remarks>
        public FileContent(string pathToContent, int bufferSize = 32768)
        {
            #region Проверка параметров

            if (pathToContent == null)
            {
                throw new ArgumentNullException("pathToContent");
            }

            if (pathToContent.Length == 0)
            {
                throw ExceptionHelper.EmptyString("pathToContent");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = new FileStream(pathToContent, FileMode.Open, FileAccess.Read);
            _bufferSize = bufferSize;
            _initialStreamPosition = 0;

            _contentType = Http.DetermineMediaType(
                Path.GetExtension(pathToContent));
        }
    }
}