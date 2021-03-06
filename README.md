<s>Dragontoppa</s> Danganronpa tools
====================================

Tools for translating Danganronpa (PC version).

### `lin_compiler`

LIN script compiler / decompiler.

To compile a script file, simply supply the input file (and optionally an
output file). The compiler will spit out a .lin file, which you can add into
the game.  
Example: `lin_compiler input.txt output.lin`

Decompiling works exactly the same, except you supply a -d (or --decompile)
argument.  
Example: `lin_compiler -d input.lin output.txt`

If you are working with Danganronpa 2 script files, you should additionally
pass the `-dr2` (or `--danganronpa2`) argument.

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

5. Go to step 3

You can also combine multiple directories into one archive like this:

    ./wad_archiver create \
        directory1/ \
        directory2/ \
        directory3/ \
        output.wad

Incremental patches were not implemented because patching
`dr1_data_keyboard.wad` this way seems to be fast enough.

### `pak_archiver`

PAK archive packer and unpacker. The usage is similar to WAD archiver with two
differences:

- since PAK files do not contain information about file names, all entries
  use hexadecimal numeric names.
- since PAK files most of the time contain only simple text tables rather than
  full fledged files, this tool allows on-the-fly patching of such entries like
  so:

      ./pak_archiver replace input.pak 1A "just an example"

  `1A` is used to refer to the entry number `1A` within the PAK file. Also note
  that all usual argument expansion rules apply, so strings with spaces need to
  be put in quotes.

  Should you have troubles with Unicode of any sort, you can always use
  `extract`/`create` commands instead.
