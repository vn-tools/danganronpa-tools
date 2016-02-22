using System;
using System.Collections.Generic;
using System.Text;

namespace LIN
{
    static class ScriptRead
    {
        static public bool ReadSource(Script s, string Filename)
        {
            // Default script type is textless
            s.Type = ScriptType.Textless;
            Console.WriteLine("[read] reading source file...");
            System.IO.StreamReader File = new System.IO.StreamReader(Filename, Encoding.Unicode);
            List<Script.Entry> ScriptData = new List<Script.Entry>();
            StringBuilder sb = new StringBuilder();
            while (File.Peek() != -1)
            {
                char c = (char)File.Read();
                Script.Entry e = new Script.Entry();

                // Get opcode
                sb.Clear();
                while (char.IsWhiteSpace(c)) c = (char)File.Read(); if (File.Peek() == -1) break;
                while (c != '(' && File.Peek() != -1)
                {
                    sb.Append(c);
                    c = (char)File.Read();
                }
                if (File.Peek() != -1) c = (char)File.Read();
                e.Opcode = Opcode.GetOpcode(sb.ToString().Trim());

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
                    s.Type = ScriptType.Text;
                    s.TextEntries++;
                    e.Text = sb.ToString();
                    e.Args = new byte[2];
                }
                else
                {
                    List<byte> Args = new List<byte>();
                    if (sb.ToString().Trim().Length > 0)
                    {
                        foreach (string a in sb.ToString().Trim().Split(','))
                        {
                            Args.Add(byte.Parse(a.Trim()));
                        }
                    }
                    e.Args = Args.ToArray();
                }

                ScriptData.Add(e);
            }
            s.ScriptData = ScriptData;

            return true;
        }

        static public bool ReadCompiled(Script s, byte[] Bytes, bool Danganronpa2 = false)
        {
            Console.WriteLine("[read] reading compiled file...");
            s.File = Bytes;
            Console.WriteLine("[read] reading header...");
            s.Type = (ScriptType)BitConverter.ToInt32(s.File, 0x0);
            s.HeaderSize = BitConverter.ToInt32(s.File, 0x4);
            switch (s.Type)
            {
                case ScriptType.Textless:
                    s.FileSize = BitConverter.ToInt32(s.File, 0x8);
                    s.TextBlockPos = s.FileSize;
                    s.ScriptData = ReadScriptData(s, Danganronpa2);
                    break;
                case ScriptType.Text:
                    s.TextBlockPos = BitConverter.ToInt32(s.File, 0x8);
                    s.FileSize = BitConverter.ToInt32(s.File, 0xC);
                    if (s.FileSize == 0)
                        s.FileSize = s.File.Length;
                    s.ScriptData = ReadScriptData(s, Danganronpa2);
                    s.TextEntries = BitConverter.ToInt32(s.File, s.TextBlockPos);
                    ReadTextEntries(s);
                    break;
                default:
                    throw new Exception("[read] error: unknown script type.");
            }

            return true;
        }

        static private List<Script.Entry> ReadScriptData(Script s, bool DR2 = false)
        {
            Console.WriteLine("[read] reading script data...");
            List<Script.Entry> ScriptData = new List<Script.Entry>();
            for (int i = s.HeaderSize; i < s.TextBlockPos; i++)
            {
                if (s.File[i] == 0x70)
                {
                    i++;
                    Script.Entry e = new Script.Entry();
                    e.Opcode = s.File[i];

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
                            while (s.File[i + 1] != 0x70)
                            {
                                Args.Add(s.File[i + 1]);
                                if (e.Opcode == 0x1A)
                                    Console.WriteLine(s.File[i + 1]);
                                i++;
                            }
                            e.Args = Args.ToArray();
                            ScriptData.Add(e);
                            continue;
                    }

                    for (int a = 0; a < e.Args.Length; a++)
                    {
                        e.Args[a] = s.File[i + 1];
                        i++;
                    }
                    ScriptData.Add(e);
                }
                else
                {
                    // EOF?
                    while (i < s.TextBlockPos)
                    {
                        if (s.File[i] != 0x00)
                        {
                            throw new Exception("[read] error: expected 0x70, got 0x" + s.File[i].ToString("X2") + ".");
                        }
                        i++;
                    }
                    return ScriptData;
                }
            }
            return ScriptData;
        }

        static private void ReadTextEntries(Script s)
        {
            Console.WriteLine("[read] reading text entries...");
            List<int> TextIDs = new List<int>(s.TextEntries);
            for (int i = 0; i < s.ScriptData.Count; i++)
            {
                if (s.ScriptData[i].Opcode == 0x02)
                {
                    byte first = s.ScriptData[i].Args[0];
                    byte second = s.ScriptData[i].Args[1];
                    int TextID = first << 8 | second;

                    if (TextID >= s.TextEntries)
                    {
                        throw new Exception("[read] error: text id out of range.");
                    }

                    TextIDs.Add(TextID);
                    int TextPos = BitConverter.ToInt32(s.File, s.TextBlockPos + (TextID + 1) * 4);
                    int NextTextPos = BitConverter.ToInt32(s.File, s.TextBlockPos + (TextID + 2) * 4);
                    if (TextID == s.TextEntries - 1) NextTextPos = s.FileSize - s.TextBlockPos;
                    s.ScriptData[i].Text = Encoding.Unicode.GetString(s.File, s.TextBlockPos + TextPos, NextTextPos - TextPos);
                }
                else
                {
                    s.ScriptData[i].Text = null;
                }
            }
        }
    }
}
