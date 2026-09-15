using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonDebug : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("MOUSE ENTRO AL BOTON");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("MOUSE SALIO DEL BOTON");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLICK DETECTADO EN EL BOTON");
    }
}