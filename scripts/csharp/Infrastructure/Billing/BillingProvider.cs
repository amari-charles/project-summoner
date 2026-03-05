using Godot;

namespace Fateforged.Infrastructure.Billing;

/// <summary>
/// Abstract base class for platform-specific billing providers.
/// Signal and method names intentionally match legacy GDScript API.
/// </summary>
public partial class BillingProvider : Node
{
    [Signal]
    public delegate void purchase_completedEventHandler(string product_id, string transaction_id);

    [Signal]
    public delegate void purchase_failedEventHandler(string product_id, string error);

    [Signal]
    public delegate void purchase_cancelledEventHandler(string product_id);

    [Signal]
    public delegate void products_loadedEventHandler(Godot.Collections.Array<BillingProduct> products);

    [Signal]
    public delegate void restore_completedEventHandler(Godot.Collections.Array<string> restored_product_ids);

    [Signal]
    public delegate void restore_failedEventHandler(string error);

    public virtual void initialize()
    {
        GD.PushWarning($"BillingProvider.initialize() not implemented in {GetType().Name}");
    }

    public virtual bool is_available()
    {
        return false;
    }

    public virtual void purchase(string product_id)
    {
        GD.PushWarning($"BillingProvider.purchase() not implemented in {GetType().Name}");
        EmitSignal("purchase_failed", product_id, "Not implemented");
    }

    public virtual void restore_purchases()
    {
        GD.PushWarning($"BillingProvider.restore_purchases() not implemented in {GetType().Name}");
        EmitSignal("restore_failed", "Not implemented");
    }

    public virtual Godot.Collections.Array<BillingProduct> get_products()
    {
        return [];
    }

    public virtual string get_localized_price(string product_id)
    {
        foreach (var product in get_products())
        {
            if (product.product_id == product_id)
                return string.Format("${0:0.00}", product.price_usd);
        }

        return "";
    }

    public virtual bool is_product_owned(string product_id)
    {
        return false;
    }

    public virtual string get_provider_name()
    {
        return "BaseProvider";
    }
}
