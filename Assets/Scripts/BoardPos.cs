using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoardPos : MonoBehaviour, IPointerDownHandler
{
    public int index;

    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager.GetInstance().PlaceCard(index, transform.position);
    }
}
