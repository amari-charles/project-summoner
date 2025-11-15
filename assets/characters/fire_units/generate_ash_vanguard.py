#!/usr/bin/env python3
"""Generate Ash Vanguard placeholder sprite - large orange-red circle."""

from PIL import Image, ImageDraw

# Constants
SIZE = 310
ASH_COLOR = "#CC4400"  # Dark orange-red for ash
OUTLINE_COLOR = "#000000"
OUTLINE_WIDTH = 4


def create_ash_vanguard_sprite(filename):
    """Create a large circle sprite for Ash Vanguard."""
    img = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Draw large filled circle
    margin = 15  # Smaller margin = bigger circle
    draw.ellipse(
        [margin, margin, SIZE - margin, SIZE - margin],
        fill=ASH_COLOR,
        outline=OUTLINE_COLOR,
        width=OUTLINE_WIDTH
    )

    img.save(filename)
    print(f"Created {filename}")


if __name__ == "__main__":
    create_ash_vanguard_sprite("ash_vanguard.png")
    print("Ash Vanguard sprite generated!")
