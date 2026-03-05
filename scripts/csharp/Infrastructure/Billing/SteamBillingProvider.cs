using System.Collections.Generic;
using Godot;

namespace Fateforged.Infrastructure.Billing;

public partial class SteamBillingProvider : BillingProvider
{
    private const string STEAM_SINGLETON_NAME = "Steam";

    private bool _available;
    private GodotObject? _steam_api;
    private Godot.Collections.Array<BillingProduct> _products = [];
    private readonly HashSet<string> _owned_products = [];

    public override void initialize()
    {
        _available = Engine.HasSingleton(STEAM_SINGLETON_NAME);
        if (_available)
        {
            _steam_api = Engine.GetSingleton(STEAM_SINGLETON_NAME);
            _connect_platform_signals();
        }
        else
        {
            GD.PushWarning("[SteamBilling] Steam singleton not found. Ensure GodotSteam is installed for Steam builds.");
        }

        _load_products_from_catalog();
        GD.Print($"[SteamBilling] Initialized (available={_available}, products={_products.Count})");
    }

    public override bool is_available()
    {
        return _available;
    }

    public override void purchase(string product_id)
    {
        if (!_available || _steam_api == null)
        {
            EmitSignal("purchase_failed", product_id, "Steam billing is unavailable");
            return;
        }

        var steamItemId = _map_product_id(product_id);
        if (string.IsNullOrEmpty(steamItemId))
        {
            EmitSignal("purchase_failed", product_id, $"Missing Steam item mapping for '{product_id}'");
            return;
        }

        if (_steam_api.HasMethod("purchase_item"))
            _steam_api.Call("purchase_item", steamItemId);
        else if (_steam_api.HasMethod("initTxn"))
            _steam_api.Call("initTxn", steamItemId);
        else
            EmitSignal("purchase_failed", product_id, "Steam billing API is not wired (missing purchase_item/initTxn)");
    }

    public override void restore_purchases()
    {
        var restored = new Godot.Collections.Array<string>();
        foreach (var productId in _owned_products)
            restored.Add(productId);
        EmitSignal("restore_completed", restored);
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
        return "SteamBilling";
    }

    private void _load_products_from_catalog()
    {
        var catalog = GetCatalog();
        if (catalog != null)
        {
            _products = catalog.get_all_products();
        }
        else
        {
            GD.PushWarning("[SteamBilling] BillingCatalog not found. No products available.");
            _products = [];
        }

        EmitSignal("products_loaded", _products);
    }

    private string _map_product_id(string product_id)
    {
        var catalog = GetCatalog();
        return catalog?.get_platform_product_id(product_id, "steam") ?? "";
    }

    private void _connect_platform_signals()
    {
        _connect_if_present("purchase_success", Callable.From<string, string>(_on_purchase_success));
        _connect_if_present("purchase_failed", Callable.From<string, string>(_on_purchase_failed));
        _connect_if_present("purchase_cancelled", Callable.From<string>(_on_purchase_cancelled));
        _connect_if_present("txn_authorized", Callable.From<long, long, bool>(_on_steam_txn_authorized));
    }

    private void _connect_if_present(string signal_name, Callable callback)
    {
        if (_steam_api == null)
            return;

        if (_steam_api.HasSignal(signal_name) && !_steam_api.IsConnected(signal_name, callback))
            _steam_api.Connect(signal_name, callback);
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

    private void _on_steam_txn_authorized(long order_id, long app_id, bool authorized)
    {
        if (!authorized)
        {
            EmitSignal("purchase_failed", "", $"Steam transaction {order_id} was not authorized");
            return;
        }

        GD.Print($"[SteamBilling] Transaction authorized (order={order_id} app={app_id})");
    }
}
