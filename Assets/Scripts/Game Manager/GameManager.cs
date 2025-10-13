using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private HexEnvironmentManager hexEnvironmentManager;
    //[SerializeField] private GameObject deathScreenPrefab;
    public GameState currentState;

    [Header("CURRENCY")]
    public int ichorStartingAmount = 100;
    public int currentIchorAmount;

    private EventBinding<UpdateGameStateEvent> gameStateBinding;
    private EventBinding<AddOrRemoveIchorEvent> ichorEventBinding;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        //deathScreenPrefab.SetActive(false);

        gameStateBinding = new EventBinding<UpdateGameStateEvent>(OnGameStateUpdated);
        EventBus<UpdateGameStateEvent>.Register(gameStateBinding);

        ichorEventBinding = new EventBinding<AddOrRemoveIchorEvent>(OnIchorAmountChanged);
        EventBus<AddOrRemoveIchorEvent>.Register(ichorEventBinding);
    }

    private void OnIchorAmountChanged(AddOrRemoveIchorEvent @event)
    {
        if(@event.addOrRemove)
        {
            currentIchorAmount += @event.ichorAmount;
        }
        else
        {
            currentIchorAmount = Mathf.Max(0, currentIchorAmount - @event.ichorAmount);
        }
    }

    private void OnGameStateUpdated(UpdateGameStateEvent @event)
    {
        currentState = @event.gameState;

        if(currentState == GameState.Lose)
        {
            //deathScreenPrefab.SetActive(true);
            SceneManager.LoadScene("DeathScreen");
        }
        else if(currentState == GameState.Win)
        {
            // Handle win state logic here, e.g., show win screen
            //Debug.Log("You Win!");
        }
    }

    void OnDisable()
    {
        EventBus<UpdateGameStateEvent>.Deregister(gameStateBinding);
        EventBus<AddOrRemoveIchorEvent>.Deregister(ichorEventBinding);
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
       // currentIchorAmount = ichorStartingAmount;

        EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent { addOrRemove = true, ichorAmount = ichorStartingAmount });

        EventBus<UpdateGameStateEvent>.Raise(new UpdateGameStateEvent { gameState = GameState.Intro });
        hexEnvironmentManager.GenerateTowerDefenseEnvironment();
    }

    [Button("Add 10 Ichor"), GUIColor(0.6f, 1f, 0.6f)]
    private void DebugAddIchor()
    {
        EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent { addOrRemove = true, ichorAmount = 10 });
    }

    [Button("Remove 10 Ichor"), GUIColor(1f, 0.6f, 0.6f)]
    private void DebugRemoveIchor()
    {
        EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent { addOrRemove = false, ichorAmount = 10 });
    }
}

public enum GameState
{
    Intro,
    Playing,
    Win,
    Lose
}

    

