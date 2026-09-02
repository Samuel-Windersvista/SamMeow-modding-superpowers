using UnityEngine;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

public static class LifetimeScopeExtensions
{
    public static TScope CreateChild<TScope>(
        this LifetimeScope parentScope,
        Transform parent,
        string childScopeName = null
    )
        where TScope : LifetimeScope
    {
        var gameObject = new GameObject(
            string.IsNullOrEmpty(childScopeName) ? "LifetimeScope (Child)" : childScopeName
        );
        gameObject.SetActive(false);
        gameObject.transform.SetParent(parent, false);

        var child = gameObject.AddComponent<TScope>();
        child.parentReference.Object = parentScope;
        gameObject.SetActive(true);
        return child;
    }
}
