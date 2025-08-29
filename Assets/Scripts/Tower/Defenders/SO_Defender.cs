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
    [SerializeField] public int cost;


    [Header("Defender Info")]
    [SerializeField] public GameObject previewDefenderPrefab;
    [SerializeField] public GameObject defenderPrefab;
    [SerializeField] public float range;
    [SerializeField] public float attackSpeed;
}
