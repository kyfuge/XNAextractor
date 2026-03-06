# Xenominer Asset Extractor

A standalone C# console utility built with MonoGame designed to extract models, textures, and audio from compiled XNA 4 `.xnb` asset files. While specifically built to handle the custom data structures of the indie title *Xenominer*, the core extraction logic can be applied to many other XNA-based games. I have not tested it with other games.
As of right now it only definitely works with the Windows version, not the Xbox 360 packaged version.

## Features

* 3D Model Extraction (`.obj`)
* Universal Texture Extraction (`.png`)
* Hardware-Lock Audio Bypass (`.wav`)
* Custom Type Reader Injection:** Seamlessly handles custom `ContentTypeReader` locks (like Xenominer's `AnimationAux.ModelExtraReader`) by forcing the CLR to load an injected `XnaAux.dll` into active memory before MonoGame parses the assets.
* Interactive CLI:** Simple, drag-and-drop terminal interface that automatically validates target directories.

## Usage

### Prerequisites
* A installation of *Xenominer*.
* The tool requires the companion `XnaAux.dll` (containing the decompiled Xenominer animation data structures) to be present in the exact same directory as the executable to bypass MonoGame's strict assembly resolution.

### Running the Extractor
1. Download the latest release from the **Releases** tab, or build the project from source.
2. Launch `XenominerExtractor.exe`.
3. When prompted, type, paste, or **drag-and-drop** your Xenominer installation folder directly into the console window.
4. Press `Enter`.

The tool will automatically locate the `Content` folder, recursively scan all directories for `.xnb` files, and begin the extraction process.

### Output
All successfully extracted files will be placed in an `ExtractedAssets` folder generated in the same directory as the executable. The original folder structure of the game will be flattened, and assets will be output as:
* Models -> `[AssetName].obj`
* Textures -> `[AssetName].png`
* Audio -> `[AssetName].wav`

## ⚠️ Known Limitations

* **Animation Data:** This tool currently discards custom animation keyframe data attached to models. Extracted `.obj` files are static geometry only.
* **Compressed Audio:** The manual audio parser only supports uncompressed PCM/ADPCM streams. If an XNB is flagged with LZX or LZ4 compression, or utilizes proprietary Xbox 360 xWMA encoding, the audio file will be skipped.
* **Effects and Custom Types** As of right now this tool does not extract effects, strings/text files, or custom Xenominer types.

