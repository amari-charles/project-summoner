namespace Fateforged.Tests.Billing;

using Fateforged.Infrastructure.Billing;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class BillingCatalogServiceTest
{
    private readonly System.Collections.Generic.List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    private BillingCatalogService CreateCatalog()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var catalog = new BillingCatalogService();
        root.AddChild(catalog);
        _createdNodes.Add(catalog);
        catalog._Ready();
        return catalog;
    }

    [TestCase]
    public void Ready_BuildsAllProducts()
    {
        var catalog = CreateCatalog();

        var all = catalog.get_all_products();
        AssertThat(all.Count).IsEqual(8);
    }

    [TestCase]
    public void GetProduct_GemPack_HasExpectedFields()
    {
        var catalog = CreateCatalog();

        var product = catalog.get_product("gems_100");
        AssertObject(product).IsNotNull();

        AssertThat(product!.product_id).IsEqual("gems_100");
        AssertThat(product.product_type).IsEqual(BillingProduct.ProductType.CONSUMABLE);
        AssertThat(product.gems_amount).IsEqual(100);
        AssertThat(product.price_usd).IsEqual(0.99f);
    }

    [TestCase]
    public void GetProduct_Bundle_HasExpectedRewards()
    {
        var catalog = CreateCatalog();

        var product = catalog.get_product("starter_pack");
        AssertObject(product).IsNotNull();

        AssertThat(product!.product_type).IsEqual(BillingProduct.ProductType.NON_CONSUMABLE);
        AssertThat(product.gems_amount).IsEqual(200);
        AssertThat(product.rewards.ContainsKey("summoners")).IsTrue();
    }

    [TestCase]
    public void PlatformMapping_Ios_UsesStoreId()
    {
        var catalog = CreateCatalog();

        var storeId = catalog.get_platform_product_id("gems_500", "ios");
        AssertThat(storeId).IsEqual("com.projectsummoner.gems_500");
    }

    [TestCase]
    public void PlatformMapping_UnknownPlatform_ReturnsInternalId()
    {
        var catalog = CreateCatalog();

        var storeId = catalog.get_platform_product_id("gems_500", "switch");
        AssertThat(storeId).IsEqual("gems_500");
    }

    [TestCase]
    public void PlatformMapping_MissingProduct_ReturnsEmptyString()
    {
        var catalog = CreateCatalog();

        var storeId = catalog.get_platform_product_id("does_not_exist", "ios");
        AssertThat(storeId).IsEqual("");
    }

    [TestCase]
    public void InternalMapping_IosStoreId_ResolvesToInternalProductId()
    {
        var catalog = CreateCatalog();

        var internalId = catalog.get_internal_product_id("com.projectsummoner.gems_500", "ios");
        AssertThat(internalId).IsEqual("gems_500");
    }

    [TestCase]
    public void ProductTypeChecks_WorkForGemPacksAndBundles()
    {
        var catalog = CreateCatalog();

        AssertThat(catalog.is_gem_pack("gems_1200")).IsTrue();
        AssertThat(catalog.is_bundle("starter_pack")).IsTrue();
        AssertThat(catalog.is_bundle("gems_1200")).IsFalse();
    }
}
