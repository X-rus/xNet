using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace xNet.Threading
{
    /// <summary>
    /// Представляет класс для выполнения операции в отдельных фоновых потоках.
    /// </summary>
    /// <typeparam name="TProgress">Тип данных значения, которое передаётся в <see cref="MultiThreadingProgressEventArgs&lt;T&gt;"/>.</typeparam>
    public class MultiThreading<TProgress> : IDisposable
    {
        #region Структуры (закрытые)

        private struct ForParams
        {
            public int Begin;
            public int End;
            public Action<MultiThreading<TProgress>, int> Action;
        }

        private struct ForEachParams<T>
        {
            public IEnumerator<T> Source;
            public Action<MultiThreading<TProgress>, T> Action;
        }

        private struct ForEachListParams<T>
        {
            public int Begin;
            public int End;
            public IList<T> List;
            public Action<MultiThreading<TProgress>, T> Action;
        }

        #endregion


        #region Поля (закрытые)

        private bool _disposed;

        private int _repsCount;
        private Barrier _barrierForReps;

        private Barrier _barrierForEndThreads;
        private ConcurrentBag<Exception> _exceptions;

        private int _threadsCount;
        private int _currentThreadsCount;

        private bool _endEnumerator;
        private bool _enableInfiniteRepeat;
        private bool _notImplementedReset;

        private bool _canceling;
        private readonly ReaderWriterLockSlim _lockForCanceling = new ReaderWriterLockSlim();

        private AsyncOperation _asyncOperation;
        private SendOrPostCallback _callbackEndWork;

        private EventHandler<EventArgs> _beginningWorkHandler;
        private AsyncEvent<EventArgs> _cancelingWorkAsyncEvent;
        private EventHandler<MultiThreadingRepeatEventArgs> _repeatCompletedHandler;
        private EventHandler<MultiThreadingCompletedEventArgs> _workCompletedAsyncEvent;
        private AsyncEvent<MultiThreadingProgressEventArgs<TProgress>> _progressChangedAsyncEvent;
        private AsyncEvent<MultiThreadingLoggingEventArgs> _logChangedkAsyncEvent;

        #endregion


        #region События (открытые)

        /// <summary>
        /// Возникает при вызове метода <see cref="Run"/>, <see cref="RunFor"/> или <see cref="RunForEach"/>.
        /// </summary>
        public event EventHandler<EventArgs> BeginningWork
        {
            add
            {
                _beginningWorkHandler += value;
            }
            remove
            {
                _beginningWorkHandler -= value;
            }
        }

        /// <summary>
        /// Возникает при отмене выполнения асинхронной операции.
        /// </summary>
        /// <remarks>Данное событие может быть вызвано автоматически, если во время выполнения асинхронной операции произойдёт исключение. Вызов происходит асинхронно.</remarks>
        public event EventHandler<EventArgs> CancelingWork
        {
            add
            {
                _cancelingWorkAsyncEvent.EventHandler += value;
            }
            remove
            {
                _cancelingWorkAsyncEvent.EventHandler -= value;
            }
        }

        /// <summary>
        /// Возникает при завершение одного из повторов выполнения асинхронной операции.
        /// </summary>
        public event EventHandler<MultiThreadingRepeatEventArgs> RepeatCompleted
        {
            add
            {
                _repeatCompletedHandler += value;
            }
            remove
            {
                _repeatCompletedHandler -= value;
            }
        }

        /// <summary>
        /// Возникает при завершение работы всех потоков.
        /// </summary>
        /// <remarks>Данное событие вызывается асинхронно.</remarks>
        public event EventHandler<MultiThreadingCompletedEventArgs> WorkCompleted
        {
            add
            {
                _workCompletedAsyncEvent += value;
            }
            remove
            {
                _workCompletedAsyncEvent -= value;
            }
        }

        /// <summary>
        /// Возникает при вызове метода <see cref="ReportProgress"/>.
        /// </summary>
        public event EventHandler<MultiThreadingProgressEventArgs<TProgress>> ProgressChanged
        {
            add
            {
                _progressChangedAsyncEvent.EventHandler += value;
            }
            remove
            {
                _progressChangedAsyncEvent.EventHandler -= value;
            }
        }

        /// <summary>
        /// Возникает при вызове метода <see cref="ReportLog"/>.
        /// </summary>
        public event EventHandler<MultiThreadingLoggingEventArgs> LogChanged
        {
            add
            {
                _logChangedkAsyncEvent.EventHandler += value;
            }
            remove
            {
                _logChangedkAsyncEvent.EventHandler -= value;
            }
        }

        #endregion


        #region Свойства (открытые)

        /// <summary>
        /// Возвращает значение, указывающие, выполняется ли асинхронная операция.
        /// </summary>
        public bool Working { get; private set; }

        /// <summary>
        /// Возвращает значение, указывающие, была ли запрошена отмена выполнения асинхронной операции.
        /// </summary>
        public bool Canceling
        {
            get
            {
                _lockForCanceling.EnterReadLock();

                try
                {
                    return _canceling;
                }
                finally
                {
                    _lockForCanceling.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Возвращает или задаёт значение, указывающие, нужно ли бесконечно выполнять асинхронную операцию.
        /// </summary>
        /// <value>Значение по умолчанию - <see langword="false"/>.</value>
        /// <exception cref="System.InvalidOperationException">Установка значения во время выполнения асинхронной операции.</exception>
        public bool EnableInfiniteRepeat
        {
            get
            {
                return _enableInfiniteRepeat;
            }
            set
            {
                #region Проверка состояния

                if (Working)
                {
                    throw new InvalidOperationException(
                        Resources.InvalidOperationException_NetProcesses_CannotSetValue);
                }

                #endregion

                _enableInfiniteRepeat = value;
            }
        }

        /// <summary>
        /// Возвращает или задаёт число потоков используемое для выполнения асинхронной операции.
        /// </summary>
        /// <value>Значение по умолчанию - 1.</value>
        /// <exception cref="System.InvalidOperationException">Установка значения во время выполнения асинхронной операции.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра меньше 1.</exception>
        public int ThreadsCount
        {
            get
            {
                return _threadsCount;
            }
            set
            {
                #region Проверка состояния

                if (Working)
                {
                    throw new InvalidOperationException(
                        Resources.InvalidOperationException_NetProcesses_CannotSetValue);
                }

                #endregion

                #region Проверка параметра

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("ThreadsCount", 1);
                }

                #endregion

                _threadsCount = value;
            }
        }

        #endregion


        /// <summary>
        /// Возвращает объект для асинхронных операций.
        /// </summary>
        protected AsyncOperation AsyncOperation
        {
            get
            {
                return _asyncOperation;
            }
        }


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiThreading&lt;TProgress&gt;"/>.
        /// </summary>
        /// <param name="threadsCount">Число потоков используемое для выполнения асинхронной операции.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="threadsCount"/> меньше 1.</exception>
        public MultiThreading(int threadsCount = 1)
        {
            #region Проверка параметров

            if (threadsCount < 1)
            {
                throw ExceptionHelper.CanNotBeLess("threadsCount", 1);
            }

            #endregion

            _threadsCount = threadsCount;

            _callbackEndWork = new SendOrPostCallback(EndWorkCallback);

            _cancelingWorkAsyncEvent = new AsyncEvent<EventArgs>(OnCancelingWork);
            _progressChangedAsyncEvent = new AsyncEvent<MultiThreadingProgressEventArgs<TProgress>>(OnProgressChanged);
            _logChangedkAsyncEvent = new AsyncEvent<MultiThreadingLoggingEventArgs>(OnLogChanged);
        }


        #region Методы (открытые)

        #region Run

        /// <summary>
        /// Запускает выполнение операции в отдельных фоновых потоках.
        /// </summary>
        /// <param name="action">Операция, которая будет выполнятся в отдельных фоновых потоках.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.InvalidOperationException">Вызов метода во время выполнения асинхронной операции.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="action"/> равно <see langword="null"/>.</exception>
        public void Run(Action<MultiThreading<TProgress>> action)
        {
            #region Проверка состояния

            ThrowIfDisposed();

            if (Working)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_MultiThreading_CannotStart);
            }

            #endregion

            #region Проверка параметров

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            #endregion

            InitBeforeRun(_threadsCount);

            try
            {
                for (int i = 0; i < _threadsCount; ++i)
                {
                    StartThread(Thread, action);
                }
            }
            catch (Exception)
            {
                EndWork();
                throw;
            }
        }

        /// <summary>
        /// Запускает выполнение цикла <see langword="for"/> в отдельных фоновых потоках. Операция выполняется на каждой итерации цикла.
        /// </summary>
        /// <param name="fromInclusive">Начальный индекс (включительный).</param>
        /// <param name="toExclusive">Конечный индекс (исключительный).</param>
        /// <param name="action">Операция, которая будет выполнятся в отдельных фоновых потоках на каждой итерации цикла.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.InvalidOperationException">Вызов метода во время выполнения асинхронной операции.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="fromInclusive"/> меньше 0.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="fromInclusive"/> больше значения параметра <paramref name="toExclusive"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="action"/> равно <see langword="null"/>.</exception>
        public void RunFor(int fromInclusive, int toExclusive, Action<MultiThreading<TProgress>, int> action)
        {
            #region Проверка состояния

            ThrowIfDisposed();

            if (Working)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_MultiThreading_CannotStart);
            }

            #endregion

            #region Проверка параметров

            if (fromInclusive < 0)
            {
                throw ExceptionHelper.CanNotBeLess("fromInclusive", 0);
            }

            if (fromInclusive > toExclusive)
            {
                throw new ArgumentOutOfRangeException("fromInclusive",
                    Resources.ArgumentException_MultiThreading_BegIndexRangeMoreEndIndex);
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            #endregion

            int range = toExclusive - fromInclusive;

            if (range == 0)
            {
                return;
            }

            int threadsCount = _threadsCount;

            if (threadsCount > range)
            {
                threadsCount = range;
            }

            InitBeforeRun(threadsCount);

            int pos = 0;
            ForParams forParams;
            int[] threadsIteration = CalculateThreadsIteration(range, threadsCount);

            try
            {
                for (int i = 0; i < threadsIteration.Length; i++)
                {
                    forParams.Action = action;

                    // Высчитываем диапазон итераций для текущего потока.
                    forParams.Begin = pos + fromInclusive;
                    forParams.End = (pos + threadsIteration[i]) + fromInclusive;

                    StartThread(ForInThread, forParams);

                    pos += threadsIteration[i];
                }
            }
            catch (Exception)
            {
                EndWork();
                throw;
            }
        }

        /// <summary>
        /// Запускает выполнение цикла <see langword="foreach"/> в отдельных фоновых потоках. Операция выполняется на каждой итерации цикла.
        /// </summary>
        /// <typeparam name="T">Тип данных в источнике.</typeparam>
        /// <param name="source">Источник данных.</param>
        /// <param name="action">Операция, которая будет выполнятся в отдельных фоновых потоках на каждой итерации цикла.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <exception cref="System.InvalidOperationException">Вызов метода во время выполнения асинхронной операции.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="source"/> равно <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="action"/> равно <see langword="null"/>.</exception>
        public void RunForEach<T>(IEnumerable<T> source, Action<MultiThreading<TProgress>, T> action)
        {
            #region Проверка состояния

            ThrowIfDisposed();

            if (Working)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_MultiThreading_CannotStart);
            }

            #endregion

            #region Проверка параметров

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            #endregion

            if (source is IList<T>)
            {
                RunForEachList<T>(source, action);
            }
            else
            {
                RunForEachOther<T>(source, action);
            }
        }

        #endregion

        #region Report

        /// <summary>
        /// Вызывает событие <see cref="ProgressChanged"/>.
        /// </summary>
        /// <param name="value">Значение передаваемое событием.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public void ReportProgress(TProgress value = default(TProgress))
        {
            ThrowIfDisposed();

            _progressChangedAsyncEvent.Post(_asyncOperation,
                this, new MultiThreadingProgressEventArgs<TProgress>(value));
        }

        /// <summary>
        /// Вызывает обычным способом событие <see cref="ProgressChanged"/>.
        /// </summary>
        /// <param name="value">Значение передаваемое событием.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <remarks>Если возможности асинхронных событий не требуются, то используйте данный метод, так как это повысит производительность.</remarks>
        public void ReportProgressSync(TProgress value = default(TProgress))
        {
            ThrowIfDisposed();

            OnProgressChanged(new MultiThreadingProgressEventArgs<TProgress>(value));
        }

        /// <summary>
        /// Вызывает событие <see cref="LogChanged"/>.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="messageType">Тип сообщения.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public void ReportLog(string message, MessageType messageType)
        {
            ThrowIfDisposed();

            _logChangedkAsyncEvent.Post(_asyncOperation,
                this, new MultiThreadingLoggingEventArgs(message, messageType));
        }

        /// <summary>
        /// Вызывает обычным способом событие <see cref="LogChanged"/>.
        /// </summary>
        /// <param name="message">Сообщение.</param>
        /// <param name="messageType">Тип сообщения.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <remarks>Если возможности асинхронных событий не требуются, то используйте данный метод, так как это повысит производительность.</remarks>
        public void ReportLogSync(string message, MessageType messageType)
        {
            ThrowIfDisposed();

            OnLogChanged(new MultiThreadingLoggingEventArgs(message, messageType));
        }

        #endregion

        /// <summary>
        /// Генерирует исключение, если значение свойства <see cref="Canceling"/> равно <see langword="true"/>.
        /// </summary>
        /// <exception cref="xNet.Threading.MultiThreadingCanceledException">Значение свойства <see cref="Canceling"/> равно <see langword="true"/>.</exception>
        /// <remarks>Генерируемое исключение поглощается автоматически, если оно произошло в методе действия.</remarks>
        public void ThrowIfCanceling()
        {
            if (Canceling)
            {
                throw new MultiThreadingCanceledException();
            }
        }

        /// <summary>
        /// Запрашивает отмену выполнения асинхронной операции и вызывает событие <see cref="CancelingWork"/>.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <remarks>Если отмена уже была запрошена, то повторного вызова события не будет.</remarks>
        public void Cancel()
        {
            ThrowIfDisposed();

            _lockForCanceling.EnterWriteLock();

            try
            {
                if (!_canceling)
                {
                    _canceling = true;
                    _cancelingWorkAsyncEvent.Post(_asyncOperation, this, EventArgs.Empty);
                }
            }
            finally
            {
                _lockForCanceling.ExitWriteLock();
            }
        }

        /// <summary>
        /// Освобождает все ресурсы, используемые текущим экземпляром класса <see cref="MultiThreading&lt;TProgress&gt;"/>.
        /// </summary>
        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _lockForCanceling.Dispose();
            }
        }

        #endregion

       
        #region Методы (защищённые)

        /// <summary>
        /// Вызывает событие <see cref="BeginningWork"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnBeginningWork(EventArgs e)
        {
            EventHandler<EventArgs> eventHandler = _beginningWorkHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// Вызывает событие <see cref="CancelingWork"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnCancelingWork(EventArgs e)
        {
            _cancelingWorkAsyncEvent.On(this, e);
        }

        /// <summary>
        /// Вызывает событие <see cref="RepeatCompleted"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnRepeatCompleted(MultiThreadingRepeatEventArgs e)
        {
            EventHandler<MultiThreadingRepeatEventArgs> eventHandler = _repeatCompletedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// Вызывает событие <see cref="WorkCompleted"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnWorkCompleted(MultiThreadingCompletedEventArgs e)
        {
            EventHandler<MultiThreadingCompletedEventArgs> eventHandler = _workCompletedAsyncEvent;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// Вызывает событие <see cref="ProgressChanged"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnProgressChanged(MultiThreadingProgressEventArgs<TProgress> e)
        {
            _progressChangedAsyncEvent.On(this, e);
        }

        /// <summary>
        /// Вызывает событие <see cref="LogChanged"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnLogChanged(MultiThreadingLoggingEventArgs e)
        {
            _logChangedkAsyncEvent.On(this, e);
        }

        #endregion


        #region Методы (закрытые)

        private void InitBeforeRun(int threadsCount, bool needCreateBarrierForReps = true)
        {
            _repsCount = 0;
            _notImplementedReset = false;
            _currentThreadsCount = threadsCount;

            if (needCreateBarrierForReps)
            {
                _barrierForReps = new Barrier(threadsCount, (b) =>
                    {
                        OnRepeatCompleted(
                            new MultiThreadingRepeatEventArgs(++_repsCount));
                    });
            }

            _barrierForEndThreads = new Barrier(threadsCount, (b) =>
                {
                    _asyncOperation.PostOperationCompleted(_callbackEndWork,
                        new MultiThreadingCompletedEventArgs(_exceptions.ToArray()));
                });

            _canceling = false;
            _exceptions = new ConcurrentBag<Exception>();
            _asyncOperation = AsyncOperationManager.CreateOperation(new object());

            Working = true;

            OnBeginningWork(EventArgs.Empty);
        }

        private void EndWork()
        {
            Working = false;
            _exceptions = null;

            _barrierForEndThreads.Dispose();
            _barrierForEndThreads = null;

            if (_barrierForReps != null)
            {
                _barrierForReps.Dispose();
                _barrierForReps = null;
            }

            _asyncOperation = null;
        }

        private void EndWorkCallback(object param)
        {
            EndWork();
            OnWorkCompleted(param as MultiThreadingCompletedEventArgs);
        }

        private int[] CalculateThreadsIteration(int iterationCount, int threadsCount)
        {
            // Список итераций для всех потоков.
            int[] threadsIteration = new int[threadsCount];

            // Число итераций для одного потока (без учёта остатка).
            int iterationOnOneThread = iterationCount / threadsCount;

            for (int i = 0; i < threadsIteration.Length; ++i)
            {
                threadsIteration[i] = iterationOnOneThread;
            }

            int index = 0;
            int balance = iterationCount -
                (threadsCount * iterationOnOneThread);

            // Распределяем оставшиеся итерации между потоками.
            for (int i = 0; i < balance; ++i)
            {
                ++threadsIteration[index];

                if (++index == threadsIteration.Length)
                {
                    index = 0;
                }
            }

            return threadsIteration;
        }

        private void StartThread(Action<object> body, object param)
        {
            var thread = new Thread(new ParameterizedThreadStart(body))
            {
                IsBackground = true
            };

            thread.Start(param);
        }

        private void RunForEachList<T>(IEnumerable<T> source, Action<MultiThreading<TProgress>, T> action)
        {
            var list = source as IList<T>;
           
            int range = list.Count;

            if (range == 0)
            {
                return;
            }

            int threadsCount = _threadsCount;

            if (threadsCount > range)
            {
                threadsCount = range;
            }

            InitBeforeRun(threadsCount);

            int pos = 0;
            ForEachListParams<T> forEachParams;
            int[] threadsIteration = CalculateThreadsIteration(range, threadsCount);

            try
            {
                for (int i = 0; i < threadsIteration.Length; i++)
                {
                    forEachParams.Action = action;
                    forEachParams.List = list;

                    // Высчитываем диапазон итераций для текущего потока.
                    forEachParams.Begin = pos;
                    forEachParams.End = pos + threadsIteration[i];

                    StartThread(ForEachListInThread<T>, forEachParams);

                    pos += threadsIteration[i];
                }
            }
            catch (Exception)
            {
                EndWork();
                throw;
            }
        }

        private void RunForEachOther<T>(IEnumerable<T> source, Action<MultiThreading<TProgress>, T> action)
        {
            _endEnumerator = false;

            InitBeforeRun(_threadsCount, false);

            ForEachParams<T> forEachParams;

            forEachParams.Action = action;
            forEachParams.Source = source.GetEnumerator();

            try
            {
                for (int i = 0; i < _threadsCount; ++i)
                {
                    StartThread(ForEachInThread<T>, forEachParams);
                }
            }
            catch (Exception)
            {
                EndWork();
                throw;
            }
        }

        #region Методы потоков

        private void Thread(object param)
        {
            var action = param as Action<MultiThreading<TProgress>>;

            try
            {
                while (!Canceling)
                {
                    action(this);

                    if (_enableInfiniteRepeat)
                    {
                        _barrierForReps.SignalAndWait();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is MultiThreadingCanceledException))
                {
                    Cancel();
                    _exceptions.Add(ex);
                }

                if (_enableInfiniteRepeat)
                {
                    // Надо сообщить, что один из потоков выбыл,
                    // чтобы остальные потоки могли продолжить работу.
                    _barrierForReps.RemoveParticipant();
                }
            }

            _barrierForEndThreads.SignalAndWait();
        }

        private void ForInThread(object param)
        {
            var forParams = (ForParams)param;

            try
            {
                do
                {
                    for (int i = forParams.Begin; i < forParams.End && !Canceling; ++i)
                    {
                        forParams.Action(this, i);
                    }

                    if (_enableInfiniteRepeat)
                    {
                        _barrierForReps.SignalAndWait();
                    }
                    else
                    {
                        break;
                    }
                } while (!Canceling) ;
            }
            catch (Exception ex)
            {
                if (!(ex is MultiThreadingCanceledException))
                {
                    Cancel();
                    _exceptions.Add(ex);
                }

                if (_enableInfiniteRepeat)
                {
                    // Надо сообщить, что один из потоков выбыл,
                    // чтобы остальные потоки могли продолжить работу.
                    _barrierForReps.RemoveParticipant();
                }
            }

            _barrierForEndThreads.SignalAndWait();
        }

        private void ForEachInThread<T>(object param)
        {
            var forEachParams = (ForEachParams<T>)param;

            try
            {
                while (!Canceling)
                {
                    T value;

                    lock (forEachParams.Source)
                    {
                        if (!forEachParams.Source.MoveNext())
                        {
                            if (_enableInfiniteRepeat && !_notImplementedReset)
                            {
                                try
                                {
                                    forEachParams.Source.Reset();
                                }
                                catch (NotImplementedException)
                                {
                                    _notImplementedReset = true;
                                    break;
                                }

                                OnRepeatCompleted(
                                    new MultiThreadingRepeatEventArgs(++_repsCount));

                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }

                        value = forEachParams.Source.Current;
                    }

                    forEachParams.Action(this, value);
                }
            }
            catch (MultiThreadingCanceledException) { }
            catch (Exception ex)
            {
                Cancel();
                _exceptions.Add(ex);
            }

            _barrierForEndThreads.SignalAndWait();
            forEachParams.Source.Dispose();
        }

        private void ForEachListInThread<T>(object param)
        {
            var forEachParams = (ForEachListParams<T>)param;
            IList<T> list = forEachParams.List;

            try
            {
                do
                {
                    for (int i = forEachParams.Begin; i < forEachParams.End && !Canceling; ++i)
                    {
                        forEachParams.Action(this, list[i]);
                    }

                    if (_enableInfiniteRepeat)
                    {
                        _barrierForReps.SignalAndWait();
                    }
                    else
                    {
                        break;
                    }
                } while (!Canceling);
            }
            catch (Exception ex)
            {
                if (!(ex is MultiThreadingCanceledException))
                {
                    Cancel();
                    _exceptions.Add(ex);
                }

                if (_enableInfiniteRepeat)
                {
                    // Надо сообщить, что один из потоков выбыл,
                    // чтобы остальные потоки могли продолжить работу.
                    _barrierForReps.RemoveParticipant();
                }
            }

            _barrierForEndThreads.SignalAndWait();
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("MultiThreading<TProgress>");
            }
        }

        #endregion
    }
}