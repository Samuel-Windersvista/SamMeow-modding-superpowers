// Compile-time facade for the VContainer build shipped inside the UnityToolkit plugin
// (com.arys.unitytoolkit). The public NuGet "VContainer" package is ancient (1.0.2,
// net7.0-only) and cannot be referenced from a .NET Framework 4.7.2 project.
// At runtime the game loads the REAL UnityToolkit VContainer.dll; this assembly exists
// only so the plugin can be compiled against the same public API surface the source uses.
// The stub is intentionally minimal: behavior is provided by the real library.

using System;
using UnityEngine;

namespace VContainer
{
    public enum Lifetime
    {
        Transient = 0,
        Singleton = 1,
        Scoped = 2,
    }

    public interface IObjectResolver
    {
        T Resolve<T>();
        object Resolve(Type type);
    }

    public interface IContainerBuilder
    {
        void RegisterInstance<TInterface>(TInterface instance);
        void RegisterBuildCallback(Action<IObjectResolver> callback);
        RegistrationBuilder Register<T>(Lifetime lifetime) where T : class;
    }

    public sealed class RegistrationBuilder
    {
        public RegistrationBuilder WithParameter(object parameter)
        {
            return this;
        }

        public RegistrationBuilder As<T>()
        {
            return this;
        }

        public RegistrationBuilder AsSelf()
        {
            return this;
        }
    }
}

namespace VContainer.Unity
{
    public sealed class ParentReference
    {
        public object Object { get; set; }
    }

    public class LifetimeScope : MonoBehaviour
    {
        public IObjectResolver Container { get; protected set; }

        protected bool autoRun;

        public ParentReference parentReference;

        protected virtual void Awake()
        {
        }

        public virtual void Build()
        {
        }

        protected virtual void Configure(IContainerBuilder builder)
        {
        }
    }
}
