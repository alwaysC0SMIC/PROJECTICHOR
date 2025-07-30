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

#region UI EVENTS

public struct UpdateUIStyleEvent : IEvent { }

public struct UpdateUIPageEvent : IEvent {
    public Enum_UIMenuPage uiPage;
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
