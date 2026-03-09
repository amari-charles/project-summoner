#!/usr/bin/env python3
"""Export frame-layered PSD files into normalized PNG frames and sprite sheets."""

from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image
from psd_tools import PSDImage


@dataclass
class FrameLayer:
    index: int
    name: str
    bbox: tuple[int, int, int, int]
    image: Image.Image


def parse_frame_number(name: str) -> int | None:
    match = re.search(r"(\d+)\s*$", name)
    if match:
        return int(match.group(1))
    return None


def slugify(value: str) -> str:
    value = value.strip().lower()
    value = re.sub(r"[^a-z0-9]+", "_", value)
    return value.strip("_") or "asset"


def sanitize_filename(value: str) -> str:
    value = value.strip()
    value = re.sub(r"[^A-Za-z0-9._-]+", "_", value)
    return value.strip("._") or "frame"


def iter_leaf_layers(layers: Iterable, include_hidden: bool) -> Iterable:
    for layer in layers:
        if layer.is_group():
            yield from iter_leaf_layers(layer, include_hidden)
            continue
        if not include_hidden and not layer.visible:
            continue
        yield layer


def load_frames(psd_path: Path, include_hidden: bool) -> list[FrameLayer]:
    psd = PSDImage.open(psd_path)
    frames: list[FrameLayer] = []

    for idx, layer in enumerate(iter_leaf_layers(psd, include_hidden)):
        bbox = layer.bbox
        if not isinstance(bbox, tuple) or len(bbox) != 4:
            continue
        x1, y1, x2, y2 = bbox
        if x2 <= x1 or y2 <= y1:
            continue
        image = layer.composite()
        frames.append(FrameLayer(index=idx, name=layer.name or f"frame_{idx}", bbox=bbox, image=image))

    def sort_key(frame: FrameLayer) -> tuple[int, int, int]:
        parsed = parse_frame_number(frame.name)
        if parsed is not None:
            return (0, parsed, frame.index)
        return (1, frame.index, frame.index)

    frames.sort(key=sort_key)
    return frames


def make_normalized_frame(layer: FrameLayer, union_bbox: tuple[int, int, int, int]) -> Image.Image:
    ux1, uy1, ux2, uy2 = union_bbox
    canvas = Image.new("RGBA", (ux2 - ux1, uy2 - uy1), (0, 0, 0, 0))
    x1, y1, _, _ = layer.bbox
    canvas.paste(layer.image, (x1 - ux1, y1 - uy1), layer.image)
    return canvas


def union_bbox(frames: list[FrameLayer]) -> tuple[int, int, int, int]:
    x1 = min(f.bbox[0] for f in frames)
    y1 = min(f.bbox[1] for f in frames)
    x2 = max(f.bbox[2] for f in frames)
    y2 = max(f.bbox[3] for f in frames)
    return (x1, y1, x2, y2)


def export_psd(
    psd_path: Path,
    output_root: Path,
    fps: int,
    include_hidden: bool,
    frame_prefix: str,
) -> None:
    frames = load_frames(psd_path, include_hidden)
    if not frames:
        raise ValueError(f"No exportable frame layers found in {psd_path}")

    psd = PSDImage.open(psd_path)
    source_canvas = (psd.width, psd.height)
    union = union_bbox(frames)
    frame_width = union[2] - union[0]
    frame_height = union[3] - union[1]

    asset_dir = output_root / slugify(psd_path.stem)
    frames_dir = asset_dir / "frames"
    frames_dir.mkdir(parents=True, exist_ok=True)

    normalized_frames: list[Image.Image] = []
    metadata_frames = []

    for i, frame in enumerate(frames):
        out_name = f"{frame_prefix}_{i:02d}.png"
        out_path = frames_dir / out_name
        normalized = make_normalized_frame(frame, union)
        normalized.save(out_path)
        normalized_frames.append(normalized)
        metadata_frames.append(
            {
                "index": i,
                "source_layer_index": frame.index,
                "source_layer_name": frame.name,
                "file": f"frames/{out_name}",
                "source_bbox": list(frame.bbox),
                "offset_within_frame": [frame.bbox[0] - union[0], frame.bbox[1] - union[1]],
            }
        )

    horizontal = Image.new("RGBA", (frame_width * len(normalized_frames), frame_height), (0, 0, 0, 0))
    for i, frame in enumerate(normalized_frames):
        horizontal.paste(frame, (i * frame_width, 0), frame)
    horizontal.save(asset_dir / "spritesheet_horizontal.png")

    vertical = Image.new("RGBA", (frame_width, frame_height * len(normalized_frames)), (0, 0, 0, 0))
    for i, frame in enumerate(normalized_frames):
        vertical.paste(frame, (0, i * frame_height), frame)
    vertical.save(asset_dir / "spritesheet_vertical.png")

    # Lightweight preview animation for quick visual validation.
    gif_frames = [f.convert("P", palette=Image.Palette.ADAPTIVE) for f in normalized_frames]
    gif_duration_ms = int(1000 / max(fps, 1))
    gif_frames[0].save(
        asset_dir / "preview.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=gif_duration_ms,
        loop=0,
        disposal=2,
        transparency=0,
    )

    metadata = {
        "source_file": str(psd_path),
        "source_canvas": {"width": source_canvas[0], "height": source_canvas[1]},
        "normalized_union_bbox": {"x1": union[0], "y1": union[1], "x2": union[2], "y2": union[3]},
        "frame_size": {"width": frame_width, "height": frame_height},
        "frame_count": len(normalized_frames),
        "fps": fps,
        "frame_prefix": frame_prefix,
        "spritesheets": {
            "horizontal": "spritesheet_horizontal.png",
            "vertical": "spritesheet_vertical.png",
        },
        "frames": metadata_frames,
    }

    with (asset_dir / "metadata.json").open("w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path, help="PSD file or directory containing PSD files")
    parser.add_argument("--output", type=Path, required=True, help="Output directory for exported assets")
    parser.add_argument("--fps", type=int, default=12, help="Preview animation FPS (default: 12)")
    parser.add_argument(
        "--frame-prefix",
        default="attack",
        help="Prefix for exported frame files (default: attack)",
    )
    parser.add_argument(
        "--include-hidden",
        action="store_true",
        help="Include hidden layers when exporting frames",
    )
    args = parser.parse_args()

    input_path = args.input
    if input_path.is_file():
        psd_files = [input_path]
    else:
        psd_files = sorted(input_path.glob("*.psd"))

    if not psd_files:
        raise SystemExit(f"No PSD files found at {input_path}")

    args.output.mkdir(parents=True, exist_ok=True)
    for psd_path in psd_files:
        export_psd(
            psd_path,
            args.output,
            fps=args.fps,
            include_hidden=args.include_hidden,
            frame_prefix=args.frame_prefix,
        )

    print(f"Exported {len(psd_files)} PSD file(s) to {args.output}")


if __name__ == "__main__":
    main()
