using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    //VARIABLES
    [SerializeField] private int cardsToGenerate = 6;
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject tempCardGap;
    [SerializeField] private UIBlock Root;
    [SerializeField] private UIBlock ParentRoot;
    [SerializeField] private UIBlock background;
    [SerializeField] private float showCardsDuration = 0.5F;
    //[SerializeField] private Scroller scroller;
    public List<GameObject> wholeHand = new List<GameObject>();
    private bool isScrolling = false;

    private bool canScroll = false;

    private bool CanScrollCards()
    {
        return wholeHand.Count > maxHandSize;
    }

    [BoxGroup("Debug Controls")]
    [Button("Scroll Left", ButtonSizes.Medium)]
    private void TestScrollLeft()
    {
        if (CanScrollCards())
            ScrollLeft();
    }

    [BoxGroup("Debug Controls")]
    [Button("Scroll Right", ButtonSizes.Medium)]
    private void TestScrollRight()
    {
        if (CanScrollCards())
            ScrollRight();
    }


    void Start()
    {
        wholeHand.Clear();

        if (Root != null)
        {
            ParentRoot.AddGestureHandler<Gesture.OnHover>(OnHover);
            ParentRoot.AddGestureHandler<Gesture.OnUnhover>(OnExitHover);
        }

        InitializeHand();
        HideCards();
    }

    void Update()
    {
        if (canScroll && CanScrollCards())
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput > 0f)
            {
                // Scroll up - trigger scroll left
                ScrollLeft();
            }
            else if (scrollInput < 0f)
            {
                // Scroll down - trigger scroll right
                ScrollRight();
            }
        }
    }


    private void OnHover(Gesture.OnHover e)
    {
        ShowCards();
    }


    private void OnExitHover(Gesture.OnUnhover e)
    {
        HideCards();
    }


    public void ShowCards()
    {
        canScroll = true;
        ChangePosition(true);
    }

    public void HideCards()
    {
        canScroll = false;
        ChangePosition(false);
    }

    public void ChangePosition(bool show)
    {
        float targetY = show ? 0f : -0.5f;
        float currentY = Root.Layout.Position.Y.Percent;

        DOTween.To(() => currentY, y =>
        {
            var position = Root.Layout.Position;
            position.Y = Length.Percentage(y);
            Root.Layout.Position = position;
            background.Layout.Position = position;
            Root.CalculateLayout();
        }, targetY, showCardsDuration).SetEase(Ease.OutQuad);

        Canvas.ForceUpdateCanvases();
    }

    public void InitializeHand()
    {
        //CLEAR PREVIOUS HAND
        for (int i = 0; i < cardsToGenerate; i++)
        {
            GameObject card = Instantiate(cardPrefab, Root.transform);
            card.GetComponent<CardUI>().ScaleUp();

            card.GetComponent<UIBlock2D>().Color = new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                1f
            );

            wholeHand.Add(card);
            card.name = "Card " + (i + 1);
        }

        EnableHandCards();

    }

    public void EnableHandCards()
    {
        //ENABLE HAND CARDS
        for (int i = 0; i < wholeHand.Count; i++)
        {
            if (i < maxHandSize)
            {
                wholeHand[i].SetActive(true);
                wholeHand[i].GetComponent<CardUI>().ScaleUp();
            }
            else
            {
                wholeHand[i].GetComponent<CardUI>().ScaleDown();
                wholeHand[i].SetActive(false);
            }
        }
    }

    public void ActiveCheckForHand()
    {
        //ENABLE HAND CARDS
        for (int i = 0; i < wholeHand.Count; i++)
        {
            if (i < maxHandSize)
            {
                wholeHand[i].SetActive(true);
            }
            else
            {
                wholeHand[i].SetActive(false);
            }
        }
    }

    public void Scroll(Gesture.OnScroll gesture)
    {


        // Handle scroll events here
        Debug.Log("SCROLLING: " + gesture.ScrollDeltaLocalSpace);

        if (gesture.ScrollDeltaLocalSpace.y > 0)
        {
            // Scroll up logic

            ScrollLeft();
        }
        else if (gesture.ScrollDeltaLocalSpace.y < 0)
        {
            // Scroll down logic

            ScrollRight();
        }

    }

    public void ScrollLeft()
    {
        if (!isScrolling && CanScrollCards())
        {
            StartCoroutine(ScrollLeftCoroutine());
        }
    }

    private IEnumerator ScrollLeftCoroutine()
    {
        isScrolling = true;

        //LOWEST CARD IN BATCH IN HIERARCHY GETS DISABLED
        ChangeCardIndex(wholeHand.Count - 1, 0);
        wholeHand[0].SetActive(true);
        wholeHand[0].GetComponent<CardUI>().ScaleUp();
        wholeHand[maxHandSize].GetComponent<CardUI>().ScaleDown();
        yield return StartCoroutine(wholeHand[wholeHand.Count - 1].GetComponent<CardUI>().ScaleDownCoroutine(() =>
        {
        }));

        //ActiveCheckForHand();
        isScrolling = false;
    }

    private IEnumerator ScrollRightCoroutine()
    {
        isScrolling = true;

        //LOWEST CARD IN BATCH IN HIERARCHY GETS DISABLED
        wholeHand[maxHandSize].SetActive(true);
        wholeHand[maxHandSize].GetComponent<CardUI>().ScaleUp();

        yield return StartCoroutine(wholeHand[0].GetComponent<CardUI>().ScaleDownCoroutine(() =>
        {
            //wholeHand[maxHandSize - 1].GetComponent<CardUI>().ScaleDown();
            ChangeCardIndex(0, wholeHand.Count - 1);

        }));

        //ActiveCheckForHand();
        isScrolling = false;
    }

    public void ScrollRight()
    {
        if (!isScrolling && CanScrollCards())
        {
            //MOVE 1st card in hierarchy to end of list
            StartCoroutine(ScrollRightCoroutine());
        }

        //Next card in hierarchy after batch joins

    }

    public void ChangeCardIndex(int oldIndex, int newIndex)
    {
        GameObject target = wholeHand[oldIndex];
        wholeHand.RemoveAt(oldIndex);
        wholeHand.Insert(newIndex, target);

        for (int i = 0; i < wholeHand.Count; i++)
        {
            wholeHand[i].transform.SetSiblingIndex(i);
        }
    }

}
