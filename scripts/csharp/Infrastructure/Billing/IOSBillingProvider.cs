using System.Collections.Generic;
using Godot;

namespace Fateforged.Infrastructure.Billing;

public partial class IOSBillingProvider : BillingProvider
{
    private const string IOS_SINGLETON_NAME = "InAppStore";

    private bool _available;
    private GodotObject? _store_api;
    private Godot.Collections.Array<BillingProduct> _products = [];
    private readonly HashSet<string> _owned_products = [];

    public override void initialize()
    {
        _available = Engine.HasSingleton(IOS_SINGLETON_NAME);
        if (_available)
        {
            _store_api = Engine.GetSingleton(IOS_SINGLETON_NAME);
            _connect_platform_signals();
        }
        else
        {
            GD.PushWarning(
                "[iOSBilling] InAppStore singleton not found. Ensure iOS IAP plugin is enabled."
            );
        }

        _load_products_from_catalog();
        GD.Print($"[iOSBilling] Initialized (available={_available}, products={_products.Count})");
    }

    public override bool is_available()
    {
        return _available;
    }

    public override void purchase(string product_id)
    {
        if (!_available || _store_api == null)
        {
            EmitSignal("purchase_failed", product_id, "iOS billing is unavailable");
            return;
        }

        var storeProductId = _map_product_id(product_id);
        if (string.IsNullOrEmpty(storeProductId))
        {
            EmitSignal(
                "purchase_failed",
                product_id,
                $"Missing App Store product mapping for '{product_id}'"
            );
            return;
        }

        if (_store_api.HasMethod("purchase"))
            _store_api.Call("purchase", storeProductId);
        else
            EmitSignal(
                "purchase_failed",
                product_id,
                "iOS billing API is not wired (missing purchase)"
            );
    }

    public override void restore_purchases()
    {
        if (!_available || _store_api == null)
        {
            EmitSignal("restore_failed", "iOS billing is unavailable");
            return;
        }

        if (_store_api.HasMethod("restore_purchases"))
            _store_api.Call("restore_purchases");
        else if (_store_api.HasMethod("restore"))
            _store_api.Call("restore");
        else
            EmitSignal("restore_failed", "iOS restore is not wired");
    }

    public override Godot.Collections.Array<BillingProduct> get_products()
    {
        return _products;
    }

    public override bool is_product_owned(string product_id)
    {
        return _owned_products.Contains(product_id);
    }

    public override string get_provider_name()
    {
        return "IOSBilling";
    }

    private void _load_products_from_catalog()
    {
        var catalog = GetCatalog();
        _products = catalog?.get_all_products() ?? [];
        EmitSignal("products_loaded", _products);
    }

    private string _map_product_id(string product_id)
    {
        var catalog = GetCatalog();
        return catalog?.get_platform_product_id(product_id, "ios") ?? "";
    }

    private void _connect_platform_signals()
    {
        _connect_if_present(
            "purchase_success",
            Callable.From<string, string>(_on_purchase_success)
        );
        _connect_if_present("purchase_failed", Callable.From<string, string>(_on_purchase_failed));
        _connect_if_present("purchase_cancelled", Callable.From<string>(_on_purchase_cancelled));
        _connect_if_present(
            "restore_completed",
            Callable.From<Godot.Collections.Array>(_on_restore_completed)
        );
        _connect_if_present("restore_failed", Callable.From<string>(_on_restore_failed));
    }

    private void _connect_if_present(string signal_name, Callable callback)
    {
        if (_store_api == null)
            return;

        if (_store_api.HasSignal(signal_name) && !_store_api.IsConnected(signal_name, callback))
            _store_api.Connect(signal_name, callback);
    }

    private void _on_purchase_success(string product_id, string transaction_id)
    {
        _owned_products.Add(product_id);
        EmitSignal("purchase_completed", product_id, transaction_id);
    }

    private void _on_purchase_failed(string product_id, string reason)
    {
        EmitSignal("purchase_failed", product_id, reason);
    }

    private void _on_purchase_cancelled(string product_id)
    {
        EmitSignal("purchase_cancelled", product_id);
    }

    private void _on_restore_completed(Godot.Collections.Array product_ids)
    {
        var restored = new Godot.Collections.Array<string>();
        foreach (var id in product_ids)
        {
            var productId = id.AsString();
            restored.Add(productId);
            _owned_products.Add(productId);
        }

        EmitSignal("restore_completed", restored);
    }

    private void _on_restore_failed(string error)
    {
        EmitSignal("restore_failed", error);
    }
}
