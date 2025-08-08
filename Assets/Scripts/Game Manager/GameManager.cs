using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private HexEnvironmentManager hexEnvironmentManager;
    public GameState currentState;

    private EventBinding<UpdateGameStateEvent> gameStateBinding;

    void OnEnable()
    {
        gameStateBinding = new EventBinding<UpdateGameStateEvent>(OnGameStateUpdated);
        EventBus<UpdateGameStateEvent>.Register(gameStateBinding);
    }

    private void OnGameStateUpdated(UpdateGameStateEvent @event)
    {
        currentState = @event.gameState;
    }

    void OnDisable()
    {
        EventBus<UpdateGameStateEvent>.Deregister(gameStateBinding);
    }

    [Button("Start Game")]
    public void StartGame()
    {
        EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Playing });
    }

    void Start()
    {
        Setup();
    }

    private void Setup()
    {
        EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Intro });
        hexEnvironmentManager.GenerateTowerDefenseEnvironment();
    }
}

public enum GameState
{
    Intro,
    Playing,
    Win,
    Lose
}
