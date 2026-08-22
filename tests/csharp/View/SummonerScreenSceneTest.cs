namespace Fateforged.Tests.View;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SummonerScreenSceneTest
{
    [TestCase]
    public void SummonerScreen_CombinesBuildProgressionAndInventory()
    {
        var packed = GD.Load<PackedScene>("res://scenes/meta/screens/summoner_screen.tscn");
        AssertThat(packed).IsNotNull();

        var screen = packed!.Instantiate<Control>();
        try
        {
            AssertThat(screen.FindChild("XPProgressBar", true, false)).IsNotNull();
            AssertThat(screen.FindChild("StatsPanel", true, false)).IsNotNull();
            AssertThat(screen.FindChild("EquipmentPanel", true, false)).IsNotNull();
            AssertThat(screen.FindChild("TraitsPanel", true, false)).IsNotNull();
            AssertThat(screen.FindChild("UpgradesButton", true, false)).IsNull();
            AssertThat(screen.FindChild("TraitsContainer", true, false)).IsNotNull();
            AssertThat(screen.FindChild("TraitDevelopmentOverlay", true, false)).IsNotNull();
            var traitsPanel = screen.FindChild("TraitsPanel", true, false) as Control;
            AssertThat(traitsPanel).IsNotNull();
            AssertThat(traitsPanel!.CustomMinimumSize.Y).IsGreaterEqual(245.0f);
            var traitsScroll = screen.FindChild("TraitsScroll", true, false) as ScrollContainer;
            AssertThat(traitsScroll).IsNotNull();
            AssertThat(traitsScroll!.VerticalScrollMode)
                .IsEqual(ScrollContainer.ScrollMode.Auto);
            AssertThat(screen.FindChild("InventoryPanel", true, false)).IsNotNull();
            AssertThat(screen.FindChild("InventoryGrid", true, false)).IsNotNull();
            var leftColumn = screen.FindChild("LeftColumn", true, false) as Control;
            var rightColumn = screen.FindChild("RightColumn", true, false) as Control;
            AssertThat(leftColumn).IsNotNull();
            AssertThat(rightColumn).IsNotNull();
            AssertThat(leftColumn!.SizeFlagsStretchRatio).IsEqual(1.0f);
            AssertThat(rightColumn!.SizeFlagsStretchRatio).IsEqual(1.0f);
            var portrait = screen.FindChild("PortraitTexture", true, false) as TextureRect;
            AssertThat(portrait).IsNotNull();
            AssertThat(portrait!.Texture).IsNotNull();

            AssertThat(screen.FindChild("LevelUpButton", true, false)).IsNull();
            AssertThat(screen.FindChild("GoldLabel", true, false)).IsNull();
            AssertThat(screen.FindChild("UpgradesPanel", true, false)).IsNull();
        }
        finally
        {
            screen.Free();
        }
    }

    [TestCase]
    public void TraitDevelopmentOverlay_UsesGraphAndContextualNodePopover()
    {
        var packed = GD.Load<PackedScene>(
            "res://scenes/meta/components/trait_development_overlay.tscn"
        );
        AssertThat(packed).IsNotNull();

        var overlay = packed!.Instantiate<Control>();
        try
        {
            AssertThat(overlay.FindChild("TreeCanvas", true, false)).IsNotNull();
            AssertThat(overlay.FindChild("DetailPanel", true, false)).IsNull();
            AssertThat(overlay.FindChild("NodeDetailPopover", true, false)).IsNotNull();
            AssertThat(overlay.FindChild("TraitSummaryLabel", true, false)).IsNull();
            AssertThat(overlay.FindChild("DetailName", true, false)).IsNotNull();
            AssertThat(overlay.FindChild("DetailRequirements", true, false)).IsNotNull();
            AssertThat(overlay.FindChild("ActionButton", true, false)).IsNotNull();
            AssertThat(overlay.FindChild("CloseButton", true, false)).IsNotNull();
        }
        finally
        {
            overlay.Free();
        }
    }
}
