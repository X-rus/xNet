using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace xNet.Threading
{
    /// <summary>
    /// Представляет класс для выполнения операции в отдельных фоновых потоках.
    /// </summary>
    public class MultiThreading : IDisposable
    {
        #region Структуры (закрытые)

        private struct ForParams
        {
            public int Begin, End;

            public Action<int> Action;
        }

        private struct ForEachParams<T>
        {
            public IEnumerator<T> Source;
            public Action<T> Action;
        }

        private struct ForEachListParams<T>
        {
            public int Begin, End;

            public IList<T> List;
            public Action<T> Action;
        }

        #endregion


        #region Поля (закрытые)

        private bool _disposed;

        private ulong _repeatCount;
        private Barrier _barrierForReps;

        private int _threadCount;
        private int _currentThreadCount;

        private bool _endEnumerator;
        private bool _enableInfiniteRepeat;
        private bool _notImplementedReset;

        private bool _canceling;
        private readonly ReaderWriterLockSlim _lockForCanceling = new ReaderWriterLockSlim();

        private object _lockForEndThread = new object();

        private AsyncOperation _asyncOperation;
        private SendOrPostCallback _callbackEndWork;

        private EventHandler<EventArgs> _beginningWorkHandler;
        private EventHandler<EventArgs> _workCompletedAsyncEvent;
        private EventHandler<MultiThreadingRepeatEventArgs> _repeatCompletedHandler;
        private AsyncEvent<MultiThreadingProgressEventArgs> _progressChangedAsyncEvent;
        private AsyncEvent<EventArgs> _cancelingWorkAsyncEvent;

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
        /// Возникает при завершение работы всех потоков.
        /// </summary>
        /// <remarks>Данное событие вызывается асинхронно.</remarks>
        public event EventHandler<EventArgs> WorkCompleted
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
        /// Возникает при вызове метода <see cref="ReportProgress"/>.
        /// </summary>
        /// <remarks>Данное событие вызывается асинхронно.</remarks>
        public event EventHandler<MultiThreadingProgressEventArgs> ProgressChanged
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
        public int ThreadCount
        {
            get
            {
                return _threadCount;
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

                _threadCount = value;
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
        /// Инициализирует новый экземпляр класса <see cref="MultiThreading"/>.
        /// </summary>
        /// <param name="threadCount">Число потоков используемое для выполнения асинхронной операции.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Значение параметра <paramref name="threadCount"/> меньше 1.</exception>
        public MultiThreading(int threadCount = 1)
        {
            #region Проверка параметров

            if (threadCount < 1)
            {
                throw ExceptionHelper.CanNotBeLess("threadCount", 1);
            }

            #endregion

            _threadCount = threadCount;

            _callbackEndWork = new SendOrPostCallback(EndWorkCallback);

            _cancelingWorkAsyncEvent = new AsyncEvent<EventArgs>(OnCancelingWork);
            _progressChangedAsyncEvent = new AsyncEvent<MultiThreadingProgressEventArgs>(OnProgressChanged);
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
        public virtual void Run(Action action)
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

            InitBeforeRun(_threadCount);

            try
            {
                for (int i = 0; i < _threadCount; ++i)
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
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Значение параметра <paramref name="fromInclusive"/> меньше 0.
        /// -или-
        /// Значение параметра <paramref name="fromInclusive"/> больше значения параметра <paramref name="toExclusive"/>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">Значение параметра <paramref name="action"/> равно <see langword="null"/>.</exception>
        public virtual void RunFor(int fromInclusive, int toExclusive, Action<int> action)
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

            int threadCount = _threadCount;

            if (threadCount > range)
            {
                threadCount = range;
            }

            InitBeforeRun(threadCount);

            int pos = 0;
            ForParams forParams;
            int[] threadsIteration = CalculateThreadsIterations(range, threadCount);

            try
            {
                for (int i = 0; i < threadsIteration.Length; i++)
                {
                    forParams.Action = action;

                    // Высчитываем индексы диапазона итераций для текущего потока.
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
        /// <exception cref="System.ArgumentNullException">
        /// Значение параметра <paramref name="source"/> равно <see langword="null"/>.
        /// -или-
        /// Значение параметра <paramref name="action"/> равно <see langword="null"/>.
        /// </exception>
        public virtual void RunForEach<T>(IEnumerable<T> source, Action<T> action)
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
        /// <param name="value">Значение передаваемое событием, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        public void ReportProgress(object value = null)
        {
            ThrowIfDisposed();

            _progressChangedAsyncEvent.Post(_asyncOperation,
                this, new MultiThreadingProgressEventArgs(value));
        }

        /// <summary>
        /// Вызывает обычным способом событие <see cref="ProgressChanged"/>.
        /// </summary>
        /// <param name="value">Значение передаваемое событием, или значение <see langword="null"/>.</param>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <remarks>Если возможности асинхронных событий не требуются, то используйте данный метод, так как это повысит производительность.</remarks>
        public void ReportProgressSync(object value = null)
        {
            ThrowIfDisposed();

            OnProgressChanged(new MultiThreadingProgressEventArgs(value));
        }

        #endregion

        /// <summary>
        /// Запрашивает отмену выполнения асинхронной операции и вызывает событие <see cref="CancelingWork"/>.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Текущий экземпляр уже был удалён.</exception>
        /// <remarks>Если отмена уже была запрошена, то повторного вызова события не будет.</remarks>
        public virtual void Cancel()
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
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

       
        #region Методы (защищённые)

        /// Освобождает неуправляемые (а при необходимости и управляемые) ресурсы, используемые объектом <see cref="MultiThreading&lt;TProgress&gt;"/>.
        /// </summary>
        /// <param name="disposing">Значение <see langword="true"/> позволяет освободить управляемые и неуправляемые ресурсы; значение <see langword="false"/> позволяет освободить только неуправляемые ресурсы.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _lockForCanceling.Dispose();
            }
        }

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
        /// Вызывает событие <see cref="WorkCompleted"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnWorkCompleted(EventArgs e)
        {
            EventHandler<EventArgs> eventHandler = _workCompletedAsyncEvent;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
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
        /// Вызывает событие <see cref="ProgressChanged"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnProgressChanged(MultiThreadingProgressEventArgs e)
        {
            _progressChangedAsyncEvent.On(this, e);
        }

        /// <summary>
        /// Вызывает событие <see cref="CancelingWork"/>.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected virtual void OnCancelingWork(EventArgs e)
        {
            _cancelingWorkAsyncEvent.On(this, e);
        }

        #endregion


        #region Методы (закрытые)

        private void InitBeforeRun(int threadCount, bool needCreateBarrierForReps = true)
        {
            _repeatCount = 0;
            _notImplementedReset = false;
            _currentThreadCount = threadCount;

            if (needCreateBarrierForReps)
            {
                _barrierForReps = new Barrier(threadCount, (b) =>
                    {
                        if (!Canceling)
                        {
                            OnRepeatCompleted(
                                new MultiThreadingRepeatEventArgs(++_repeatCount));
                        }
                    });
            }

            _canceling = false;
            _asyncOperation = AsyncOperationManager.CreateOperation(new object());

            Working = true;

            OnBeginningWork(EventArgs.Empty);
        }

        private bool EndThread()
        {
            lock (_lockForEndThread)
            {
                --_currentThreadCount;

                if (_currentThreadCount == 0)
                {
                    _asyncOperation.PostOperationCompleted(
                        _callbackEndWork, new EventArgs());

                    return true;
                }
            }

            return false;
        }

        private void EndWork()
        {
            Working = false;

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
            OnWorkCompleted(param as EventArgs);
        }

        private int[] CalculateThreadsIterations(int iterationCount, int threadsCount)
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

        #region Запуск потоков

        private void StartThread(Action<object> body, object param)
        {
            var thread = new Thread(new ParameterizedThreadStart(body))
            {
                IsBackground = true
            };

            thread.Start(param);
        }

        private void RunForEachList<T>(IEnumerable<T> source, Action<T> action)
        {
            var list = source as IList<T>;
           
            int range = list.Count;

            if (range == 0)
            {
                return;
            }

            int threadCount = _threadCount;

            if (threadCount > range)
            {
                threadCount = range;
            }

            InitBeforeRun(threadCount);

            int pos = 0;
            ForEachListParams<T> forEachParams;
            int[] threadsIteration = CalculateThreadsIterations(range, threadCount);

            try
            {
                for (int i = 0; i < threadsIteration.Length; i++)
                {
                    forEachParams.Action = action;
                    forEachParams.List = list;

                    // Высчитываем индексы диапазона итераций для текущего потока.
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

        private void RunForEachOther<T>(IEnumerable<T> source, Action<T> action)
        {
            _endEnumerator = false;

            InitBeforeRun(_threadCount, false);

            ForEachParams<T> forEachParams;

            forEachParams.Action = action;
            forEachParams.Source = source.GetEnumerator();

            try
            {
                for (int i = 0; i < _threadCount; ++i)
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

        #endregion

        #region Методы потоков

        private void Thread(object param)
        {
            var action = param as Action;

            try
            {
                while (!Canceling)
                {
                    action();

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
            catch (Exception)
            {
                Cancel();

                if (_enableInfiniteRepeat)
                {
                    _barrierForReps.RemoveParticipant();
                }

                throw;
            }
            finally
            {
                EndThread();
            }
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
                        forParams.Action(i);
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
            catch (Exception)
            {
                Cancel();

                if (_enableInfiniteRepeat)
                {
                    _barrierForReps.RemoveParticipant();
                }

                throw;
            }
            finally
            {
                EndThread();
            }
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
                        forEachParams.Action(list[i]);
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
            catch (Exception)
            {
                Cancel();

                if (_enableInfiniteRepeat)
                {
                    _barrierForReps.RemoveParticipant();
                }

                throw;
            }
            finally
            {
                EndThread();
            }
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
                        if (Canceling)
                        {
                            break;
                        }

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
                                    new MultiThreadingRepeatEventArgs(++_repeatCount));

                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }

                        value = forEachParams.Source.Current;
                    }

                    forEachParams.Action(value);
                }
            }
            catch (Exception ex)
            {
                Cancel();
            }
            finally
            {
                bool isLastThread = EndThread();

                if (isLastThread)
                {
                    forEachParams.Source.Dispose();
                }
            }
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