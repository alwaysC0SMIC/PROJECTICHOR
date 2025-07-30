using UnityEngine;

[CreateAssetMenu(menuName = "Game/Defaults/Player Profile Defaults")]
public class SO_PlayerProfileDefaults : ScriptableObject
{
    public float startingCoins = 100;
    public float startingReputation = 0;
    public int startingXP = 0;
    public int startingLevel = 1;
}
