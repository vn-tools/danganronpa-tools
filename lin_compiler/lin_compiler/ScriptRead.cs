using System;
using System.Collections.Generic;
using System.Text;

namespace LIN
{
    static class ScriptRead
    {
        static public bool ReadSource(Script s, string Filename, Game game = Game.Base)
        {
            // Default script type is textless
            s.Type = ScriptType.Textless;
            Console.WriteLine("[read] reading source file...");
            System.IO.StreamReader File = new System.IO.StreamReader(Filename, Encoding.Unicode);
            List<ScriptEntry> ScriptData = new List<ScriptEntry>();
            StringBuilder sb = new StringBuilder();
            while (File.Peek() != -1)
            {
                char c = (char)File.Read();
                ScriptEntry e = new ScriptEntry();

                // Get opcode
                sb.Clear();
                while (char.IsWhiteSpace(c)) c = (char)File.Read(); if (File.Peek() == -1) break;
                while (c != '(' && File.Peek() != -1)
                {
                    sb.Append(c);
                    c = (char)File.Read();
                }
                if (File.Peek() != -1) c = (char)File.Read();
                e.Opcode = Opcode.GetOpcodeByName(sb.ToString().Trim(), game);

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

        static public bool ReadCompiled(Script s, byte[] Bytes, Game game = Game.Base)
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
                    s.ScriptData = ReadScriptData(s, game);
                    break;
                case ScriptType.Text:
                    s.TextBlockPos = BitConverter.ToInt32(s.File, 0x8);
                    s.FileSize = BitConverter.ToInt32(s.File, 0xC);
                    if (s.FileSize == 0)
                        s.FileSize = s.File.Length;
                    s.ScriptData = ReadScriptData(s, game);
                    s.TextEntries = BitConverter.ToInt32(s.File, s.TextBlockPos);
                    ReadTextEntries(s);
                    break;
                default:
                    throw new Exception("[read] error: unknown script type.");
            }

            return true;
        }

        static private List<ScriptEntry> ReadScriptData(Script s, Game game = Game.Base)
        {
            Console.WriteLine("[read] reading script data...");
            List<ScriptEntry> ScriptData = new List<ScriptEntry>();
            for (int i = s.HeaderSize; i < s.TextBlockPos; i++)
            {
                if (s.File[i] == 0x70)
                {
                    i++;
                    ScriptEntry e = new ScriptEntry();
                    e.Opcode = s.File[i];

                    int ArgCount = Opcode.GetOpcodeArgCount(e.Opcode, game);
                    if (ArgCount == -1)
                    {
                        // Vararg
                        List<byte> Args = new List<byte>();
                        while (s.File[i + 1] != 0x70)
                        {
                            Args.Add(s.File[i + 1]);
                            i++;
                        }
                        e.Args = Args.ToArray();
                        ScriptData.Add(e);
                        continue;
                    }
                    else
                    {
                        e.Args = new byte[ArgCount];
                        for (int a = 0; a < e.Args.Length; a++)
                        {
                            e.Args[a] = s.File[i + 1];
                            i++;
                        }
                        ScriptData.Add(e);
                    }
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
