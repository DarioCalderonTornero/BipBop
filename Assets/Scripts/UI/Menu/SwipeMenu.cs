using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeMenu : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private int maxPage;
    int currentPage;
    Vector3 targetPos;
    [SerializeField] private Vector3 pageStep;
    [SerializeField] private RectTransform swipeContainerRect;

    [SerializeField] private float dragThresold;

    [SerializeField] private float tweenTime;
    [SerializeField] private LeanTweenType tweenType;

    private void Awake()
    {
        currentPage = 1;
        targetPos = swipeContainerRect.localPosition;
        dragThresold = Screen.width / 2;
    }

    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
    }

    void MovePage()
    {
        swipeContainerRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThresold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();

        }

        else
        {
            MovePage();
        }
    }
}
