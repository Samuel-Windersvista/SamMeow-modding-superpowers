namespace SamSWAT.HeliCrash.ArysReloaded;

public interface IEvent;

public delegate void EventHandler<TEvent>(ref TEvent @event)
    where TEvent : struct, IEvent;

public static class EventDispatcher<TEvent>
    where TEvent : struct, IEvent
{
    private static event EventHandler<TEvent> OnEvent;

    public static void Dispatch(ref TEvent @event)
    {
        OnEvent?.Invoke(ref @event);
    }

    public static void Subscribe(EventHandler<TEvent> handler)
    {
        OnEvent += handler;
    }

    public static void Unsubscribe(EventHandler<TEvent> handler)
    {
        OnEvent -= handler;
    }

    public static void UnsubscribeAll()
    {
        OnEvent = null;
    }
}
