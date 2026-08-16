using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Godot;

namespace Fateforged.View.Debug;

/// <summary>
/// Opt-in battle-scene harness for testing WASD-controlled summoners without
/// changing the default stationary rules of normal arena battles.
/// </summary>
[GlobalClass]
public partial class DebugSummonerMovementController : Node
{
    [Export]
    public bool MovementEnabled { get; set; }

    [Export]
    public float MovementSpeed { get; set; } = 12f;

    [Export]
    public Rect2 PlayerMovementBounds { get; set; } = new(
        BattlefieldBounds.MinX,
        BattlefieldBounds.MinZ,
        BattlefieldBounds.MaxX - BattlefieldBounds.MinX,
        BattlefieldBounds.MaxZ - BattlefieldBounds.MinZ
    );

    [Export]
    public NodePath MovementTogglePath { get; set; } = "../UI/MovementTestPanel/VBox/MovementToggle";

    [Export]
    public NodePath MovementStatusPath { get; set; } = "../UI/MovementTestPanel/VBox/MovementStatus";

    [Export]
    public NodePath CameraControllerPath { get; set; } = "../BaseBattlefield3D/Camera3D";

    private CheckButton? _movementToggle;
    private Label? _movementStatus;
    private Node? _cameraController;
    private bool _cameraInputStateCaptured;
    private bool _keyboardPanBeforeMovement;
    private bool _edgePanBeforeMovement;

    public override void _Ready()
    {
        _movementToggle = GetNodeOrNull<CheckButton>(MovementTogglePath);
        _movementStatus = GetNodeOrNull<Label>(MovementStatusPath);
        _cameraController = GetNodeOrNull<Node>(CameraControllerPath);

        if (_movementToggle != null)
        {
            _movementToggle.ButtonPressed = MovementEnabled;
            _movementToggle.Toggled += OnMovementToggled;
        }

        RefreshMovementStatus();
        RefreshCameraInputOwnership();
    }

    public override void _ExitTree()
    {
        if (_movementToggle != null)
            _movementToggle.Toggled -= OnMovementToggled;
        RestoreCameraInputState();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!MovementEnabled)
            return;

        var battleScene = GetParentOrNull<BattleScene>();
        if (battleScene?.CurrentState != BattleScene.GameState.Playing)
            return;

        var simNode = GetTree().GetFirstNodeInGroup(GroupIDs.SimulationNode) as SimulationNode;
        if (simNode == null)
            return;

        var state = simNode.GetState();
        if (state.Phase == GamePhase.GameOver)
            return;

        var input = Godot.Input.GetVector(
            "move_left",
            "move_right",
            "move_up",
            "move_down"
        );
        if (input.IsZeroApprox())
            return;

        var current = state.Summoners[0].Position;
        var target = CalculateBoundedTarget(
            current,
            input,
            MovementSpeed,
            (float)delta,
            PlayerMovementBounds
        );
        simNode.SubmitCommand(new MoveSummonerCommand(0, target));
    }

    public void OnMovementToggled(bool enabled)
    {
        MovementEnabled = enabled;
        RefreshMovementStatus();
        RefreshCameraInputOwnership();
    }

    public static SimVector3 CalculateBoundedTarget(
        SimVector3 current,
        Vector2 input,
        float speed,
        float delta,
        Rect2 bounds
    )
    {
        var normalized = input.LengthSquared() > 1f ? input.Normalized() : input;
        float targetX = current.X + normalized.X * speed * delta;
        float targetZ = current.Z - normalized.Y * speed * delta;
        float maxX = bounds.Position.X + bounds.Size.X;
        float maxZ = bounds.Position.Y + bounds.Size.Y;

        return new SimVector3(
            Mathf.Clamp(targetX, bounds.Position.X, maxX),
            current.Y,
            Mathf.Clamp(targetZ, bounds.Position.Y, maxZ)
        );
    }

    private void RefreshMovementStatus()
    {
        if (_movementStatus == null)
            return;

        _movementStatus.Text = MovementEnabled
            ? "WASD: summoner · screen edge: camera"
            : "Summoner remains stationary";
    }

    private void RefreshCameraInputOwnership()
    {
        if (_cameraController == null)
            return;

        if (MovementEnabled)
        {
            if (!_cameraInputStateCaptured)
            {
                _keyboardPanBeforeMovement = ReadBoolProperty(
                    _cameraController,
                    "keyboard_pan_enabled"
                );
                _edgePanBeforeMovement = ReadBoolProperty(
                    _cameraController,
                    "edge_pan_enabled"
                );
                _cameraInputStateCaptured = true;
            }

            ApplyCameraInputMode(
                _cameraController,
                keyboardPanEnabled: false,
                edgePanEnabled: true
            );
            return;
        }

        RestoreCameraInputState();
    }

    private void RestoreCameraInputState()
    {
        if (_cameraController == null || !_cameraInputStateCaptured)
            return;

        ApplyCameraInputMode(
            _cameraController,
            _keyboardPanBeforeMovement,
            _edgePanBeforeMovement
        );
        _cameraInputStateCaptured = false;
    }

    public static void ApplyCameraInputMode(
        Node cameraController,
        bool keyboardPanEnabled,
        bool edgePanEnabled
    )
    {
        cameraController.Set("keyboard_pan_enabled", keyboardPanEnabled);
        cameraController.Set("edge_pan_enabled", edgePanEnabled);
    }

    private static bool ReadBoolProperty(Node node, StringName propertyName)
    {
        var value = node.Get(propertyName);
        return value.VariantType == Variant.Type.Bool && (bool)value;
    }
}
