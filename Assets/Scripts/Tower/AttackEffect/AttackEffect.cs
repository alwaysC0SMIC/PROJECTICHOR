using System.ComponentModel;
using UnityEngine;

public abstract class AttackEffect : MonoBehaviour, ITarget
{
    [SerializeField] public Transform target;
    public bool isInitialized = false;
    public float damageAmount = 100f; // Default damage amount

    // public void InitializeAttack(Transform inTarget)
    // {
    //     target = inTarget;
    //     isInitialized = true;
    // }

    public abstract void AttackStart();

    public void SetTarget(Transform intarget, float indamageAmount = 100)
    {
        damageAmount = indamageAmount;
        target = intarget;
        isInitialized = true;
    }
}
