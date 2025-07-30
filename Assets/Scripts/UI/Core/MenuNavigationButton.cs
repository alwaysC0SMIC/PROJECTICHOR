using Sirenix.OdinInspector;
using UnityEngine;

public class MenuNavigationButton : GeneralButton
{
    //VARIABLES
    [TitleGroup("Menu Navigation")]
    [SerializeField] public Enum_UIMenuPage targetPage;

    public override void OnClick()
    {
        base.OnClick();
        TriggerMenuPageChange();
    }
    
    private void TriggerMenuPageChange()
    {
        EventBus<UpdateUIPageEvent>.Raise(new UpdateUIPageEvent { uiPage = targetPage });
    }
}
