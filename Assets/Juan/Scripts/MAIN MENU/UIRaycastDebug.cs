using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastDebug : MonoBehaviour
{
    private EventSystem eventSystem;

    private void Start()
    {
        eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogError(
                "UI DEBUG: NO HAY EVENT SYSTEM ACTIVO"
            );

            return;
        }

        Debug.Log(
            $"UI DEBUG READY | EventSystem: {eventSystem.name}"
        );
    }

    private void Update()
    {
        if (eventSystem == null)
            return;

        if (Mouse.current == null)
        {
            Debug.LogError(
                "UI DEBUG: NO SE DETECTA MOUSE"
            );

            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        PointerEventData pointerData =
            new PointerEventData(
                eventSystem
            );

        pointerData.position =
            mousePosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        eventSystem.RaycastAll(
            pointerData,
            results
        );

        Debug.Log(
            $"UI CLICK DEBUG | " +
            $"Mouse: {mousePosition} | " +
            $"Hits: {results.Count}"
        );

        if (results.Count == 0)
        {
            Debug.LogWarning(
                "UI DEBUG: EL MOUSE NO ESTÁ IMPACTANDO NINGÚN ELEMENTO UI"
            );

            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            Debug.Log(
                $"UI HIT {i} | " +
                $"{results[i].gameObject.name}"
            );
        }
    }
}