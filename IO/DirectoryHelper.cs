using System;
using System.Collections.Generic;
using System.IO;

namespace xNet.IO
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с директориями.
    /// </summary>
    public static class DirectoryHelper
    {
        #region Статические методы (открытые)

        /// <summary>
        /// Возвращает значение, указывающее, пустой ли каталог.
        /// </summary>
        /// <param name="path">Путь к проверяемому каталогу.</param>
        /// <returns>Значение <see langword="true"/>, если каталог пустой, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.IO.PathTooLongException">Значение параметра <paramref name="path"/> превышает наибольшую возможную длину, определенную системой. Для операционной системы Windows длина пути не должна превышать 248 знаков.</exception>
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

            IEnumerable<string> items;

            try
            {
                items = Directory.EnumerateFileSystemEntries(path);
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw new ArgumentException(Resources.ArgumentException_WrongPath, "path", ex);
            }
            catch (PathTooLongException) { throw; }
            catch (IOException ex)
            {
                throw new DirectoryNotFoundException(string.Format(
                    Resources.DirectoryNotFoundException_DirectoryNotFound, path), ex);
            }

            #endregion

            using (IEnumerator<string> en = items.GetEnumerator())
            {
                return !en.MoveNext();
            }
        }

        /// <summary>
        /// Возвращает значение, указывающее, пустой ли каталог.
        /// </summary>
        /// <param name="dirInfo">Проверяемый каталог.</param>
        /// <returns>Значение <see langword="true"/>, если каталог пустой, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dirInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="dirInfo"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool IsEmpty(this DirectoryInfo dirInfo)
        {
            #region Проверка параметров

            if (dirInfo == null)
            {
                throw new ArgumentNullException("dirInfo");
            }

            #endregion

            return IsEmpty(dirInfo.FullName);
        }

        /// <summary>
        /// Возвращает значение, указывающее, есть ли в каталоге заданный файл.
        /// </summary>
        /// <param name="path">Путь к каталогу, в котором будет вестись поиск.</param>
        /// <param name="fileName">Имя файла с расширением, используемое для поиска.</param>
        /// <returns>Значение <see langword="true"/>, если в каталоге есть заданный файл, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="fileName"/> является пустой строкой.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.IO.PathTooLongException">Значение параметра <paramref name="path"/> превышает наибольшую возможную длину, определенную системой. Для операционной системы Windows длина пути не должна превышать 248 знаков.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool ContainsFile(string path, string fileName)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (fileName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("fileName");
            }

            #endregion

            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(path);
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (PathTooLongException) { throw; }
            catch (IOException ex)
            {
                throw new DirectoryNotFoundException(string.Format(
                    Resources.DirectoryNotFoundException_DirectoryNotFound, path), ex);
            }

            #endregion

            foreach (string file in files)
            {
                string currentFileName = Path.GetFileName(file);

                if (currentFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Возвращает значение, указывающее, есть ли в каталоге заданный файл.
        /// </summary>
        /// <param name="dirInfo">Каталог, в котором будет вестись поиск.</param>
        /// <param name="fileName">Имя файла с расширением, используемое для поиска.</param>
        /// <returns>Значение <see langword="true"/>, если в каталоге есть заданный файл, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dirInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="fileName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="fileName"/> является пустой строкой.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="dirInfo"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool ContainsFile(this DirectoryInfo dirInfo, string fileName)
        {
            #region Проверка параметров

            if (dirInfo == null)
            {
                throw new ArgumentNullException("dirInfo");
            }

            #endregion

            return ContainsFile(dirInfo.FullName, fileName);
        }

        /// <summary>
        /// Возвращает значение, указывающее, есть ли в каталоге заданный каталог.
        /// </summary>
        /// <param name="path">Путь к каталогу, в котором будет вестись поиск.</param>
        /// <param name="dirName">Имя каталога, используемое для поиска.</param>
        /// <returns>Значение <see langword="true"/>, если в каталоге есть заданный каталог, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="path"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="path"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dirName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="dirName"/> является пустой строкой.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="path"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.IO.PathTooLongException">Значение параметра <paramref name="path"/> превышает наибольшую возможную длину, определенную системой. Для операционной системы Windows длина пути не должна превышать 248 знаков.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool ContainsDirectory(string path, string dirName)
        {
            #region Проверка параметров

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (dirName == null)
            {
                throw new ArgumentNullException("dirName");
            }

            if (dirName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("dirName");
            }

            #endregion

            IEnumerable<string> directories;

            try
            {
                directories = Directory.EnumerateDirectories(path);
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (PathTooLongException) { throw; }
            catch (IOException ex)
            {
                throw new DirectoryNotFoundException(string.Format(
                    Resources.DirectoryNotFoundException_DirectoryNotFound, path), ex);
            }

            #endregion

            foreach (string directory in directories)
            {
                string curDirName;
                int pos = directory.LastIndexOf('\\');

                if (pos == -1)
                {
                    curDirName = directory;
                }
                else
                {
                    curDirName = directory.Substring(pos + 1);
                }

                if (curDirName.Equals(dirName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Возвращает значение, указывающее, есть ли в каталоге заданный каталог.
        /// </summary>
        /// <param name="dirInfo">Каталог, в котором будет вестись поиск.</param>
        /// <param name="dirName">Имя каталога, используемое для поиска.</param>
        /// <returns>Значение <see langword="true"/>, если в каталоге есть заданный каталог, иначе значение <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dirInfo"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dirName"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="dirName"/> является пустой строкой.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="dirInfo"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        public static bool ContainsDirectory(this DirectoryInfo dirInfo, string dirName)
        {
            #region Проверка параметров

            if (dirInfo == null)
            {
                throw new ArgumentNullException("dirInfo");
            }

            #endregion

            return ContainsDirectory(dirInfo.FullName, dirName);
        }

        /// <summary>
        /// Копирует каталог со всем его содержимым в другой каталог.
        /// </summary>
        /// <param name="sourceDir">Путь к каталогу, который необходимо скопировать.</param>
        /// <param name="destinationDir">Путь к конечному каталогу. Если он не существует, то будет создан новый.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="sourceDir"/> или <paramref name="destinationDir"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="sourceDir"/> или <paramref name="destinationDir"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="sourceDir"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="destinationDir"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.PathTooLongException">Значение параметра <paramref name="sourceDir"/> или <paramref name="destinationDir"/> превышает наибольшую возможную длину, определенную системой. Для операционной системы Windows длина пути не должна превышать 248 знаков.</exception>
        /// <exception cref="System.IO.PathTooLongException">При копировании образовался слишком длинный путь.</exception>
        /// <exception cref="System.IO.IOException">Произошла ошибка ввода-вывода.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Каталог, заданный параметром <paramref name="destinationDir"/>, доступен только для чтения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Один из файлов, находящихся в каталоге <paramref name="destinationDir"/>, доступен только для чтения.</exception>
        public static void Copy(string sourceDir, string destinationDir)
        {
            #region Проверка параметров

            if (sourceDir == null)
            {
                throw new ArgumentNullException("sourceDir");
            }

            if (destinationDir == null)
            {
                throw new ArgumentNullException("destinationDir");
            }

            #endregion

            if (!Directory.Exists(destinationDir))
            {
                try
                {
                    Directory.CreateDirectory(destinationDir);
                }
                #region Catch's

                catch (ArgumentException ex)
                {
                    throw ExceptionHelper.WrongPath("destinationDir", ex);
                }
                catch (NotSupportedException ex)
                {
                    throw ExceptionHelper.WrongPath("destinationDir", ex);
                }
                catch (PathTooLongException) { throw; }
                catch (DirectoryNotFoundException) { throw; }
                catch (IOException ex)
                {
                    throw new UnauthorizedAccessException(string.Format(
                        Resources.UnauthorizedAccessException_DirOnlyForRead, destinationDir), ex);
                }

                #endregion
            }

            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(sourceDir);
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("sourceDir", ex);
            }
            catch (PathTooLongException) { throw; }
            catch (IOException ex)
            {
                throw new DirectoryNotFoundException(string.Format(
                    Resources.DirectoryNotFoundException_DirectoryNotFound, sourceDir), ex);
            }

            #endregion

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destination = Path.Combine(destinationDir, fileName);

                File.Copy(file, destination, true);
            }

            IEnumerable<string> directories = Directory.EnumerateDirectories(sourceDir);

            foreach (string directory in directories)
            {
                string dirName = Path.GetDirectoryName(directory);
                string destination = Path.Combine(destinationDir, dirName);

                Copy(directory, destination);
            }
        }

        /// <summary>
        /// Копирует каталог со всем его содержимым в другой каталог.
        /// </summary>
        /// <param name="sourceDir">Каталог, который необходимо скопировать.</param>
        /// <param name="destinationDir">Путь к конечному каталогу. Если он не существует, то будет создан новый.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="sourceDir"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="destinationDir"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="destinationDir"/> является пустой строкой, содержит только пробелы или содержит недопустимые символы.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="sourceDir"/> указывает на несуществующий каталог.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Значение параметра <paramref name="destinationDir"/> указывает на недопустимый путь.</exception>
        /// <exception cref="System.IO.PathTooLongException">Значение параметра <paramref name="destinationDir"/> превышает наибольшую возможную длину, определенную системой. Для операционной системы Windows длина пути не должна превышать 248 знаков.</exception>
        /// <exception cref="System.IO.PathTooLongException">При копировании образовался слишком длинный путь.</exception>
        /// <exception cref="System.IO.IOException">Произошла ошибка ввода-вывода.</exception>
        /// <exception cref="System.Security.SecurityException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Вызывающий оператор не имеет необходимого разрешения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Каталог, заданный параметром <paramref name="destinationDir"/>, доступен только для чтения.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Один из файлов, находящихся в каталоге <paramref name="destinationDir"/>, доступен только для чтения.</exception>
        public static void Copy(this DirectoryInfo sourceDir, string destinationDir)
        {
            #region Проверка параметров

            if (sourceDir == null)
            {
                throw new ArgumentNullException("sourceDir");
            }

            #endregion

            DirectoryHelper.Copy(sourceDir.FullName, destinationDir);
        }

        #endregion
    }
}