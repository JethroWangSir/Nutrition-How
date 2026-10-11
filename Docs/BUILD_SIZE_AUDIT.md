# macOS Build Size Audit

Audit date: 2026-10-11

Target: Unity 6000.3.26f1, macOS Apple Silicon (`arm64`)

## Result

The original uncompressed player was approximately 5.7 GB. Enabling Unity's
lossless LZ4HC player-data compression and targeted media optimization reduced
the built app to 417.2 MB without changing texture resolution. The compressed
DMG is 314 MB, and the build remains Apple Silicon-only.

## Unity Build Report

| Category | Uncompressed size | Share |
| --- | ---: | ---: |
| Textures | 5.5 GB | 98.6% |
| Other assets, including video | 62.4 MB | 1.1% |
| Audio | 10.7 MB | 0.2% |
| Shaders | 1.6 MB | <0.1% |
| Animations and levels | 1.4 MB | <0.1% |

Largest texture groups by top-level asset folder:

| Folder | Reported uncompressed size |
| --- | ---: |
| `Assets/第一關` | 1,541.6 MB |
| `Assets/介面` | 1,118.0 MB |
| `Assets/敵人` | 1,059.9 MB |
| `Assets/第三關` | 832.1 MB |
| `Assets/第四關` | 786.3 MB |
| `Assets/玩家` | 247.6 MB |

The largest individual runtime textures are animation frames, commonly 7–15 MB
each before player-data compression. Representative examples include the fourth
level ship frames, cake-enemy frames, stamp animation frames, and large intestine
background sprites.

## Import and duplicate findings

- 3,204 of 3,223 textures automatically select DXT5 for the Standalone target.
- 14 select DXT1, four RGBA32, and one DXT1 Crunch format.
- Source-media hashing found 54 duplicate groups with only about 9.0 MiB of
  removable source duplication in total.
- Duplicate files retain different Unity GUIDs and are referenced by existing
  animations or scenes. Deleting them without first rewriting references would
  be unsafe, while the possible saving is small.

## Applied optimizations

- Unity player data uses lossless LZ4HC compression.
- `宣傳片.mp4` and `吸殘渣.mp4` were re-encoded as 1080p H.264 CRF 20 while
  preserving their original AAC audio. SSIM scores were 0.9964 and 0.9895.
- Background music imports as streamed Vorbis at quality 0.7 instead of being
  fully preloaded.
- Existing automatic DXT texture compression remains enabled; no additional
  lossy texture downscaling was applied.

This keeps the optimization focused on large, measurable wins without breaking
Unity object references or unnecessarily reducing visual resolution.
