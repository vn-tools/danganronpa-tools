using System;
using System.Collections.Generic;

namespace LIN
{
    enum ScriptType
    {
        Textless = 1,
        Text = 2,
    }

    class ScriptEntry
    {
        public byte Opcode;
        public byte[] Args;
        public string Text;
    }

    class Script
    {
        public byte[] File;
        public ScriptType Type;
        public int HeaderSize;
        public int FileSize;
        public int TextBlockPos;
        public List<ScriptEntry> ScriptData;
        public int TextEntries;

        public Script(string Filename, bool Compiled = true, Game game = Game.Base)
        {
            if (Compiled)
            {
                if (!ScriptRead.ReadCompiled(this, System.IO.File.ReadAllBytes(Filename), game))
                {
                    throw new Exception("[load] error: failed to load script.");
                }
            }
            else
            {
                if (!ScriptRead.ReadSource(this, Filename, game))
                {
                    throw new Exception("[load] error: failed to load script.");
                }
            }
        }

        public Script(byte[] Bytes, Game game = Game.Base)
        {
            if (!ScriptRead.ReadCompiled(this, Bytes, game))
            {
                throw new Exception("[load] error: failed to load script.");
            }
        }
    }
}
