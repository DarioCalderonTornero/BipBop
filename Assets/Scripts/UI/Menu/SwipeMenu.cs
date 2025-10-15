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

    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [SerializeField] private Button page1Button;
    [SerializeField] private Button page2Button;
    [SerializeField] private Button page3Button;

    private void Awake()
    {
        currentPage = 1;
        targetPos = swipeContainerRect.localPosition;
        dragThresold = Screen.width / 15;
        //UpdateBar();
        UpdateButtons();

        page1Button.onClick.AddListener(() =>
        {
            Page1();
        });

        page2Button.onClick.AddListener(() =>
        {
            Page2();
        });

        page3Button.onClick.AddListener(() =>
        {
            Page3();
        });
        
    }

    private void Page1()
    {
        if (currentPage != 1)
        {
            if (currentPage == 2)
            {
                Previous();
            }

            if (currentPage == maxPage)
            {
                currentPage -= 2;
                targetPos -= pageStep * 2;
                MovePage();
            }
        }
    }

    private void Page2()
    {
        if (currentPage != 2)
        {
            if (currentPage == 1)
            {
                Next();
            }

            if (currentPage == maxPage)
            {
                Previous();
            }
        }
    }

    private void Page3()
    {
        if (currentPage != maxPage)
        {
            if (currentPage == 1)
            {
                currentPage += 2;
                targetPos += pageStep * 2;
                MovePage();
            }

           if (currentPage == 2)
            {
                Next();
            }
        }
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
        //UpdateBar();
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
        previousButton.interactable = currentPage != 1;
        nextButton.interactable = currentPage != maxPage;
        page1Button.interactable = currentPage != 1;
        page2Button.interactable = currentPage != 2;
        page3Button.interactable = currentPage != 3;
    }
}
