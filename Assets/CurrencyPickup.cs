using DG.Tweening;
using UnityEngine;

public class CurrencyPickup : MonoBehaviour
{
    #region VARIABLES

    [Header("REFERENCES")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float growAnimationDuration = 0.35F;

    // SET BY SPAWNER
    private HexCoordinates owningTile;
    private int minCurrency;
    private int maxCurrency;
    private float lifetime;
    private System.Action<CurrencyPickup> onDespawn;

    // OPTIONAL: GLOBAL EVENT OTHER SYSTEMS CAN LISTEN TO
    // E.G. YOUR CURRENCY MANAGER CAN SUBSCRIBE TO THIS
    public static System.Action<int> OnPickupCollected;

    // SMALL FLAG SO WE DON'T DESPAWN TWICE
    private bool isDespawning;

    #endregion

    #region PROPERTIES

    public HexCoordinates OwningTile => owningTile;

    #endregion

    #region UNITY EVENTS

    private void Awake()
    {
        if (modelTransform == null && transform.childCount > 0)
            modelTransform = transform.GetChild(0);
    }

    #endregion
    #region SETUP

    // CALLED BY SPAWNER RIGHT AFTER INSTANTIATE
    public void Setup(
        HexCoordinates tile,
        int minAmount,
        int maxAmount,
        float lifeSeconds,
        System.Action<CurrencyPickup> despawnCallback
    )
    {
        owningTile = tile;
        minCurrency = minAmount;
        maxCurrency = maxAmount;
        lifetime = lifeSeconds;
        onDespawn = despawnCallback;

        // START LIFETIME TIMER
        Invoke(nameof(DespawnBecauseTimeout), lifetime);

        // SPAWN ANIMATION
        if (modelTransform != null)
        {
            modelTransform.localScale = Vector3.zero;
            modelTransform.DOScale(Vector3.one, growAnimationDuration).SetEase(Ease.OutBack);
        }
    }

    #endregion

    #region INPUT

    // SIMPLE CLICK HANDLING
    private void OnMouseDown()
    {
        // NOTE: IF YOU USE A DIFFERENT INPUT SYSTEM / RAYCASTER, CALL Collect() FROM THERE INSTEAD
        Collect();
    }

    #endregion

    #region BEHAVIOUR

    private void Collect()
    {
        if (isDespawning)
            return;

        isDespawning = true;

        // ROLL CURRENCY
        int amount = Random.Range(minCurrency, maxCurrency + 1);

        // FIRE GLOBAL EVENT
        EventBus<AddOrRemoveIchorEvent>.Raise(new AddOrRemoveIchorEvent { addOrRemove = true, ichorAmount = amount });
        OnPickupCollected?.Invoke(amount);

        // INFORM SPAWNER SO IT CAN FREE THE TILE
        onDespawn?.Invoke(this);

        // DESTROY SELF
        Destroy(gameObject);
    }

    private void DespawnBecauseTimeout()
    {
        if (isDespawning)
            return;

        isDespawning = true;

        // JUST TELL SPAWNER TO FREE TILE, NO CURRENCY
        onDespawn?.Invoke(this);

        Destroy(gameObject);
    }

    #endregion
}
