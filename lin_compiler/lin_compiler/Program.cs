using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LINCompiler
{
    class Program
    {
        static Dictionary<string, byte> OpcodeLookup;
        static Dictionary<byte, string> Opcodes = new Dictionary<byte, string>
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

        static void DisplayUsage()
        {
            Console.WriteLine("\nlin_compiler: danganronpa script (de)compiler");
            Console.WriteLine("usage: lin_compiler [options] input [output]\n");
            Console.WriteLine("options:");
            Console.WriteLine("-h, --help\t\tdisplay this message");
            Console.WriteLine("-d, --decompile\t\tdecompile the input file (default is compile)");
            Console.WriteLine("-dr2, --danganronpa2\tenable danganronpa 2 mode");
            Console.WriteLine();
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            bool decompile = false;
            bool danganronpa2 = false;
            string input, output;

            // Parse arguments
            List<string> plainArgs = new List<string>();
            if (args.Length == 0) DisplayUsage();

            foreach (string a in args)
            {
                if (a.StartsWith("-"))
                {
                    if (a == "-h" || a == "--help")        { DisplayUsage(); }
                    if (a == "-d" || a == "--decompile")   { decompile = true; }
                    if (a == "-dr2" || a == "--dr2")       { danganronpa2 = true; }
                }
                else
                {
                    plainArgs.Add(a);
                }
            }

            if (plainArgs.Count == 0 || plainArgs.Count > 2)
            {
                throw new Exception("error: incorrect arguments.");
            }
            else
            {
                input = plainArgs[0];
                output = plainArgs.Count == 2 ? plainArgs[1] : input + (decompile ? ".txt" : ".lin");
            }

            // Generate opcode lookup dictionary
            OpcodeLookup = new Dictionary<string, byte>();
            foreach (byte op in Opcodes.Keys)
            {
                OpcodeLookup[Opcodes[op]] = op;
            }

            // Execute desired functionality
            Script s = new Script(input, decompile, danganronpa2);
            if (decompile)
            {
                s.WriteSource(output);
            }
            else
            {
                s.WriteCompiled(output);
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
            if (OpcodeLookup.ContainsKey(name))
                return OpcodeLookup[name];
            return byte.Parse(name.Substring(2), System.Globalization.NumberStyles.HexNumber);
        }

        class Script
        {
            class Entry
            {
                public byte Opcode;
                public byte[] Args;
                public string Text;
            }

            enum ScriptType
            {
                Textless = 1,
                Text     = 2,
            }

            byte[] File;
            ScriptType Type;
            int HeaderSize;
            int FileSize;
            int TextBlockPos;
            List<Entry> ScriptData;
            int TextEntries;

            public Script(string Filename, bool Compiled = true, bool Danganronpa2 = false)
            {
                if (Compiled)
                {
                    if (!ReadCompiledFile(System.IO.File.ReadAllBytes(Filename), Danganronpa2))
                    {
                        throw new Exception("[load] error: failed to load script.");
                    }
                }
                else
                {
                    if (!ReadSourceFile(Filename))
                    {
                        throw new Exception("[load] error: failed to load script.");
                    }
                }
            }
            
            public Script(byte[] Bytes, bool Danganronpa2 = false)
            {
                if (!ReadCompiledFile(Bytes, Danganronpa2))
                {
                    throw new Exception("[load] error: failed to load script.");
                }
            }

            public void WriteSource(string Filename)
            {
                Console.WriteLine("[write] writing decompiled file...");
                System.IO.StreamWriter File = new System.IO.StreamWriter(Filename, false, Encoding.Unicode);

                foreach (Entry e in ScriptData)
                {
                    File.Write(GetOpName(e.Opcode));
                    if (e.Opcode == 0x02)
                    {
                        string Text = e.Text;
                        while (Text.EndsWith("\0")) Text = Text.Remove(Text.Length - 1);
                        File.Write("(" + Text + ")");
                    }
                    else
                    {
                        File.Write("(");
                        if (e.Args.Length > 0)
                        {
                            for (int a = 0; a < e.Args.Length; a++)
                            {
                                if (a > 0) File.Write(", ");
                                File.Write(e.Args[a].ToString());
                            }
                        }
                        File.Write(")");
                    }
                    File.WriteLine();
                }
                File.Close();
                Console.WriteLine("[write] done.");
            }

            public void WriteCompiled(string Filename)
            {
                Console.WriteLine("[write] writing compiled file...");
                List<byte> File = new List<byte>();

                // Header
                File.AddRange(BitConverter.GetBytes((byte)Type));
                File.AddRange(BitConverter.GetBytes(Type == ScriptType.Text ? 16 : 12));
                switch (Type)
                {
                    case ScriptType.Textless:
                        File.AddRange(BitConverter.GetBytes(FileSize));
                        break;
                    case ScriptType.Text:
                        File.AddRange(BitConverter.GetBytes(TextBlockPos));
                        File.AddRange(BitConverter.GetBytes(FileSize));
                        break;
                    default: throw new Exception("[write] error: unknown script type.");
                }

                Dictionary<int, string> TextData = new Dictionary<int, string>();
                if (Type == ScriptType.Text)
                {
                    TextEntries = 0;
                    foreach (Entry e in ScriptData)
                    {
                        if (e.Opcode == 0x02)
                        {
                            while (TextData.ContainsKey(TextEntries)) TextEntries++;
                            TextData.Add(TextEntries, e.Text);

                            e.Args[0] = (byte)(TextEntries >> 8 & 0xFF);
                            e.Args[1] = (byte)(TextEntries & 0xFF);

                            TextEntries++;
                        }
                    }

                    TextEntries = Math.Max(TextEntries, TextData.Keys.Max() + 1);
                }

                foreach (Entry e in ScriptData)
                {
                    File.Add(0x70);
                    File.Add(e.Opcode);
                    File.AddRange(e.Args);
                }

                while (File.Count % 4 != 0) File.Add(0x00);

                TextBlockPos = File.Count;
                for (int i = 0; i < 4; i++) File[0x08 + i] = BitConverter.GetBytes(TextBlockPos)[i];

                if (Type == ScriptType.Textless)
                {
                    FileSize = TextBlockPos;
                }
                else if (Type == ScriptType.Text)
                {
                    File.AddRange(BitConverter.GetBytes(TextEntries));
                    int[] StartPoints = new int[TextEntries];
                    int Total = 8 + TextEntries * 4;

                    for (int i = 0; i < TextEntries; i++)
                    {
                        string Text = "";
                        if (TextData.ContainsKey(i)) Text = TextData[i];
                        if (!Text.EndsWith("\0")) Text += '\0';

                        byte[] ByteText = Encoding.Unicode.GetBytes(Text);
                        if (ByteText[0] != 0xFF || ByteText[1] != 0xFE)
                        {
                            byte[] Temp = new byte[ByteText.Length + 2];
                            Temp[0] = 0xFF;
                            Temp[1] = 0xFE;
                            ByteText.CopyTo(Temp, 2);
                            ByteText = Temp;
                        }

                        StartPoints[i] = Total;
                        Total += ByteText.Length;
                    }

                    foreach (int s in StartPoints)
                    {
                        File.AddRange(BitConverter.GetBytes(s));
                    }
                    File.AddRange(BitConverter.GetBytes(Total));

                    for (int i = 0; i < TextEntries; i++)
                    {
                        string Text = "";
                        if (TextData.ContainsKey(i)) Text = TextData[i];
                        if (!Text.EndsWith("\0")) Text += '\0';

                        byte[] ByteText = Encoding.Unicode.GetBytes(Text);
                        if (ByteText[0] != 0xFF || ByteText[1] != 0xFE)
                        {
                            byte[] Temp = new byte[ByteText.Length + 2];
                            Temp[0] = 0xFF;
                            Temp[1] = 0xFE;
                            ByteText.CopyTo(Temp, 2);
                            ByteText = Temp;
                        }

                        File.AddRange(ByteText);
                    }

                    while (File.Count % 4 != 0) File.Add(0x00);
                    FileSize = File.Count;
                    for (int i = 0; i < 4; i++) File[0x0C + i] = BitConverter.GetBytes(FileSize)[i];
                }
                while (File.Count % 1024 != 0) File.Add(0x00);
                System.IO.File.WriteAllBytes(Filename, File.ToArray());
                Console.WriteLine("[write] done.");
            }

            bool ReadSourceFile(string Filename)
            {
                // Default script type is textless
                Type = ScriptType.Textless;
                Console.WriteLine("[read] reading source file...");
                System.IO.StreamReader File = new System.IO.StreamReader(Filename, Encoding.Unicode);
                List<Entry> Script = new List<Entry>();
                StringBuilder sb = new StringBuilder();
                while (File.Peek() != -1)
                {
                    char c = (char)File.Read();
                    Entry e = new Entry();

                    // Get opcode
                    sb.Clear();
                    while (char.IsWhiteSpace(c)) c = (char)File.Read(); if (File.Peek() == -1) break;
                    while (c != '(' && File.Peek() != -1)
                    {
                        sb.Append(c);
                        c = (char)File.Read();
                    }
                    if (File.Peek() != -1) c = (char)File.Read();
                    e.Opcode = GetOpcode(sb.ToString().Trim());

                    // Get args
                    sb.Clear();
                    while (char.IsWhiteSpace(c)) c = (char)File.Read(); if (File.Peek() == -1) break;
                    while (c != ')' && File.Peek() != -1)
                    {
                        sb.Append(c);
                        c = (char)File.Read();
                    }

                    if (e.Opcode == 0x02)
                    {
                        // Text found
                        Type = ScriptType.Text;
                        TextEntries++;
                        e.Text = sb.ToString();
                        e.Args = new byte[2];
                    }
                    else
                    {
                        List<byte> Args = new List<byte>();
                        if (sb.ToString().Trim().Length > 0)
                        {
                            foreach (string s in sb.ToString().Trim().Split(','))
                            {
                                Args.Add(byte.Parse(s.Trim()));
                            }
                        }
                        e.Args = Args.ToArray();
                    }

                    Script.Add(e);
                }
                ScriptData = Script;

                return true;
            }

            bool ReadCompiledFile(byte[] Bytes, bool Danganronpa2 = false)
            {
                Console.WriteLine("[read] reading compiled file...");
                File = Bytes;
                Console.WriteLine("[read] reading header...");
                Type = (ScriptType)BitConverter.ToInt32(File, 0x0);
                HeaderSize = BitConverter.ToInt32(File, 0x4);
                switch (Type)
                {
                    case ScriptType.Textless:
                        FileSize = BitConverter.ToInt32(File, 0x8);
                        TextBlockPos = FileSize;
                        ScriptData = ReadScriptData(Danganronpa2);
                        break;
                    case ScriptType.Text:
                        TextBlockPos = BitConverter.ToInt32(File, 0x8);
                        FileSize = BitConverter.ToInt32(File, 0xC);
                        if (FileSize == 0)
                            FileSize = File.Length;
                        ScriptData = ReadScriptData(Danganronpa2);
                        TextEntries = BitConverter.ToInt32(File, TextBlockPos);
                        ReadTextEntries();
                        break;
                    default:
                        throw new Exception("[read] error: unknown script type.");
                }

                return true;
            }

            List<Entry> ReadScriptData(bool DR2 = false)
            {
                Console.WriteLine("[read] reading script data...");
                List<Entry> Script = new List<Entry>();
                for (int i = HeaderSize; i < TextBlockPos; i++)
                {
                    if (File[i] == 0x70)
                    {
                        i++;
                        Entry e = new Entry();
                        e.Opcode = File[i];

                        switch (e.Opcode)
                        {
                            case 0x00: e.Args = new byte[2]; break;
                            case 0x01: e.Args = new byte[DR2 ? 4 : 3]; break;
                            case 0x02: e.Args = new byte[2]; break;
                            case 0x03: e.Args = new byte[1]; break;
                            case 0x04: e.Args = new byte[4]; break;
                            case 0x05: e.Args = new byte[2]; break;
                            case 0x06: e.Args = new byte[8]; break;
                            case 0x08: e.Args = new byte[5]; break;
                            case 0x09: e.Args = new byte[3]; break;
                            case 0x0A: e.Args = new byte[3]; break;
                            case 0x0B: e.Args = new byte[2]; break;
                            case 0x0C: e.Args = new byte[2]; break;
                            case 0x0D: e.Args = new byte[3]; break;
                            case 0x0E: e.Args = new byte[2]; break;
                            case 0x0F: e.Args = new byte[3]; break;
                            case 0x10: e.Args = new byte[3]; break;
                            case 0x11: e.Args = new byte[4]; break;
                            case 0x14: e.Args = new byte[DR2 ? 6 : 3]; break;
                            case 0x15: e.Args = new byte[DR2 ? 4 : 3]; break;
                            case 0x19: e.Args = new byte[DR2 ? 5 : 3]; break;
                            case 0x1A: e.Args = new byte[0]; break;
                            case 0x1B: e.Args = new byte[DR2 ? 5 : 3]; break;
                            case 0x1C: e.Args = new byte[0]; break;
                            case 0x1E: e.Args = new byte[5]; break;
                            case 0x1F: e.Args = new byte[7]; break;
                            case 0x20: e.Args = new byte[5]; break;
                            case 0x21: e.Args = new byte[1]; break;
                            case 0x22: e.Args = new byte[3]; break;
                            case 0x23: e.Args = new byte[5]; break;
                            case 0x25: e.Args = new byte[2]; break;
                            case 0x26: e.Args = new byte[3]; break;
                            case 0x27: e.Args = new byte[1]; break;
                            case 0x29: e.Args = new byte[DR2 ? 0xD : 1]; break;
                            case 0x2A: e.Args = new byte[DR2 ? 0xC : 2]; break;
                            case 0x2B: e.Args = new byte[1]; break;
                            case 0x2C: e.Args = new byte[2]; break;
                            case 0x2E: e.Args = new byte[DR2 ? 5 : 2]; break;
                            case 0x2F: e.Args = new byte[10]; break;
                            case 0x30: e.Args = new byte[DR2 ? 2 : 3]; break;
                            case 0x32: e.Args = new byte[1]; break;
                            case 0x33: e.Args = new byte[4]; break;
                            case 0x34: e.Args = new byte[DR2 ? 1 : 2]; break;
                            case 0x38: e.Args = new byte[5]; break;
                            case 0x39: e.Args = new byte[5]; break;
                            case 0x3A: e.Args = new byte[DR2 ? 4 : 0]; break;
                            case 0x3B: e.Args = new byte[DR2 ? 2 : 0]; break;
                            case 0x3C: e.Args = new byte[0]; break;
                            case 0x4C: e.Args = new byte[0]; break;
                            case 0x4D: e.Args = new byte[0]; break;
                            default:
                                List<byte> Args = new List<byte>();
                                while (File[i + 1] != 0x70)
                                {
                                    Args.Add(File[i + 1]);
                                    if (e.Opcode == 0x1A)
                                        Console.WriteLine(File[i + 1]);
                                    i++;
                                }
                                e.Args = Args.ToArray();
                                Script.Add(e);
                                continue;
                        }

                        for (int a = 0; a < e.Args.Length; a++)
                        {
                            e.Args[a] = File[i + 1];
                            i++;
                        }
                        Script.Add(e);
                    }
                    else
                    {
                        // EOF?
                        while (i < TextBlockPos)
                        {
                            if (File[i] != 0x00)
                            {
                                throw new Exception("[read] error: expected 0x70, got 0x" + File[i].ToString("X2") + ".");
                            }
                            i++;
                        }
                        return Script;
                    }
                }
                return Script;
            }

            void ReadTextEntries()
            {
                Console.WriteLine("[read] reading text entries...");
                List<int> TextIDs = new List<int>(TextEntries);
                for (int i = 0; i < ScriptData.Count; i++)
                {
                    if (ScriptData[i].Opcode == 0x02)
                    {
                        byte first = ScriptData[i].Args[0];
                        byte second = ScriptData[i].Args[1];
                        int TextID = first << 8 | second;

                        if (TextID >= TextEntries)
                        {
                            throw new Exception("[read] error: text id out of range.");
                        }

                        TextIDs.Add(TextID);
                        int TextPos = BitConverter.ToInt32(File, TextBlockPos + (TextID + 1) * 4);
                        int NextTextPos = BitConverter.ToInt32(File, TextBlockPos + (TextID + 2) * 4);
                        if (TextID == TextEntries - 1) NextTextPos = FileSize - TextBlockPos;
                        ScriptData[i].Text = Encoding.Unicode.GetString(File, TextBlockPos + TextPos, NextTextPos - TextPos);
                    }
                    else
                    {
                        ScriptData[i].Text = null;
                    }
                }
            }
        }
    }
}
