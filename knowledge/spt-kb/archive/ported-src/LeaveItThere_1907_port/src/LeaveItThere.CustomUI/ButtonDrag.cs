using UnityEngine;
using UnityEngine.EventSystems;

namespace LeaveItThere.CustomUI;

public class ButtonDrag : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
{
	private RectTransform _draggableRect;

	private Vector2 _clickPos;

	public void Init(RectTransform rectToDrag)
	{
		_draggableRect = rectToDrag;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_draggableRect, eventData.position, eventData.pressEventCamera, out _clickPos);
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 val = default(Vector2);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_draggableRect, eventData.position, eventData.pressEventCamera, out val);
		((Transform)_draggableRect).position = ((Component)_draggableRect).transform.TransformPoint(val - _clickPos);
		RectTransformExtensions.CorrectPositionResolution(_draggableRect, default(MarginsRect));
	}
}
