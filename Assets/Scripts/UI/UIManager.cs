//using System;
using AllIn1SpringsToolkit;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI ichorText;
    [SerializeField] private TransformSpringComponent ichorTextSpring;

    private EventBinding<AddOrRemoveIchorEvent> ichorEventBinding;

    void OnEnable()
    {
        ichorEventBinding = new EventBinding<AddOrRemoveIchorEvent>(OnIchorAmountChanged);
        EventBus<AddOrRemoveIchorEvent>.Register(ichorEventBinding);
    }



    void OnDisable()
    {
        EventBus<AddOrRemoveIchorEvent>.Deregister(ichorEventBinding);
    }


    private void OnIchorAmountChanged(AddOrRemoveIchorEvent @event)
    {
        if (@event.addOrRemove)
        {
            //ichorTextSpring.AddVelocityScale(Vector3.one * 1.1f);
            ichorTextSpring.AddVelocityRotation(Vector3.forward * 2.5f);
        }
        else
        {
            //ichorTextSpring.AddVelocityScale(Vector3.one * -1.1f);
            ichorTextSpring.AddVelocityRotation(Vector3.forward * -2.5f);
        }

        Invoke("DelayedUpdateIchorText", 0.1F);
    }

    private void DelayedUpdateIchorText()
    {
        ichorText.text = "" + gameManager.currentIchorAmount;
    }
}
