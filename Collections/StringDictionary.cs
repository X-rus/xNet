using System.Collections.Generic;

namespace xNet.Collections
{
    /// <summary>
    /// Представляет коллекцию строк. Данный класс наследует от <see cref="Dictionary&lt;TKey, TValue&gt;"/>, где ключ и значение представлены в виде строки.
    /// </summary>
    public class StringDictionary : Dictionary<string, string>
    {
        #region Конструкторы (открытые)

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="StringDictionary"/> с начальной емкостью по умолчанию и использующий компаратор по умолчанию, проверяющий на равенство.
        /// </summary>
        public StringDictionary() { }

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="StringDictionary"/> с заданной начальной емкостью и использующий компаратор по умолчанию, проверяющий на равенство.
        /// </summary>
        /// <param name="capacity">Начальное количество элементов, которое может содержать коллекция.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="capacity"/> меньше 0.</exception>
        public StringDictionary(int capacity) 
            : base(capacity) { }

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="StringDictionary"/> с начальной емкостью по умолчанию и использующий указанный компаратор <see cref="IEqualityComparer&lt;T&gt;"/>.
        /// </summary>
        /// <param name="comparer">Реализация <see cref="IEqualityComparer&lt;T&gt;"/>, которую следует использовать при сравнении ключей, или <see langword="null"/>, если для данного типа ключа должна использоваться реализация <see cref="EqualityComparer&lt;T&gt;"/> по умолчанию.</param>
        public StringDictionary(IEqualityComparer<string> comparer)
            : base(comparer) { }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="StringDictionary"/>, который содержит элементы, скопированные из заданного словаря <see cref="IDictionary&lt;TKey, TValue&gt;"/> и использует компаратор по умолчанию, проверяющий на равенство.
        /// </summary>
        /// <param name="dictionary">Словарь, элементы которого будут скопированы в новый словарь.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dictionary"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="dictionary"/> содержит один или более повторяющихся ключей.</exception>
        public StringDictionary(IDictionary<string, string> dictionary)
            : base(dictionary) { }

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="StringDictionary"/> с заданной начальной емкостью и использует заданный компаратор <see cref="IEqualityComparer&lt;T&gt;"/>.
        /// </summary>
        /// <param name="capacity">Начальное количество элементов, которое может содержать коллекция.</param>
        /// <param name="comparer">Реализация <see cref="IEqualityComparer&lt;T&gt;"/>, которую следует использовать при сравнении ключей, или <see langword="null"/>, если для данного типа ключа должна использоваться реализация <see cref="EqualityComparer&lt;T&gt;"/> по умолчанию.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="capacity"/> меньше 0.</exception>
        public StringDictionary(int capacity, IEqualityComparer<string> comparer)
            : base(capacity, comparer) { }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="StringDictionary"/>, который содержит элементы, скопированные из заданного словаря <see cref="IDictionary&lt;TKey, TValue&gt;"/> и использует заданный компаратор <see cref="IEqualityComparer&lt;T&gt;"/>.
        /// </summary>
        /// <param name="dictionary">Словарь, элементы которого будут скопированы в новый словарь.</param>
        /// <param name="comparer">Реализация <see cref="IEqualityComparer&lt;T&gt;"/>, которую следует использовать при сравнении ключей, или <see langword="null"/>, если для данного типа ключа должна использоваться реализация <see cref="EqualityComparer&lt;T&gt;"/> по умолчанию.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="dictionary"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="dictionary"/> содержит один или более повторяющихся ключей.</exception>
        public StringDictionary(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer)
            : base(dictionary, comparer) { }

        #endregion
    }
}