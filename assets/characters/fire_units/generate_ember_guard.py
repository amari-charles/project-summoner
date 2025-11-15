#!/usr/bin/env python3
"""Generate Ember Guard placeholder sprite - red square."""

from PIL import Image, ImageDraw

# Constants
SIZE = 310
EMBER_COLOR = "#DD2200"  # Bright red for ember
OUTLINE_COLOR = "#000000"
OUTLINE_WIDTH = 4


def create_ember_guard_sprite(filename):
    """Create a square sprite for Ember Guard."""
    img = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Draw square
    margin = 30
    draw.rectangle(
        [margin, margin, SIZE - margin, SIZE - margin],
        fill=EMBER_COLOR,
        outline=OUTLINE_COLOR,
        width=OUTLINE_WIDTH
    )

    img.save(filename)
    print(f"Created {filename}")


if __name__ == "__main__":
    create_ember_guard_sprite("ember_guard.png")
    print("Ember Guard sprite generated!")
