# TPHDFileUpdater
A tool to update the FileSizeList and DecompressedFileSize files when modding Twilight Princess HD for Cemu.

## How to use

The tool can be used in 2 ways:
- Place it in your graphicPack mod folder (eg: `graphicPacks/TwilightPrincessHD_MyMod/TPHDFileUpdated.exe`), next to the "content" sub-folder
- Place it wherever and drag-n-drop your mod folder (that contains the "content" sub-folder) onto the executable

## Pending features
- [ ] automatically backup the FileSizeList.txt and DecompressedFileSize.txt files
- [ ] make it possible to add new assets

## Notes
I'm currently unsure about how Twilight Princess HD handles multiple graphic packs through Cemu, that is, multiple packs that contain different FileSizeList and/or DecompressedFileSize files.

Based on other mods, I think the "safest" approach would be to merge all the wanted mods into a single combined mod, and update the FileSizeList and DecompressedFileSize files in there.
