// Compile-time facade for the UNITY build of UniTask (Cysharp.Threading.Tasks),
// as shipped inside the UnityToolkit plugin (com.arys.unitytoolkit).
//
// The NuGet builds of UniTask are compiled WITHOUT the UNITY define and therefore
// lack the Unity-dependent members used by the mod source:
//   UniTask.SwitchToMainThread / UniTask.Yield / Timeout / DelayType
// At runtime the game loads the REAL UnityToolkit UniTask.dll; this assembly exists
// only so the plugin can be compiled against the same public API surface the source uses.
// The stub is intentionally minimal: behavior is provided by the real library.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    // Needed on net4x targets where the BCL lacks this attribute.
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
        AttributeTargets.Delegate | AttributeTargets.Enum,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
}

namespace Cysharp.Threading.Tasks
{
    public enum DelayType
    {
        DeltaTime,
        UnscaledDeltaTime,
        IgnoreTimeScale,
        Realtime,
    }

    public interface IUniTaskSource
    {
    }

    public interface IUniTaskSource<out T>
    {
    }

    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
    public readonly struct UniTask
    {
        internal readonly IUniTaskSource Source;
        internal readonly short Token;

        public UniTask(IUniTaskSource source, short token)
        {
            Source = source;
            Token = token;
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        public static UniTask Yield(CancellationToken cancellationToken = default)
        {
            return new UniTask(new CompletedSource(), 0);
        }

        public static Awaiter SwitchToMainThread(CancellationToken cancellationToken = default)
        {
            return new Awaiter(default);
        }

        public static UniTask<T> FromResult<T>(T value)
        {
            return new UniTask<T>(new CompletedSource<T>(value), 0);
        }

        public static UniTask<T> FromException<T>(Exception exception)
        {
            return new UniTask<T>(new FaultedSource<T>(exception), 0);
        }

        public struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly UniTask _task;

            public Awaiter(UniTask task)
            {
                _task = task;
            }

            public bool IsCompleted => true;

            public Awaiter GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
            }

            public void OnCompleted(Action continuation)
            {
            }

            public void UnsafeOnCompleted(Action continuation)
            {
            }
        }
    }

    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder<>))]
    public readonly struct UniTask<T>
    {
        internal readonly IUniTaskSource<T> Source;
        internal readonly short Token;

        public UniTask(IUniTaskSource<T> source, short token)
        {
            Source = source;
            Token = token;
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }

        public struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly UniTask<T> _task;

            public Awaiter(UniTask<T> task)
            {
                _task = task;
            }

            public bool IsCompleted => true;

            public Awaiter GetAwaiter()
            {
                return this;
            }

            public T GetResult()
            {
                return default;
            }

            public void OnCompleted(Action continuation)
            {
            }

            public void UnsafeOnCompleted(Action continuation)
            {
            }
        }
    }

    internal sealed class CompletedSource : IUniTaskSource
    {
    }

    internal sealed class CompletedSource<T> : IUniTaskSource<T>
    {
        public CompletedSource(T result)
        {
        }
    }

    internal sealed class FaultedSource<T> : IUniTaskSource<T>
    {
        public FaultedSource(Exception exception)
        {
        }
    }

    public struct AsyncUniTaskMethodBuilder
    {
        public static AsyncUniTaskMethodBuilder Create()
        {
            return default;
        }

        public UniTask Task => default;

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetException(Exception exception)
        {
        }

        public void SetResult()
        {
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    public struct AsyncUniTaskMethodBuilder<T>
    {
        public static AsyncUniTaskMethodBuilder<T> Create()
        {
            return default;
        }

        public UniTask<T> Task => default;

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetException(Exception exception)
        {
        }

        public void SetResult(T result)
        {
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    public static class UniTaskExtensions
    {
        public static UniTask AsUniTask(this Task task, bool useCurrentSynchronizationContext = true)
        {
            return new UniTask(new CompletedSource(), 0);
        }

        public static UniTask<T> AsUniTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
        {
            return new UniTask<T>(new CompletedSource<T>(default), 0);
        }

        public static Task AsTask(this UniTask task)
        {
            return Task.CompletedTask;
        }

        public static Task<T> AsTask<T>(this UniTask<T> task)
        {
            return Task.FromResult(default(T));
        }

        public static UniTask<T> Timeout<T>(
            this UniTask<T> task,
            TimeSpan timeout,
            DelayType delayType = DelayType.DeltaTime,
            CancellationTokenSource taskCancellationTokenSource = null)
        {
            return task;
        }
    }
}
