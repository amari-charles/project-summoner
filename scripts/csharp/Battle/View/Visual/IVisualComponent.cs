using Godot;

namespace Fateforged.Visual;

/// <summary>
/// Interface for visual rendering components.
/// Allows swapping between sprite-based and skeletal implementations.
/// All visual components must implement this interface.
/// </summary>
public interface IVisualComponent
{
    // =========================================================================
    // Animation
    // =========================================================================

    /// <summary>
    /// Play an animation by name (no auto-play).
    /// Overload for GDScript compatibility (Godot bug #59025).
    /// </summary>
    /// <param name="animName">Name of the animation to play.</param>
    void PlayAnimation(string animName);

    /// <summary>
    /// Play an animation by name.
    /// </summary>
    /// <param name="animName">Name of the animation to play.</param>
    /// <param name="autoPlay">If true, animation loops automatically.</param>
    void PlayAnimation(string animName, bool autoPlay);

    /// <summary>
    /// Stop the current animation.
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Get the name of the currently playing animation.
    /// </summary>
    /// <returns>Current animation name, or empty string if none.</returns>
    string GetCurrentAnimation();

    /// <summary>
    /// Check if an animation is currently playing.
    /// </summary>
    /// <returns>True if animation is playing.</returns>
    bool IsPlaying();

    /// <summary>
    /// Set the playback speed of animations.
    /// </summary>
    /// <param name="speed">Speed multiplier (1.0 = normal).</param>
    void SetAnimationSpeed(float speed);

    /// <summary>
    /// Get the duration of an animation in seconds.
    /// </summary>
    /// <param name="animName">Name of the animation.</param>
    /// <returns>Duration in seconds.</returns>
    float GetAnimationDuration(string animName);

    // =========================================================================
    // Rendering
    // =========================================================================

    /// <summary>
    /// Get the height of the sprite in world units.
    /// </summary>
    /// <returns>Sprite height.</returns>
    float GetSpriteHeight();

    /// <summary>
    /// Get the width of the sprite in world units.
    /// Used for shadow auto-sizing.
    /// </summary>
    /// <returns>Sprite width.</returns>
    float GetSpriteWidth();

    /// <summary>
    /// Get the horizontal offset for HP bar positioning.
    /// Used when the visual content is offset from the unit's origin.
    /// </summary>
    /// <returns>HP bar X offset in world units.</returns>
    float GetHpBarOffsetX();

    /// <summary>
    /// Flash the sprite white (damage feedback).
    /// </summary>
    void FlashWhite();

    /// <summary>
    /// Set horizontal flip state for the sprite.
    /// </summary>
    /// <param name="flip">True to flip horizontally.</param>
    void SetFlipH(bool flip);

    /// <summary>
    /// Set the render priority for depth ordering.
    /// Higher values render in front, lower values render behind.
    /// Range: -128 to 127.
    /// </summary>
    /// <param name="priority">Render priority value.</param>
    void SetRenderPriority(int priority);

    /// <summary>
    /// Check if the visual component is fully initialized.
    /// Important for spawn reveal timing.
    /// </summary>
    /// <returns>True if fully initialized.</returns>
    bool IsFullyInitialized();

    // =========================================================================
    // Spawn Preview (compile-time enforced!)
    // =========================================================================

    /// <summary>
    /// Create a ghost visual for spawn preview.
    /// This method is REQUIRED - ensures all units have spawn previews.
    /// </summary>
    /// <returns>A Node3D containing the ghost visual.</returns>
    Node3D CreateGhostVisual();

    /// <summary>
    /// Apply a tint color to the ghost visual.
    /// </summary>
    /// <param name="tint">Color to apply (e.g., blue for valid, red for invalid).</param>
    void ApplyGhostTint(Color tint);
}
