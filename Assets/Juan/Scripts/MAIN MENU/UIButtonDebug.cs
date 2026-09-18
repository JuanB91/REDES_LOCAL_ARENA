using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonDebug :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(
        PointerEventData eventData)
    {
        Debug.Log(
            $"MOUSE ENTERED: {gameObject.name}"
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        Debug.Log(
            $"MOUSE EXITED: {gameObject.name}"
        );
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        Debug.Log(
            $"BUTTON CLICKED: {gameObject.name}"
        );
    }
}