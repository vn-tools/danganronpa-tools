<s>Dragontoppa</s> Danganronpa tools
====================================

Tools for translating Danganronpa (PC version).

### `lin_compiler`

LIN script compiler / decompiler.

TODO (tzk): describe usage.

### `wad_archiver`

WAD archive packer and unpacker. Does not convert anything by itself - it's
used only to repack the outermost WAD files. The tool requires Python 3.5. It
is recommended to use following workflow for working with the patches:

1. Make a backup of the auxiliary archive (i.e. the small one):  
   `cp /path/to/game/dr1_data_keyboard.wad backup.wad`

2. Extract the auxiliary archive:  
   `./wad_archiver extract /path/to/game/dr1_data_keyboard.wad extracted/`

3. *Modify assets in the `extracted/` directory*

4. Pack the files back:  
   `./wad_archiver create extracted/ /path/to/game/dr1_data_keyboard.wad`

Incremental patches were not implemented because patching
`dr1_data_keyboard.wad` this way seems to be fast enough.