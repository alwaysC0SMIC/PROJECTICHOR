using UnityEngine;

[CreateAssetMenu(fileName = "Defender", menuName = "ScriptableObjects/Defender")]
public class SO_Defender : ScriptableObject
{
    [Header("Card Info")]
    [ColorUsage(true, true)]
    public Color hdrColorForCard;
    [SerializeField] public string defenderName;
    [SerializeField] public string defenderDescription;
    [SerializeField] public Sprite defenderArt;
    [SerializeField] public int cost = 100;


    [Header("Defender Info")]
    // [SerializeField] public GameObject previewDefenderPrefab;
    // [SerializeField] public GameObject defenderPrefab;
    [SerializeField] public float defenderHealth = 100F;
    [SerializeField] public float defenderDamage = 10F;
    [SerializeField] public float range = 5F;
    [SerializeField] public float attackSpeed = 1F;
}
