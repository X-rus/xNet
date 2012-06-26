using System;

namespace xNet.Net
{
    /// <summary>
    /// Представляет данные для события, сообщающим о прогрессе выгрузки данных.
    /// </summary>
    public sealed class UploadProgressChangedEventArgs : EventArgs
    {
        #region Свойства (открытые)

        /// <summary>
        /// Возвращает количество отправленных байтов.
        /// </summary>
        public int BytesSent { get; private set; }

        /// <summary>
        /// Возвращает общее количество отправляемых байтов.
        /// </summary>
        public int TotalBytesToSend { get; private set; }

        /// <summary>
        /// Возвращает процент отправленных байтов.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                return ((double)BytesSent / (double)TotalBytesToSend) * 100.0;
            }
        }

        #endregion


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="UploadProgressChangedEventArgs"/>.
        /// </summary>
        /// <param name="bytesSent">Количество отправленных байтов.</param>
        /// <param name="totalBytesToSend">Общее количество отправляемых байтов.</param>
        public UploadProgressChangedEventArgs(int bytesSent, int totalBytesToSend)
        {
            BytesSent = bytesSent;
            TotalBytesToSend = totalBytesToSend;
        }
    }
}