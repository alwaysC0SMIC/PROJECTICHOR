using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class TowerClick : MonoBehaviour
{
    [SerializeField] Tower tower;

    void OnMouseUp()
    {
        //Debug.Log("Clicked on" + tower.defenderData.name);
        TowerUI.Instance.SetCurrentTower(tower);
        OnMouseExit();
    }

    void OnMouseEnter()
    {
        transform.localScale = Vector3.one * 0.6F;
    }

    void OnMouseExit()
    {
        transform.localScale = Vector3.one*0.5F;
    }
}
