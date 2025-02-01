using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [SerializeField] private Image[] barImage;

    [SerializeField] private Sprite barClosed, barOpen;

    [SerializeField] private Button previousButton, nextButton;

    private void Awake()
    {
        currentPage = 1;
        targetPos = swipeContainerRect.localPosition;
        dragThresold = Screen.width / 2;
        UpdateBar();
        UpdateButtons();
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

    private void MovePage()
    {
        swipeContainerRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        UpdateBar();
        UpdateButtons();
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

    private void UpdateBar()
    {
        foreach(var item in barImage)
        {
            item.sprite = barClosed;
        }
        barImage[currentPage - 1].sprite = barOpen;
    }

    private void UpdateButtons()
    {
        previousButton.interactable = true;
        nextButton.interactable = true;

        if (currentPage == 1)
            previousButton.interactable = false;
        else if (currentPage == maxPage)
            nextButton.interactable = false;    
    }
}
