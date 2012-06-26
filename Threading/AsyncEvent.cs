using System;
using System.ComponentModel;
using System.Threading;

namespace xNet.Threading
{
    /// <summary>
    /// Представляет оболочку для асинхронного вызова события с помощью класса <see cref="AsyncOperation"/>.
    /// </summary>
    /// <typeparam name="TEventArgs">Тип данных аргументов события. Должен наследоваться от <see cref="System.EventArgs"/>.</typeparam>
    public class AsyncEvent<TEventArgs> where TEventArgs : EventArgs
    {
        #region Поля (закрытые)

        private readonly Action<TEventArgs> _onEvent;
        private readonly SendOrPostCallback _callbackOnEvent;

        #endregion


        /// <summary>
        /// Обработчик события.
        /// </summary>
        public EventHandler<TEventArgs> EventHandler { get; set; }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="AsyncEvent&lt;TEventArgs&gt;"/> заданным методом вызова события.
        /// </summary>
        /// <param name="onEvent">Метод вызова события.</param>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="onEvent"/> равно <see langword="null"/>.</exception>
        public AsyncEvent(Action<TEventArgs> onEvent)
        {
            #region Проверка параметров

            if (onEvent == null)
            {
                throw new ArgumentNullException("onEvent");
            }

            #endregion

            _onEvent = onEvent;
            _callbackOnEvent = new SendOrPostCallback(OnCallback);
        }


        #region Методы (открытые)

        /// <summary>
        /// Вызывает событие обычным образом.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="eventArgs">Аргументы события.</param>
        public void On(object sender, TEventArgs eventArgs)
        {
            EventHandler<TEventArgs> handler = EventHandler;

            if (handler != null)
            {
                handler(sender, eventArgs);
            }
        }

        /// <summary>
        /// Асинхронно вызывает событие.
        /// </summary>
        /// <param name="asyncOperation">Объект для асинхронных операций.</param>
        /// <param name="sender">Источник события.</param>
        /// <param name="eventArgs">Аргументы события.</param>
        /// <remarks>Если значение параметра <paramref name="asyncOperation"/> равно <see langword="null"/>, то событие будет вызвано обычным образом.</remarks>
        /// <exception cref="System.InvalidOperationException">Уже было завершено выполнение асинхронных операций для <paramref name="asyncOperation"/>.</exception>
        public void Post(AsyncOperation asyncOperation, object sender, TEventArgs eventArgs)
        {
            if (asyncOperation == null)
            {
                On(sender, eventArgs);
            }
            else
            {
                asyncOperation.Post(_callbackOnEvent, eventArgs);
            }
        }

        /// <summary>
        /// Асинхронно вызывает событие и завершает выполнение асинхронной операции объекта <see cref="AsyncOperation"/>.
        /// </summary>
        /// <param name="asyncOperation">Объект для асинхронных операций.</param>
        /// <param name="sender">Источник события.</param>
        /// <param name="eventArgs">Аргументы события.</param>
        /// <remarks>Если значение параметра <paramref name="asyncOperation"/> равно <see langword="null"/>, то событие будет вызвано обычным образом.</remarks>
        /// <exception cref="System.InvalidOperationException">Уже было завершено выполнение асинхронных операций для <paramref name="asyncOperation"/>.</exception>
        public void PostOperationCompleted(AsyncOperation asyncOperation, object sender, TEventArgs eventArgs)
        {
            if (asyncOperation == null)
            {
                On(sender, eventArgs);
            }
            else
            {
                asyncOperation.PostOperationCompleted(_callbackOnEvent, eventArgs);
            }
        }

        #endregion


        private void OnCallback(object param)
        {
            _onEvent(param as TEventArgs);
        }
    }
}