using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Fateforged.Infrastructure.Billing;

public partial class StubBillingProvider : BillingProvider
{
    public enum Mode
    {
        AUTO_SUCCESS,
        AUTO_FAIL,
        AUTO_CANCEL,
        INTERACTIVE,
    }

    private Mode _mode = Mode.AUTO_SUCCESS;
    private float _simulate_delay = 1.0f;
    private string _fail_message = "Simulated billing failure";

    private Godot.Collections.Array<BillingProduct> _products = [];
    private readonly HashSet<string> _owned_products = [];
    private int _transaction_counter;

    public override void initialize()
    {
        _load_stub_products();
        GD.Print($"[StubBilling] Initialized with {_products.Count} products");
    }

    public override bool is_available()
    {
        return true;
    }

    public override async void purchase(string product_id)
    {
        GD.Print($"[StubBilling] Purchase requested: {product_id} (mode: {_mode})");

        await _delay_seconds(_simulate_delay);

        switch (_mode)
        {
            case Mode.AUTO_SUCCESS:
                _complete_purchase(product_id);
                break;
            case Mode.AUTO_FAIL:
                EmitSignal("purchase_failed", product_id, _fail_message);
                break;
            case Mode.AUTO_CANCEL:
                EmitSignal("purchase_cancelled", product_id);
                break;
            default:
                _complete_purchase(product_id);
                break;
        }
    }

    public override async void restore_purchases()
    {
        GD.Print("[StubBilling] Restore requested");
        await _delay_seconds(0.5f);

        var restored = new Godot.Collections.Array<string>();
        foreach (var productId in _owned_products)
            restored.Add(productId);

        GD.Print($"[StubBilling] Restored {restored.Count} purchases");
        EmitSignal("restore_completed", restored);
    }

    public override Godot.Collections.Array<BillingProduct> get_products()
    {
        return _products;
    }

    public override string get_localized_price(string product_id)
    {
        var product = _find_product(product_id);
        return product != null ? string.Format("${0:0.00}", product.price_usd) : "";
    }

    public override bool is_product_owned(string product_id)
    {
        return _owned_products.Contains(product_id);
    }

    public override string get_provider_name()
    {
        return "StubBilling";
    }

    public void set_mode(int mode)
    {
        if (Enum.IsDefined(typeof(Mode), mode))
            _mode = (Mode)mode;
        GD.Print($"[StubBilling] Mode set to: {_mode}");
    }

    public void set_delay(float delay)
    {
        _simulate_delay = Mathf.Max(0.0f, delay);
    }

    public void set_fail_message(string message)
    {
        _fail_message = message;
    }

    public void simulate_own_product(string product_id)
    {
        _owned_products.Add(product_id);
    }

    public void clear_owned_products()
    {
        _owned_products.Clear();
    }

    private void _complete_purchase(string product_id)
    {
        _transaction_counter += 1;
        var unix = Time.GetUnixTimeFromSystem();
        var transactionId = $"stub_txn_{unix}_{_transaction_counter}";

        var product = _find_product(product_id);
        if (product != null && product.product_type == BillingProduct.ProductType.NON_CONSUMABLE)
            _owned_products.Add(product_id);

        GD.Print($"[StubBilling] Purchase completed: {product_id} -> {transactionId}");
        EmitSignal("purchase_completed", product_id, transactionId);
    }

    private BillingProduct? _find_product(string product_id)
    {
        foreach (var product in _products)
        {
            if (product.product_id == product_id)
                return product;
        }

        return null;
    }

    private void _load_stub_products()
    {
        var catalog = GetCatalog();
        if (catalog != null)
        {
            _products = catalog.get_all_products();
            EmitSignal("products_loaded", _products);
            return;
        }

        _products =
        [
            BillingProduct.create_gem_pack("gems_100", 100, 0.99f),
            BillingProduct.create_gem_pack("gems_500", 550, 4.99f, 10),
            BillingProduct.create_gem_pack("gems_1200", 1320, 9.99f, 10),
            BillingProduct.create_gem_pack("gems_2500", 2750, 19.99f, 10),
            BillingProduct.create_bundle(
                "starter_pack",
                "Starter Pack",
                "200 Gems + Lightning Adept Summoner",
                4.99f,
                new Godot.Collections.Dictionary
                {
                    ["summoners"] = new Godot.Collections.Array<string> { "lightning_adept" },
                },
                200
            ),
        ];

        EmitSignal("products_loaded", _products);
    }

    private async Task _delay_seconds(float seconds)
    {
        if (!IsInsideTree() || seconds <= 0.0f)
            return;

        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
