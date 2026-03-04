using Godot;

namespace Fateforged.Infrastructure.Billing;

/// <summary>
/// Main entry point for real-money purchases.
/// API intentionally mirrors legacy GDScript singleton.
/// </summary>
[GlobalClass]
public partial class PlatformBillingService : Node
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

    public enum Platform
    {
        UNKNOWN,
        EDITOR,
        STEAM,
        IOS,
        ANDROID,
        WEB,
    }

    private sealed partial class DisabledBillingProvider : BillingProvider
    {
        private readonly string _reason;

        public DisabledBillingProvider(string reason)
        {
            _reason = reason;
        }

        public override bool is_available()
        {
            return false;
        }

        public override void purchase(string product_id)
        {
            EmitSignal("purchase_failed", product_id, _reason);
        }

        public override void restore_purchases()
        {
            EmitSignal("restore_failed", _reason);
        }

        public override string get_provider_name()
        {
            return "DisabledBilling";
        }
    }

    public static PlatformBillingService? Instance { get; private set; }

    public Platform current_platform = Platform.UNKNOWN;

    private BillingProvider? _provider;
    private bool _provider_initialized;

    public override void _Ready()
    {
        Instance = this;
        current_platform = _detect_platform();
        _provider = _create_provider();
        _connect_provider_signals();

        if (!_provider_initialized)
            _provider.initialize();

        GD.Print($"[PlatformBilling] Platform: {current_platform}, Provider: {_provider.get_provider_name()}");
    }

    public bool is_available()
    {
        return _provider?.is_available() == true;
    }

    public void purchase(string product_id)
    {
        if (!is_available())
        {
            EmitSignal("purchase_failed", product_id, "Billing not available on this platform");
            return;
        }

        _provider?.purchase(product_id);
    }

    public void restore_purchases()
    {
        _provider?.restore_purchases();
    }

    public Godot.Collections.Array<BillingProduct> get_products()
    {
        return _provider?.get_products() ?? [];
    }

    public string get_localized_price(string product_id)
    {
        return _provider?.get_localized_price(product_id) ?? "";
    }

    public bool is_product_owned(string product_id)
    {
        return _provider?.is_product_owned(product_id) == true;
    }

    public Platform get_platform()
    {
        return current_platform;
    }

    public BillingProvider? get_provider()
    {
        return _provider;
    }

    public void stub_set_mode(int mode)
    {
        if (_provider is StubBillingProvider stub)
        {
            stub.set_mode(mode);
        }
        else
        {
            GD.PushWarning("stub_set_mode only works with StubBillingProvider");
        }
    }

    public void stub_set_delay(float delay)
    {
        if (_provider is StubBillingProvider stub)
        {
            stub.set_delay(delay);
        }
        else
        {
            GD.PushWarning("stub_set_delay only works with StubBillingProvider");
        }
    }

    public void stub_set_fail_message(string message)
    {
        if (_provider is StubBillingProvider stub)
        {
            stub.set_fail_message(message);
        }
        else
        {
            GD.PushWarning("stub_set_fail_message only works with StubBillingProvider");
        }
    }

    private Platform _detect_platform()
    {
        if (OS.HasFeature("editor"))
            return Platform.EDITOR;
        if (OS.HasFeature("steam"))
            return Platform.STEAM;
        if (OS.HasFeature("ios"))
            return Platform.IOS;
        if (OS.HasFeature("android"))
            return Platform.ANDROID;
        if (OS.HasFeature("web"))
            return Platform.WEB;
        return Platform.UNKNOWN;
    }

    private BillingProvider _create_provider()
    {
        return current_platform switch
        {
            Platform.EDITOR => _create_stub_provider(),
            Platform.STEAM => _create_platform_provider(new SteamBillingProvider(), "Steam"),
            Platform.IOS => _create_platform_provider(new IOSBillingProvider(), "iOS"),
            Platform.ANDROID => _create_platform_provider(new AndroidBillingProvider(), "Android"),
            _ => _create_disabled_provider("Billing is not supported on this platform"),
        };
    }

    private StubBillingProvider _create_stub_provider()
    {
        var provider = new StubBillingProvider { Name = "StubBillingProvider" };
        AddChild(provider);
        return provider;
    }

    private BillingProvider _create_platform_provider(BillingProvider provider, string platform_name)
    {
        provider.Name = $"{platform_name}Provider";
        AddChild(provider);
        provider.initialize();
        _provider_initialized = true;

        if (!provider.is_available())
            GD.PushError($"{platform_name} billing provider unavailable; purchases are disabled for this session.");

        return provider;
    }

    private BillingProvider _create_disabled_provider(string reason)
    {
        var provider = new DisabledBillingProvider(reason) { Name = "DisabledBillingProvider" };
        AddChild(provider);
        return provider;
    }

    private void _connect_provider_signals()
    {
        if (_provider == null)
            return;

        _provider.Connect("purchase_completed", Callable.From<string, string>(_on_provider_purchase_completed));
        _provider.Connect("purchase_failed", Callable.From<string, string>(_on_provider_purchase_failed));
        _provider.Connect("purchase_cancelled", Callable.From<string>(_on_provider_purchase_cancelled));
        _provider.Connect("products_loaded", Callable.From<Godot.Collections.Array<BillingProduct>>(_on_provider_products_loaded));
        _provider.Connect("restore_completed", Callable.From<Godot.Collections.Array<string>>(_on_provider_restore_completed));
        _provider.Connect("restore_failed", Callable.From<string>(_on_provider_restore_failed));
    }

    private void _on_provider_purchase_completed(string product_id, string transaction_id)
    {
        EmitSignal("purchase_completed", product_id, transaction_id);
    }

    private void _on_provider_purchase_failed(string product_id, string error)
    {
        EmitSignal("purchase_failed", product_id, error);
    }

    private void _on_provider_purchase_cancelled(string product_id)
    {
        EmitSignal("purchase_cancelled", product_id);
    }

    private void _on_provider_products_loaded(Godot.Collections.Array<BillingProduct> products)
    {
        EmitSignal("products_loaded", products);
    }

    private void _on_provider_restore_completed(Godot.Collections.Array<string> restored_product_ids)
    {
        EmitSignal("restore_completed", restored_product_ids);
    }

    private void _on_provider_restore_failed(string error)
    {
        EmitSignal("restore_failed", error);
    }
}
