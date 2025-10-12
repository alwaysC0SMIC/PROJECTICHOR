using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemy")]
public class SO_Enemy : ScriptableObject
{
    [TitleGroup("General Info")]
    [SerializeField] public string enemyName = "New Enemy";

    [TitleGroup("Combat Stats")]
    [SerializeField] public float maxhealth = 100f;
    [SerializeField] public float speed = 5f;
    [SerializeField] public float damage = 10f;
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackRate = 1f;
    [SerializeField] public GameObject attackEffectPrefab;

    [TitleGroup("Wave System")]
    [Tooltip("The cost of this enemy for the procedural wave budget. Higher cost means a greater threat.")]
    [MinValue(1)]
    [SerializeField] public int threatCost = 10;
}
