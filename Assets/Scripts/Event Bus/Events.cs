using UnityEngine;
using System;
using System.Collections.Generic;

public interface IEvent {}

//STRUCTS ARE ALLOCATED ON THE STACK, NOT THE HEAP
// AND ARE MORE MEMORY EFFICIENT THAN CLASSES

public struct TestEvent : IEvent {   }

public struct HealthEvent : IEvent {  
    public int health;
    public int mana;
 }

public struct OnGameStateLoaded : IEvent
{
    public SaveData loadedData;
}

#region CAMERA EVENTS

public struct ToggleGameplayCamEvent : IEvent
{
    public bool allowCam;
}

public struct ResetCameraEvent : IEvent
{ }

#endregion

#region UI EVENTS

public struct UpdateUIStyleEvent : IEvent { }

public struct UpdateUIPageEvent : IEvent
{
    public Enum_UIMenuPage uiPage;
}


#endregion

public struct EnvironmentGeneratedEvent : IEvent{}

public struct AddOrRemoveIchorEvent : IEvent
{
    public bool addOrRemove;
    public int ichorAmount;
}

public struct CentreTowerAttackEvent : IEvent
{
    public float damageAmount;
}

public struct UpdateGameStateEvent : IEvent
{
    public GameState gameState;
}

#region BUILDING EVENTS

public struct BuildingEvent : IEvent
{
    public SO_Defender defenderToBuild;
    public bool isBuilding;
}

public struct PathwayTransformsEvent : IEvent
{
    public List<List<Transform>> pathwayTransformsByLane;
}

#endregion

#region AUDIO

public struct AudioEvent : IEvent
{
    public AudioTrigger id;

    public AudioEvent(AudioTrigger inID)
    {
        id = inID;
    }
}

#endregion
