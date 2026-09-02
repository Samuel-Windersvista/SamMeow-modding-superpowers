using HarmonyLib;
using System;
using System.Reflection;

namespace DebugTooltip
{
    internal static class R
    {
        public static void Init()
        {
            // Order is significant, as some reference each other
            UIElement.InitUITypes();
            UIInputNode.InitUITypes();
            UIContext.InitTypes();
        }

        public abstract class Wrapper(object value)
        {
            public object Value { get; protected set; } = value;
        }

        public class UIElement(object value) : Wrapper(value)
        {
            private static FieldInfo UIField;

            public static void InitUITypes()
            {
                UIField = AccessTools.Field(typeof(EFT.UI.UIElement), "UI");
            }

            public UIContext UI { get { return new UIContext(UIField.GetValue(Value)); } }
        }

        public class UIInputNode(object value) : Wrapper(value)
        {
            private static FieldInfo UIField;

            public static void InitUITypes()
            {
                UIField = AccessTools.Field(typeof(EFT.UI.UIInputNode), "UI");
            }

            public UIContext UI { get { return new UIContext(UIField.GetValue(Value)); } }
        }

        public class UIContext(object value) : Wrapper(value)
        {
            public static Type Type { get; private set; }
            private static MethodInfo AddDisposableActionMethod;

            public static void InitTypes()
            {
                Type = AccessTools.Field(typeof(EFT.UI.UIElement), "UI").FieldType;
                AddDisposableActionMethod = AccessTools.Method(Type, "AddDisposable", [typeof(Action)]);
            }

            public void AddDisposable(Action destroy) => AddDisposableActionMethod.Invoke(Value, [destroy]);
        }
    }

    internal static class RExtentensions
    {
    }
}
