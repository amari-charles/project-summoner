namespace Fateforged.Tests.View;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class PlayerSummonerSpriteSceneTest
{
    private const string CampusPlayerTexture =
        "res://assets/placeholders/tiny_swords/characters/placeholder_player_pawn_idle.png";

    [TestCase("res://scenes/battle/battlefield/battle_3d.tscn")]
    [TestCase("res://scenes/battle/battlefield/dev/debug_arena.tscn")]
    public void PlayerSummoner_UsesWalkableCampusSprite(string scenePath)
    {
        var packed = GD.Load<PackedScene>(scenePath);
        AssertThat(packed).IsNotNull();

        var scene = packed!.Instantiate<Node3D>();
        try
        {
            var visual = scene.GetNode<Sprite3D>("PlayerSummoner/Visual");

            AssertThat(visual.Texture.ResourcePath).IsEqual(CampusPlayerTexture);
            AssertThat(visual.Hframes).IsEqual(8);
            AssertThat(visual.PixelSize).IsEqual(0.075f);
            AssertThat(visual.Offset).IsEqual(new Vector2(0f, 39f));
        }
        finally
        {
            scene.Free();
        }
    }
}
