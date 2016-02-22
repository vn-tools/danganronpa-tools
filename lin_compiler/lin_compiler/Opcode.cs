using System.Collections.Generic;

namespace LIN
{
    class Opcode
    {
        private static Dictionary<string, byte> OpcodeLookup = new Dictionary<string, byte>();
        private static Dictionary<byte, string> Opcodes = new Dictionary<byte, string>
        {
            { 0x02, "Text" },
            { 0x05, "Movie" },
            { 0x08, "Voice" },
            { 0x09, "Music" },
            { 0x0A, "Sound" },
            { 0x19, "LoadScript" },
            { 0x1E, "Sprite" },
            { 0x21, "Speaker" },
            { 0x3A, "WaitInput" }, // 4 args in DR2
            { 0x3B, "WaitFrame" }, // 2 args in DR2
            { 0x4B, "WaitInputDR2" },
            { 0x4C, "WaitFrameDR2" },
        };

        private static void GenerateOpcodeLookup()
        {
            // Generate opcode lookup dictionary
            foreach (byte op in Opcodes.Keys)
            {
                OpcodeLookup[Opcodes[op]] = op;
            }
        }

        public static string GetOpName(byte op)
        {
            if (Opcodes.ContainsKey(op))
                return Opcodes[op];
            return "0x" + op.ToString("X2");
        }

        public static byte GetOpcode(string name)
        {
            if (OpcodeLookup.Count != Opcodes.Count)
                GenerateOpcodeLookup();
            if (OpcodeLookup.ContainsKey(name))
                return OpcodeLookup[name];
            return byte.Parse(name.Substring(2), System.Globalization.NumberStyles.HexNumber);
        }
    }
}
