namespace Fateforged.Tests.View;

using Fateforged.View;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class PlayerSummonerSpriteSceneTest
{
    private const string CampusPlayerTexture =
        "res://assets/placeholders/tiny_swords/characters/placeholder_player_pawn_idle.png";
    private const string CampusPlayerRunTexture =
        "res://assets/placeholders/tiny_swords/characters/placeholder_player_pawn_run.png";

    [TestCase("res://scenes/battle/battlefield/battle_3d.tscn")]
    [TestCase("res://scenes/battle/battlefield/dev/debug_arena.tscn")]
    [TestCase("res://scenes/battle/battlefield/dev/compact_ruin_skirmish.tscn")]
    public void PlayerSummoner_UsesWalkableCampusSprite(string scenePath)
    {
        var packed = GD.Load<PackedScene>(scenePath);
        AssertThat(packed).IsNotNull();

        var scene = packed!.Instantiate<Node3D>();
        try
        {
            var visual = scene.GetNode<Sprite3D>("PlayerSummoner/Visual");
            var summoner = scene.GetNode<SummonerVisual>("PlayerSummoner");

            AssertThat(visual.Texture.ResourcePath).IsEqual(CampusPlayerTexture);
            AssertThat(visual.Hframes).IsEqual(8);
            AssertThat(visual.PixelSize).IsEqual(0.075f);
            AssertThat(visual.Offset).IsEqual(new Vector2(0f, 39f));
            AssertThat(summoner.MovementIdleTexture!.ResourcePath).IsEqual(CampusPlayerTexture);
            AssertThat(summoner.MovementRunTexture!.ResourcePath).IsEqual(CampusPlayerRunTexture);
            AssertThat(summoner.MovementIdleFrameCount).IsEqual(8);
            AssertThat(summoner.MovementRunFrameCount).IsEqual(6);
        }
        finally
        {
            scene.Free();
        }
    }
}
