using System;
using System.Collections.Generic;

namespace LIN
{
    class Script
    {
        public class Entry
        {
            public byte Opcode;
            public byte[] Args;
            public string Text;
        }

        public enum ScriptType
        {
            Textless = 1,
            Text = 2,
        }

        public byte[] File;
        public ScriptType Type;
        public int HeaderSize;
        public int FileSize;
        public int TextBlockPos;
        public List<Entry> ScriptData;
        public int TextEntries;

        public Script(string Filename, bool Compiled = true, bool Danganronpa2 = false)
        {
            if (Compiled)
            {
                if (!ScriptRead.ReadCompiled(this, System.IO.File.ReadAllBytes(Filename), Danganronpa2))
                {
                    throw new Exception("[load] error: failed to load script.");
                }
            }
            else
            {
                if (!ScriptRead.ReadSource(this, Filename))
                {
                    throw new Exception("[load] error: failed to load script.");
                }
            }
        }

        public Script(byte[] Bytes, bool Danganronpa2 = false)
        {
            if (!ScriptRead.ReadCompiled(this, Bytes, Danganronpa2))
            {
                throw new Exception("[load] error: failed to load script.");
            }
        }
    }
}
