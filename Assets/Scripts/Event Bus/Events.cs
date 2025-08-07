using UnityEngine;
using System;

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

#region BUILDING EVENTS

public struct BuildingEvent : IEvent
{
    public bool isBuilding;
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
