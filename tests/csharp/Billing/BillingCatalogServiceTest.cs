namespace Fateforged.Tests.Billing;

using Fateforged.Infrastructure.Billing;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class BillingCatalogServiceTest
{
    [TestCase]
    public void Ready_BuildsAllProducts()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var all = catalog.get_all_products();
        AssertThat(all.Count).IsEqual(8);
    }

    [TestCase]
    public void GetProduct_GemPack_HasExpectedFields()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

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
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var product = catalog.get_product("starter_pack");
        AssertObject(product).IsNotNull();

        AssertThat(product!.product_type).IsEqual(BillingProduct.ProductType.NON_CONSUMABLE);
        AssertThat(product.gems_amount).IsEqual(200);
        AssertThat(product.rewards.ContainsKey("summoners")).IsTrue();
    }

    [TestCase]
    public void PlatformMapping_Ios_UsesStoreId()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var storeId = catalog.get_platform_product_id("gems_500", "ios");
        AssertThat(storeId).IsEqual("com.projectsummoner.gems_500");
    }

    [TestCase]
    public void PlatformMapping_UnknownPlatform_ReturnsInternalId()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var storeId = catalog.get_platform_product_id("gems_500", "switch");
        AssertThat(storeId).IsEqual("gems_500");
    }

    [TestCase]
    public void PlatformMapping_MissingProduct_ReturnsEmptyString()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var storeId = catalog.get_platform_product_id("does_not_exist", "ios");
        AssertThat(storeId).IsEqual("");
    }

    [TestCase]
    public void InternalMapping_IosStoreId_ResolvesToInternalProductId()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        var internalId = catalog.get_internal_product_id("com.projectsummoner.gems_500", "ios");
        AssertThat(internalId).IsEqual("gems_500");
    }

    [TestCase]
    public void ProductTypeChecks_WorkForGemPacksAndBundles()
    {
        var catalog = new BillingCatalogService();
        catalog._Ready();

        AssertThat(catalog.is_gem_pack("gems_1200")).IsTrue();
        AssertThat(catalog.is_bundle("starter_pack")).IsTrue();
        AssertThat(catalog.is_bundle("gems_1200")).IsFalse();
    }
}
