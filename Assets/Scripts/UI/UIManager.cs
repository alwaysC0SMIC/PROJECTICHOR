//using System;
using AllIn1SpringsToolkit;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI ichorText;
    [SerializeField] private TransformSpringComponent ichorTextSpring;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject gameplayScreen;

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


    void Start()
    {
        pauseScreen.SetActive(false);
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

    public void Pause()
    {
        pauseScreen.SetActive(true);
        gameplayScreen.SetActive(false);
        GameTime.Pause();
        
    }

    public void UnPause()
    {
        pauseScreen.SetActive(false);
        gameplayScreen.SetActive(true);
        GameTime.Resume();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
