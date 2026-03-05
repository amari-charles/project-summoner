namespace Fateforged.Tests.Billing;

using System.Collections.Generic;
using System.Reflection;
using Fateforged.Infrastructure.Billing;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public partial class PlatformBillingServiceTest
{
    private sealed partial class TestBillingProvider : BillingProvider
    {
        private readonly HashSet<string> _owned = [];

        public override bool is_available()
        {
            return true;
        }

        public override bool is_product_owned(string product_id)
        {
            return _owned.Contains(product_id);
        }

        public void MarkOwned(string product_id)
        {
            _owned.Add(product_id);
        }
    }

    private readonly List<Node> _created_nodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (var i = _created_nodes.Count - 1; i >= 0; i--)
        {
            var node = _created_nodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _created_nodes.Clear();
    }

    [TestCase]
    public void ProviderPurchaseCompleted_NormalizesIosStoreId()
    {
        var (platformBilling, provider) = CreateHarness(PlatformBillingService.Platform.IOS);

        var seenProductId = "";
        platformBilling.Connect("purchase_completed", Callable.From<string, string>((productId, _txn) => seenProductId = productId));

        provider.EmitSignal("purchase_completed", "com.projectsummoner.starter_pack", "txn_1");
        AssertThat(seenProductId).IsEqual("starter_pack");
    }

    [TestCase]
    public void ProviderRestoreCompleted_NormalizesStoreIds()
    {
        var (platformBilling, provider) = CreateHarness(PlatformBillingService.Platform.IOS);

        Godot.Collections.Array<string> restored = [];
        platformBilling.Connect("restore_completed", Callable.From<Godot.Collections.Array<string>>(ids => restored = ids));

        provider.EmitSignal("restore_completed", new Godot.Collections.Array<string>
        {
            "com.projectsummoner.starter_pack",
            "com.projectsummoner.gems_500",
        });

        AssertThat(restored.Contains("starter_pack")).IsTrue();
        AssertThat(restored.Contains("gems_500")).IsTrue();
    }

    [TestCase]
    public void IsProductOwned_Consumable_ReturnsFalseEvenAfterPurchase()
    {
        var (platformBilling, provider) = CreateHarness(PlatformBillingService.Platform.IOS);

        provider.MarkOwned("com.projectsummoner.gems_500");
        provider.EmitSignal("purchase_completed", "com.projectsummoner.gems_500", "txn_2");

        AssertThat(platformBilling.is_product_owned("gems_500")).IsFalse();
        AssertThat(platformBilling.is_product_owned("com.projectsummoner.gems_500")).IsFalse();
    }

    [TestCase]
    public void IsProductOwned_NonConsumable_ReturnsTrueAfterPurchase()
    {
        var (platformBilling, provider) = CreateHarness(PlatformBillingService.Platform.IOS);

        provider.MarkOwned("com.projectsummoner.starter_pack");
        provider.EmitSignal("purchase_completed", "com.projectsummoner.starter_pack", "txn_3");

        AssertThat(platformBilling.is_product_owned("starter_pack")).IsTrue();
    }

    private (PlatformBillingService Billing, TestBillingProvider Provider) CreateHarness(PlatformBillingService.Platform platform)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var existingCatalog = root.GetNodeOrNull<BillingCatalogService>("BillingCatalog");
        if (existingCatalog == null)
        {
            var catalog = new BillingCatalogService { Name = "BillingCatalog" };
            root.AddChild(catalog);
            _created_nodes.Add(catalog);
        }

        var billing = new PlatformBillingService
        {
            Name = $"PlatformBillingTest_{_created_nodes.Count}",
        };
        root.AddChild(billing);
        _created_nodes.Add(billing);
        billing.current_platform = platform;

        var provider = new TestBillingProvider { Name = "TestProvider" };
        billing.AddChild(provider);
        _created_nodes.Add(provider);

        SetPrivateField(billing, "_provider", provider);
        InvokePrivateMethod(billing, "_connect_provider_signals");

        return (billing, provider);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = typeof(PlatformBillingService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }

    private static void InvokePrivateMethod(object instance, string methodName)
    {
        var method = typeof(PlatformBillingService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(instance, null);
    }
}
