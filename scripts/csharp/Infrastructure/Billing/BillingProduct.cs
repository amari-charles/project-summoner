using Godot;

namespace Fateforged.Infrastructure.Billing;

/// <summary>
/// Data model for real-money purchasable products.
/// Field names intentionally mirror legacy GDScript keys for compatibility.
/// </summary>
[GlobalClass]
public partial class BillingProduct : Resource
{
    public enum ProductType
    {
        CONSUMABLE,
        NON_CONSUMABLE,
        SUBSCRIPTION,
    }

    [Export]
    public string product_id = "";

    [Export]
    public string internal_id = "";

    [Export]
    public string display_name = "";

    [Export]
    public string description = "";

    [Export]
    public ProductType product_type = ProductType.CONSUMABLE;

    [Export]
    public float price_usd = 0.99f;

    [Export]
    public int gems_amount;

    [Export]
    public Godot.Collections.Dictionary rewards = [];

    [Export]
    public int bonus_percent;

    [Export]
    public bool is_limited_time;

    public static BillingProduct create_gem_pack(string id, int gems, float price, int bonus = 0)
    {
        return new BillingProduct
        {
            product_id = id,
            internal_id = id,
            display_name = $"{gems} Gems",
            description = $"A pack of {gems} gems",
            product_type = ProductType.CONSUMABLE,
            price_usd = price,
            gems_amount = gems,
            bonus_percent = bonus,
        };
    }

    public static BillingProduct create_bundle(
        string id,
        string name,
        string desc,
        float price,
        Godot.Collections.Dictionary reward_dict,
        int gems = 0
    )
    {
        return new BillingProduct
        {
            product_id = id,
            internal_id = id,
            display_name = name,
            description = desc,
            product_type = ProductType.NON_CONSUMABLE,
            price_usd = price,
            gems_amount = gems,
            rewards = reward_dict,
        };
    }
}
