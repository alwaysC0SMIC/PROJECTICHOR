using System.ComponentModel;
using UnityEngine;

public abstract class AttackEffect : MonoBehaviour, ITarget
{
    [SerializeField] public Transform target;
    public bool isInitialized = false;

    

    // public void InitializeAttack(Transform inTarget)
    // {
    //     target = inTarget;
    //     isInitialized = true;
    // }

    public abstract void AttackStart();

    public void SetTarget(Transform target)
    {
        target = target;
        isInitialized = true;
    }
}
