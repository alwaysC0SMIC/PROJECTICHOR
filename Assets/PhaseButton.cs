
using UnityEngine;
using UnityEngine.UI;

public class PhaseButton : MonoBehaviour
{
    //VARIABLES
    [SerializeField] public Image buttonImage;

    [SerializeField] Sprite playIcon;
    [SerializeField] Sprite pauseIcon;

    public void ChangeGameTime()
    {

        if (GameManager.Instance.currentState == GameState.Intro)
        {
            //START WAVES
            EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Playing });

            buttonImage.sprite = pauseIcon;

        }
        else if (GameManager.Instance.currentState == GameState.Playing)
        {
            //TOGGLE PAUSE
            if (GameTime.IsPaused)
            {
                //IS BEING UN PAUSED
                buttonImage.sprite = playIcon;
            }
            else
            {
                //IS BEING PAUSED
                buttonImage.sprite = pauseIcon;
            }


            GameTime.TogglePause();
        }

    }


}

