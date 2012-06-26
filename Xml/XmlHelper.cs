using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace xNet.Xml
{
    /// <summary>
    /// Представляет статический класс, предназначенный для помощи в работе с XML-документами.
    /// </summary>
    public static class XmlHelper
    {
        #region Статические методы (открытые)

        /// <summary>
        /// Возвращает значение узла, выбранного в соответствии с выражением XPath.
        /// </summary>
        /// <param name="xmlDocument">XML-документ, в котором будет вестись поиск узла.</param>
        /// <param name="xpath">Выражение XPath.</param>
        /// <returns>Найденное значение, иначе пустая строка.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xmlDocument"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xpath"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.Xml.XPath.XPathException">Неверное выражение XPath.</exception>
        public static string GetNodeValue(this XmlDocument xmlDocument, string xpath)
        {
            #region Проверка параметров

            if (xmlDocument == null)
            {
                throw new ArgumentNullException("xmlDocument");
            }

            if (xpath == null)
            {
                throw new ArgumentNullException("xpath");
            }

            #endregion

            XmlNodeList nodesList = xmlDocument.DocumentElement.SelectNodes(xpath);

            if (nodesList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return nodesList.Item(0).InnerText;
            }
        }

        /// <summary>
        /// Устанавливает значение узла, выбранного в соответствии с выражением XPath.
        /// </summary>
        /// <param name="xmlDocument">XML-документ, в котором будет вестись поиск узла.</param>
        /// <param name="xpath">Выражение XPath.</param>
        /// <param name="value">Устанавливаемое значение.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xmlDocument"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xpath"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.Xml.XPath.XPathException">Неверное выражение XPath.</exception>
        public static void SetNodeValue(this XmlDocument xmlDocument, string xpath, string value)
        {
            #region Проверка параметров

            if (xmlDocument == null)
            {
                throw new ArgumentNullException("xmlDocument");
            }

            if (xpath == null)
            {
                throw new ArgumentNullException("xpath");
            }

            #endregion

            XmlNodeList nodesList = xmlDocument.DocumentElement.SelectNodes(xpath);

            if (nodesList.Count != 0)
            {
                nodesList.Item(0).InnerText = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Возвращает узел, выбранный в соответствии с выражением XPath.
        /// </summary>
        /// <param name="xmlDocument">XML-документ, в котором будет вестись поиск узла.</param>
        /// <param name="xpath">Выражение XPath.</param>
        /// <returns>Найденный узел, иначе значение <see langword="null"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xmlDocument"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xpath"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.Xml.XPath.XPathException">Неверное выражение XPath.</exception>
        public static XmlNode GetNode(this XmlDocument xmlDocument, string xpath)
        {
            #region Проверка параметров

            if (xmlDocument == null)
            {
                throw new ArgumentNullException("xmlDocument");
            }

            if (xpath == null)
            {
                throw new ArgumentNullException("xpath");
            }

            #endregion

            XmlNodeList nodesList = xmlDocument.DocumentElement.SelectNodes(xpath);

            if (nodesList.Count == 0)
            {
                return null;
            }
            else
            {
                return nodesList.Item(0);
            }
        }

        /// <summary>
        /// Возвращает список узлов, выбранных в соответствии с выражением XPath.
        /// </summary>
        /// <param name="xmlDocument">XML-документ, в котором будет вестись поиск узлов.</param>
        /// <param name="xpath">Выражение XPath.</param>
        /// <returns>Список найденных узлов.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xmlDocument"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xpath"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.Xml.XPath.XPathException">Неверное выражение XPath.</exception>
        public static XmlNodeList GetNodes(this XmlDocument xmlDocument, string xpath)
        {
            #region Проверка параметров

            if (xmlDocument == null)
            {
                throw new ArgumentNullException("xmlDocument");
            }

            if (xpath == null)
            {
                throw new ArgumentNullException("xpath");
            }

            #endregion

            return xmlDocument.DocumentElement.SelectNodes(xpath);
        }

        /// <summary>
        /// Конструирует объект по схеме XML-документа.
        /// </summary>
        /// <typeparam name="T">Тип конструируемого объекта. Должен иметь открытый конструктор.</typeparam>
        /// <param name="xml">XML-документ, по схеме которого будет сконструирован объект.</param>
        /// <returns>Сконструированный объект.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="xml"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Значение параметра <paramref name="xml"/> является пустой строкой.</exception>
        /// <exception cref="System.InvalidOperationException">Возникла ошибка при десериализации.</exception>
        public static T XmlToObject<T>(string xml) where T : new()
        {
            #region Проверка параметров

            if (xml == null)
            {
                throw new ArgumentNullException("xml");
            }

            if (xml.Length == 0)
            {
                throw ExceptionHelper.EmptyString("xml");
            }

            #endregion

            T result = default(T);

            using (var strReader = new StringReader(xml))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                result = (T)xmlSerializer.Deserialize(strReader);
            }

            return result;
        }

        /// <summary>
        /// Преобразует открытые поля и свойства объекта в XML-документ.
        /// </summary>
        /// <param name="obj">Объект, который будет преобразован в XML-документ.</param>
        /// <returns>Полученный XML-документ.</returns>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="obj"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.InvalidOperationException">Возникла ошибка при сериализации.</exception>
        public static string ObjectToXml(object obj)
        {
            #region Проверка параметров

            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            #endregion

            var strBuilder = new StringBuilder();
            var xmlSerializer = new XmlSerializer(obj.GetType());

            using (var strWriter = new StringWriter(strBuilder, CultureInfo.InvariantCulture))
            {
                xmlSerializer.Serialize(strWriter, obj);
            }

            return strBuilder.ToString();
        }

        #endregion
    }
}