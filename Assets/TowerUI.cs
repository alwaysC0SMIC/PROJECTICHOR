using System;
using NUnit.Framework;
using TMPro;
using DG.Tweening;
using UnityEngine;
using AllIn1SpringsToolkit;


public class TowerUI : MonoBehaviour
{
    public static TowerUI Instance;

    void Awake()
    {
        if (Instance != this)
        {
            Destroy(Instance);
            Instance = this;
        }
    }

    public Tower currentSelectedTower;
    [SerializeField] GameObject uiRoot;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text upgradeText;
    public int upgradeAmount = 50;
    [SerializeField] TMP_Text sellText;
    public int sellAmount = 50;
    private bool canUpgrade = true;

    [SerializeField] TransformSpringComponent notEnoughMoneySpring;

    void Start()
    {
        ShowUI(false);
    }

    public void SetCurrentTower(Tower newSelectedTower)
    {
        currentSelectedTower = newSelectedTower;

        UpdateUI();
        ShowUI(true);
    }

    private void UpdateUI()
    {
        levelText.text = "Level " + currentSelectedTower.towerLevel;

        upgradeAmount =   currentSelectedTower.towerLevel*50;
        upgradeText.text = "Upgrade -" + upgradeAmount;

        sellAmount = currentSelectedTower.towerLevel * 25;
        sellText.text = "Sell +" + sellAmount;
        
    }

    public void CloseUI()
    {
        ShowUI(false);
    }

    public void UpgradeTower()
    {
        if (!canUpgrade) return;

        if (GameManager.Instance.currentIchorAmount >= upgradeAmount)
        {
            EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent { addOrRemove = false, ichorAmount = upgradeAmount });
            currentSelectedTower.UpgradeTower(currentSelectedTower.towerLevel + 1);
            UpdateUI();
        }
        else
        {
            NotEnoughMoneyPrompt();
        }
        
        canUpgrade = false;
        Invoke(nameof(ResetUpgradeCooldown), 0.2f);
    }

    private void ResetUpgradeCooldown()
    {
        canUpgrade = true;
    }

    public void SellTower()
    {
        currentSelectedTower.ResetTowerLevel();
        currentSelectedTower.TakeDamage(999999999999999999999F);
        EventBus<AddOrRemoveIchorEvent>.Raise( new AddOrRemoveIchorEvent {  addOrRemove =  true, ichorAmount = sellAmount} ); 
        CloseUI();
    }

    private void NotEnoughMoneyPrompt()
    {
        notEnoughMoneySpring.AddVelocityRotation(Vector3.one);
        //upgradeText.GetComponentInParent<RectTransform>().DOShakeRotation(0.25f, new Vector3(0, 0, 45), 15);
    }

    public void ShowUI(bool show)
    {
        if (show)
        {
            uiRoot.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad);
        }
        else
        {
            uiRoot.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad);
        }
    }
}
