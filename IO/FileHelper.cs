using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace xNet.IO
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с файлами.
    /// </summary>
    public static class FileHelper
    {
        #region Статические методы (открытые)

        /// <summary>
        /// Возвращает значение, указывающее, пустой ли файл.
        /// </summary>
        /// <param name="path">Путь к проверяемому файлу.</param>
        /// <returns>Значение <see langword="true"/>, если файл пустой, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.PathTooLongException">Длина указанного пути, имени файла или обоих параметров превышает установленный системой предел. Для операционной системы Windows длина пути не должна превышать 248 знаков, а имена файлов не должны содержать более 260 знаков.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool IsEmpty(string path)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            FileInfo fileInfo;

            try
            {
                fileInfo = new FileInfo(path);
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

            return (fileInfo.Length == 0);
        }

        /// <summary>
        /// Возвращает значение, указывающее, пустой ли файл.
        /// </summary>
        /// <param name="fileInfo">Проверяемый файл.</param>
        /// <returns>Значение <see langword="true"/>, если файл пустой, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.IOException">Произошла ошибка ввода-вывода.</exception>
        public static bool IsEmpty(this FileInfo fileInfo)
        {
            #region Проверка параметров

            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            #endregion

            return (fileInfo.Length == 0);
        }

        /// <summary>
        /// Открывает текстовый файл, считывает диапазон строк, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="path">Путь к текстовому файлу.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="startLine">Позиция строки, с которой начинается считывание. Отсчёт от 1.</param>
        /// <param name="linesCount">Число строк, которое нужно считать. Если значение равно 0, то будут считаны все строки.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные данные.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="startLine"/> меньше 1.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="linesCount"/> меньше 0.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(string path, Func<string, T> converter,
            int startLine, int linesCount, Encoding encoding = null)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }

            if (startLine < 1)
            {
                throw ExceptionHelper.CanNotBeLess("startLine", 1);
            }

            if (linesCount < 0)
            {
                throw ExceptionHelper.CanNotBeLess("linesCount", 0);
            }

            #endregion

            if (encoding == null)
            {
                encoding = Encoding.Default;
            }

            StreamReader sReader = null;

            try
            {
                sReader = new StreamReader(path, encoding);
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

            using (sReader)
            {
                // Если нужно начать не с первой строки.
                if (startLine > 1)
                {
                    #region Перемещаемся до нужной строки

                    Stream stream = sReader.BaseStream;

                    for (int i = 1; i < startLine; ++i)
                    {
                        while (true)
                        {
                            int b = stream.ReadByte();

                            // Если достигнут конец потока.
                            if (b == -1)
                            {
                                yield break;
                            }

                            // Если считан символ '\n', то есть, достигнут конец строки.
                            if (b == 10)
                            {
                                break;
                            }
                        }
                    }

                    #endregion
                }

                string line;
                int linesRead = 0;

                while ((line = sReader.ReadLine()) != null)
                {
                    T value = converter(line);

                    if (value != null)
                    {
                        yield return value;
                    }

                    // Если нужно вести учёт считанных строк и достигнут предел.
                    if (linesCount != 0 && ++linesRead == linesCount)
                    {
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Открывает текстовый файл, считывает диапазон строк, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="fileInfo">Текстовый файл.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="startLine">Позиция строки, с которой начинается считывание. Отсчёт от 1.</param>
        /// <param name="linesCount">Число строк, которое нужно считать. Если значение равно 0, то будут считаны все строки.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные данные.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="startLine"/> меньше 1.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="linesCount"/> меньше 0.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(this FileInfo fileInfo, Func<string, T> converter,
            int startLine, int linesCount, Encoding encoding = null)
        {
            #region Проверка параметров

            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            #endregion

            return FileHelper.ReadData<T>(
                fileInfo.FullName, converter, startLine, linesCount, encoding);
        }

        /// <summary>
        /// Открывает текстовый файл, считывает строки начиная с заданной позиции, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="path">Путь к текстовому файлу.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="startLine">Позиция строки, с которой начинается считывание. Отсчёт от 1.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные данные.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="startLine"/> меньше 1.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(string path, Func<string, T> converter,
            int startLine, Encoding encoding = null)
        {
            return ReadData<T>(path, converter, startLine, 0, encoding);
        }

        /// <summary>
        /// Открывает текстовый файл, считывает строки начиная с заданной позиции, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="fileInfo">Текстовый файл.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="startLine">Позиция линии, с которой начинается считывание. Отсчёт от 1.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использоваться <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные объекты.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="startLine"/> меньше 1.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(this FileInfo fileInfo, Func<string, T> converter,
            int startLine, Encoding encoding = null)
        {
            return FileHelper.ReadData<T>(fileInfo, converter, startLine, 0, encoding);
        }

        /// <summary>
        /// Открывает текстовый файл, считывает все строки, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="path">Путь к текстовому файлу.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные данные.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(string path,
            Func<string, T> converter, Encoding encoding = null)
        {
            return ReadData<T>(path, converter, 1, 0, encoding);
        }

        /// <summary>
        /// Открывает текстовый файл, считывает все строки, преобразуя их в заданный тип данных с помощью заданного метода, и закрывает файл.
        /// </summary>
        /// <typeparam name="T">Тип данных возвращаемых перечислителем и <paramref name="converter"/>.</typeparam>
        /// <param name="fileInfo">Текстовый файл.</param>
        /// <param name="converter">Метод, который будет преобразовывать строки в заданный тип данных. Если метод вернёт <see langword="null"/>, то он не будет возвращён перечислителем.</param>
        /// <param name="encoding">Кодировка, применяемая к содержимому файла. Если значение параметра равно <see langword="null"/>, то будет использовано значение <see cref="System.Text.Encoding.Default"/>.</param>
        /// <returns>Перечислитель, содержащий считанные данные.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="converter"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.IO.FileNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на несуществующий файл.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="fileInfo"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.IOException">Ошибка ввода-вывода при работе с файлом.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Операция чтения файла не поддерживается на текущей платформе.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Значение параметра <paramref name="path"/> определяет каталог.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <remarks>Данный метод можно использовать, допустим, если в текстовом файле хранятся строки, представляющие информацию об объектах. Он позволит считать строки, разобрать их и преобразовать в определённый объект. Если строку не удастся распознать, то следует вернуть <see langword="null"/>.</remarks>
        public static IEnumerable<T> ReadData<T>(this FileInfo fileInfo,
            Func<string, T> converter, Encoding encoding = null)
        {
            return FileHelper.ReadData<T>(
                fileInfo, converter, 1, 0, encoding);
        }

        #endregion
    }
}