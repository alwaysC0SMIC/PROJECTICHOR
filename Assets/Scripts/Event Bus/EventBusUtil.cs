
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EventBusUtil
{
   public static IReadOnlyList<Type> EventTypes{get; set;}
   public static IReadOnlyList<Type> EventBusTypes{get; set;}

    //CLEARS GARBAGE IN UNITY EDITOR (STOPS MEMORY LEAK)
    #if UNITY_EDITOR
    public static PlayModeStateChange PlayModeState{get; set;}
    [InitializeOnLoadMethod]
    public static void InitializeEditor(){

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        PlayModeState = state;
        if(state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearAllBuses();
        }
    }

    #endif

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   public static void Initialize()
   {
      EventTypes = PredefinedAssemblyUtil.GetTypes(typeof(IEvent));
        EventBusTypes = InitializeAllBuses();
   }

   static List<Type> InitializeAllBuses()
   {
      List<Type> eventBusTypes = new List<Type>();

      var typedef = typeof(EventBus<>);
      foreach (var eventType in EventTypes)
      {
         var busType = typedef.MakeGenericType(eventType);
         eventBusTypes.Add(busType);

         //Debug.Log($"Initialized EventBus Type: <{eventType.Name}>");
      }

      return eventBusTypes;
   }

   public static void ClearAllBuses(){
        //Debug.Log("Clearing all event buses...");
        for(int i = 0; i < EventBusTypes.Count; i++)
        {
            var busType = EventBusTypes[i];
            var clearMethod = busType.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            clearMethod.Invoke(null, null);
        }
   }
}
