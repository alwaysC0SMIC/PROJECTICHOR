using UnityEngine;

public class EventTester : MonoBehaviour
{
    #region VARIABLES
    private int health = 100;
    private int mana = 10;

    #endregion

    #region BINDING TO EVENTS

    EventBinding<TestEvent> testEventBinding;
    EventBinding<HealthEvent> healthEventBinding;

    #endregion

    #region ASSIGNING/UNASSIGNING EVENTS

    void OnEnable()
    {
        testEventBinding = new EventBinding<TestEvent>(HandleTestEvent);
        EventBus<TestEvent>.Register(testEventBinding);

        healthEventBinding = new EventBinding<HealthEvent>(HandleHealthEvent);
        EventBus<HealthEvent>.Register(healthEventBinding);
    }

    void OnDisable()
    {
        EventBus<TestEvent>.Deregister(testEventBinding);
        EventBus<HealthEvent>.Deregister(healthEventBinding);
    }

    #endregion

    //TRIGGERING EVENTS
    void Update()
    {
        //TEST EVENT TRIGGER
        if(Input.GetKeyDown(KeyCode.W))
        {
            EventBus<TestEvent>.Raise(new TestEvent());
        }

        //HEALTH EVENT TRIGGER
        if(Input.GetKeyDown(KeyCode.E))
        {
            //SET VALUES OF EVENT LIKE THIS
            EventBus<HealthEvent>.Raise(new HealthEvent(){health = health, mana = mana});
        }
    }

    #region HANDLING EVENTS

    void HandleTestEvent(){

        Debug.Log("Test event triggered!");
    }

    //TAKE EVENT AS PAREMETER IF VARIABLES ARE ATTACHED TO THE EVENT
    void HandleHealthEvent(HealthEvent healthEvent){

        //RANDOM SHIT
        Debug.Log("Health event triggered!");
        health -= 10;
        mana += 5;
        Debug.Log($"Health: {health}, Mana: {mana}");
    }

    #endregion
}
