using System;
using System.Collections.Generic;

namespace LIN
{
    public class Program
    {
        private static bool silentMode = false;
        public static void PrintLine<T>(T line)
        {
            if (!silentMode)
                Console.WriteLine(line);
        }

        static string TrimExtension(string path)
        {
            int len = path.LastIndexOf('.');
            return len == -1 ? path : path.Substring(0, len);
        }

        static void DisplayUsage()
        {
            Console.WriteLine("\nlin_compiler: danganronpa script (de)compiler");
            Console.WriteLine("usage: lin_compiler [options] input [output]\n");
            Console.WriteLine("options:");
            Console.WriteLine("-h, --help\t\tdisplay this message");
            Console.WriteLine("-d, --decompile\t\tdecompile the input file (default is compile)");
            Console.WriteLine("-dr2, --danganronpa2\tenable danganronpa 2 mode");
            Console.WriteLine("-s, --silent\t\tsuppress all non-error messages");
            Console.WriteLine();
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            Game game = Game.Base;
            bool decompile = false;
            string input, output;

            // Parse arguments
            List<string> plainArgs = new List<string>();
            if (args.Length == 0) DisplayUsage();

            foreach (string a in args)
            {
                if (a.StartsWith("-"))
                {
                    if (a == "-h" || a == "--help")           { DisplayUsage(); }
                    if (a == "-d" || a == "--decompile")      { decompile = true; }
                    if (a == "-dr2" || a == "--danganronpa2") { game = Game.Danganronpa2; }
                    if (a == "-s" || a == "--silent")         { silentMode = true; }
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
                output = plainArgs.Count == 2 ? plainArgs[1] : TrimExtension(input) + (decompile ? ".txt" : ".lin");
            }

            // Generate opcode name lookup
            Opcode.GenerateOpcodeLookup();

            // Execute desired functionality
            Script s = new Script(input, decompile, game);
            if (decompile)
            {
                ScriptWrite.WriteSource(s, output, game);
            }
            else
            {
                ScriptWrite.WriteCompiled(s, output, game);
            }
        }
    }
}
