using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Enemy")]
public class SO_Enemy : ScriptableObject
{
    //VARIABLES
    [SerializeField] public string enemyName;
    [SerializeField] public float maxhealth = 100F;
    [SerializeField] public float speed = 5F;
    [SerializeField] public float damage = 10F;
    [SerializeField] public float attackRange = 2F;
    [SerializeField] public float attackRate = 1F;
    [SerializeField] public GameObject attackEffectPrefab;
}
