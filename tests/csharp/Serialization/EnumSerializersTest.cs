namespace ProjectSummoner.Tests.Serialization;

using System;
using GdUnit4;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for EnumSerializers - centralized enum serialization/deserialization.
/// </summary>
[TestSuite]
public class EnumSerializersTest
{
    // =========================================================================
    // ItemSlot Tests
    // =========================================================================

    [TestCase]
    public void ItemSlot_Serialize_ReturnsLowercaseString()
    {
        AssertThat(EnumSerializers.Serialize(ItemSlot.Weapon)).IsEqual("weapon");
        AssertThat(EnumSerializers.Serialize(ItemSlot.Ring1)).IsEqual("ring1");
        AssertThat(EnumSerializers.Serialize(ItemSlot.Ring2)).IsEqual("ring2");
        AssertThat(EnumSerializers.Serialize(ItemSlot.Vestments)).IsEqual("vestments");
    }

    [TestCase]
    public void ItemSlot_Deserialize_ParsesLowercaseString()
    {
        AssertThat(EnumSerializers.DeserializeSlot("weapon")).IsEqual(ItemSlot.Weapon);
        AssertThat(EnumSerializers.DeserializeSlot("ring1")).IsEqual(ItemSlot.Ring1);
        AssertThat(EnumSerializers.DeserializeSlot("ring2")).IsEqual(ItemSlot.Ring2);
        AssertThat(EnumSerializers.DeserializeSlot("vestments")).IsEqual(ItemSlot.Vestments);
    }

    [TestCase]
    public void ItemSlot_Deserialize_ReturnsNullForEmptyOrNull()
    {
        AssertThat(EnumSerializers.DeserializeSlot(null)).IsNull();
        AssertThat(EnumSerializers.DeserializeSlot("")).IsNull();
    }

    [TestCase]
    public void ItemSlot_Deserialize_ReturnsNullForInvalidValue()
    {
        AssertThat(EnumSerializers.DeserializeSlot("invalid")).IsNull();
        AssertThat(EnumSerializers.DeserializeSlot("WEAPON")).IsNull(); // Case sensitive
        AssertThat(EnumSerializers.DeserializeSlot("Weapon")).IsNull(); // Case sensitive
    }

    [TestCase]
    public void ItemSlot_RoundTrip_AllValues()
    {
        foreach (var slot in new[] { ItemSlot.Weapon, ItemSlot.Ring1, ItemSlot.Ring2, ItemSlot.Vestments })
        {
            var serialized = EnumSerializers.Serialize(slot);
            var deserialized = EnumSerializers.DeserializeSlot(serialized);
            AssertThat(deserialized).IsEqual(slot);
        }
    }

    [TestCase]
    public void ItemSlot_DeserializeStrict_ParsesValidValues()
    {
        AssertThat(EnumSerializers.DeserializeSlotStrict("weapon")).IsEqual(ItemSlot.Weapon);
        AssertThat(EnumSerializers.DeserializeSlotStrict("ring1")).IsEqual(ItemSlot.Ring1);
        AssertThat(EnumSerializers.DeserializeSlotStrict("ring2")).IsEqual(ItemSlot.Ring2);
        AssertThat(EnumSerializers.DeserializeSlotStrict("vestments")).IsEqual(ItemSlot.Vestments);
    }

    [TestCase]
    public void ItemSlot_DeserializeStrict_ThrowsForNullOrEmpty()
    {
        AssertThrown(() => EnumSerializers.DeserializeSlotStrict(null))
            .IsInstanceOf<ArgumentException>();
        AssertThrown(() => EnumSerializers.DeserializeSlotStrict(""))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void ItemSlot_DeserializeStrict_ThrowsForInvalidValue()
    {
        AssertThrown(() => EnumSerializers.DeserializeSlotStrict("invalid"))
            .IsInstanceOf<ArgumentException>();
        AssertThrown(() => EnumSerializers.DeserializeSlotStrict("WEAPON"))
            .IsInstanceOf<ArgumentException>();
    }

    // =========================================================================
    // ContentBinding Tests
    // =========================================================================

    [TestCase]
    public void ContentBinding_Serialize_ReturnsIntValue()
    {
        AssertThat(EnumSerializers.Serialize(ContentBinding.AccountWide)).IsEqual(0);
        AssertThat(EnumSerializers.Serialize(ContentBinding.SummonerBound)).IsEqual(1);
    }

    [TestCase]
    public void ContentBinding_Deserialize_ParsesValidInt()
    {
        AssertThat(EnumSerializers.DeserializeBinding(0)).IsEqual(ContentBinding.AccountWide);
        AssertThat(EnumSerializers.DeserializeBinding(1)).IsEqual(ContentBinding.SummonerBound);
    }

    [TestCase]
    public void ContentBinding_Deserialize_DefaultsToAccountWideForInvalidValue()
    {
        // Invalid values should default to AccountWide with warning
        AssertThat(EnumSerializers.DeserializeBinding(-1)).IsEqual(ContentBinding.AccountWide);
        AssertThat(EnumSerializers.DeserializeBinding(99)).IsEqual(ContentBinding.AccountWide);
    }

    [TestCase]
    public void ContentBinding_RoundTrip_AllValues()
    {
        foreach (var binding in new[] { ContentBinding.AccountWide, ContentBinding.SummonerBound })
        {
            var serialized = EnumSerializers.Serialize(binding);
            var deserialized = EnumSerializers.DeserializeBinding(serialized);
            AssertThat(deserialized).IsEqual(binding);
        }
    }

    [TestCase]
    public void ContentBinding_DeserializeStrict_ParsesValidValues()
    {
        AssertThat(EnumSerializers.DeserializeBindingStrict(0)).IsEqual(ContentBinding.AccountWide);
        AssertThat(EnumSerializers.DeserializeBindingStrict(1)).IsEqual(ContentBinding.SummonerBound);
    }

    [TestCase]
    public void ContentBinding_DeserializeStrict_ThrowsForInvalidValue()
    {
        AssertThrown(() => EnumSerializers.DeserializeBindingStrict(-1))
            .IsInstanceOf<ArgumentException>();
        AssertThrown(() => EnumSerializers.DeserializeBindingStrict(99))
            .IsInstanceOf<ArgumentException>();
    }

    // =========================================================================
    // ResourceType Tests
    // =========================================================================

    [TestCase]
    public void ResourceType_Serialize_ReturnsLowercaseString()
    {
        AssertThat(EnumSerializers.Serialize(ResourceType.Gold)).IsEqual("gold");
        AssertThat(EnumSerializers.Serialize(ResourceType.Gems)).IsEqual("gems");
        AssertThat(EnumSerializers.Serialize(ResourceType.Essence)).IsEqual("essence");
        AssertThat(EnumSerializers.Serialize(ResourceType.Fragments)).IsEqual("fragments");
    }

    [TestCase]
    public void ResourceType_Deserialize_ParsesLowercaseString()
    {
        AssertThat(EnumSerializers.DeserializeResourceType("gold")).IsEqual(ResourceType.Gold);
        AssertThat(EnumSerializers.DeserializeResourceType("gems")).IsEqual(ResourceType.Gems);
        AssertThat(EnumSerializers.DeserializeResourceType("essence")).IsEqual(ResourceType.Essence);
        AssertThat(EnumSerializers.DeserializeResourceType("fragments")).IsEqual(ResourceType.Fragments);
    }

    [TestCase]
    public void ResourceType_Deserialize_ReturnsNullForEmptyOrNull()
    {
        AssertThat(EnumSerializers.DeserializeResourceType(null)).IsNull();
        AssertThat(EnumSerializers.DeserializeResourceType("")).IsNull();
    }

    [TestCase]
    public void ResourceType_Deserialize_ReturnsNullForInvalidValue()
    {
        AssertThat(EnumSerializers.DeserializeResourceType("invalid")).IsNull();
        AssertThat(EnumSerializers.DeserializeResourceType("GOLD")).IsNull(); // Case sensitive
        AssertThat(EnumSerializers.DeserializeResourceType("Gold")).IsNull(); // Case sensitive
    }

    [TestCase]
    public void ResourceType_RoundTrip_AllValues()
    {
        foreach (var type in new[] { ResourceType.Gold, ResourceType.Gems, ResourceType.Essence, ResourceType.Fragments })
        {
            var serialized = EnumSerializers.Serialize(type);
            var deserialized = EnumSerializers.DeserializeResourceType(serialized);
            AssertThat(deserialized).IsEqual(type);
        }
    }

    [TestCase]
    public void ResourceType_DeserializeStrict_ParsesValidValues()
    {
        AssertThat(EnumSerializers.DeserializeResourceTypeStrict("gold")).IsEqual(ResourceType.Gold);
        AssertThat(EnumSerializers.DeserializeResourceTypeStrict("gems")).IsEqual(ResourceType.Gems);
        AssertThat(EnumSerializers.DeserializeResourceTypeStrict("essence")).IsEqual(ResourceType.Essence);
        AssertThat(EnumSerializers.DeserializeResourceTypeStrict("fragments")).IsEqual(ResourceType.Fragments);
    }

    [TestCase]
    public void ResourceType_DeserializeStrict_ThrowsForNullOrEmpty()
    {
        AssertThrown(() => EnumSerializers.DeserializeResourceTypeStrict(null))
            .IsInstanceOf<ArgumentException>();
        AssertThrown(() => EnumSerializers.DeserializeResourceTypeStrict(""))
            .IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    public void ResourceType_DeserializeStrict_ThrowsForInvalidValue()
    {
        AssertThrown(() => EnumSerializers.DeserializeResourceTypeStrict("invalid"))
            .IsInstanceOf<ArgumentException>();
        AssertThrown(() => EnumSerializers.DeserializeResourceTypeStrict("GOLD"))
            .IsInstanceOf<ArgumentException>();
    }

    // =========================================================================
    // ResourceTypeExtensions Integration Tests
    // =========================================================================

    [TestCase]
    public void ResourceTypeExtensions_ToKey_DelegatesToEnumSerializers()
    {
        // Verify the extension methods work correctly
        AssertThat(ResourceType.Gold.ToKey()).IsEqual("gold");
        AssertThat(ResourceType.Gems.ToKey()).IsEqual("gems");
    }

    [TestCase]
    public void ResourceTypeExtensions_FromKey_DelegatesToEnumSerializers()
    {
        AssertThat(ResourceTypeExtensions.FromKey("gold")).IsEqual(ResourceType.Gold);
        AssertThat(ResourceTypeExtensions.FromKey("invalid")).IsNull();
    }
}
