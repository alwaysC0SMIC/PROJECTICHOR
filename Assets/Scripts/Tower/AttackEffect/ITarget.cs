using UnityEngine;

public interface ITarget
{
    //public Transform Target { get; set; }

    void SetTarget(Transform target, float damageAmount = 100f);
}
