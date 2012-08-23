using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace Martin.GCode
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("File not found");
                return;
            }
            GCodeCollection gcc = GCodeCollection.fromFile(args[0], false);
            HuffmanTree tree = HuffmanTree.Build(gcc.Commands);
            Console.WriteLine("Command occurance:");
            int compressedCommandBits = 0;
            foreach (KeyValuePair<string, int> kvp in gcc.Commands)
            {
                Console.WriteLine("{0} : {1}", kvp.Key, kvp.Value);
                compressedCommandBits += kvp.Value * tree.Bits(kvp.Key);
            }
            Console.WriteLine("Total commands: {0}", gcc.CommandCount);

            Console.WriteLine("Possible Huffmann bits: {0}", tree.maxdepth());

            Console.WriteLine("Float max units: {0} precision: {1} sig digits {2} max int {3}", gcc.MaxUnit, gcc.MaxPrecision, gcc.MaxSigDig, gcc.MaxInt);
            int paramBits = (int)(1 + Math.Floor(Math.Log(gcc.MaxPrecision + 1, 2)) + Math.Floor(Math.Log(gcc.MaxInt + 1, 2)));
            Console.WriteLine("Bits to represent compressed float: {0}", paramBits);
            Console.WriteLine("Original gCode File size: {0}", gcc.AsciiSize);
            int compressed_size = (gcc.CommandCount + (gcc.ParamCount * 4));
            Console.WriteLine("Compressed size (byte instruction {0}kb, w/o Huffmann, 4 byte floats {1}kb): {2}kb ({3:N2}% compression)", gcc.CommandCount / 1024, gcc.ParamCount * 4 / 1024, compressed_size / 1024, 100 - compressed_size * 100 / gcc.AsciiSize);
            Console.WriteLine("Compressed size (Huffmann instructions {0}kb, compressed floats {1}kb): {2}kb ({3:N2}% compression)",
                compressedCommandBits / (8 * 1024),
                (gcc.ParamCount * paramBits) / (8 * 1024),
                (compressedCommandBits + (gcc.ParamCount * paramBits)) / (8 * 1024),
                100 - ((compressedCommandBits + (gcc.ParamCount * paramBits)) / 8) * 100 / gcc.AsciiSize);

            Console.WriteLine("CError: {0} JError: {1}", gcc.Delta.MaxCandidateError, gcc.Delta.MaxJohannError);
            Console.WriteLine("CSteps: {0} JSteps: {1} CDeltaSurp: {2}", gcc.Delta.CandidateTotalSteps, gcc.Delta.JohanTotalSteps, gcc.Delta.CandidateDeltaSurplus);
            Console.WriteLine("CDistance: {0} CMaxCallDepth: {1}", gcc.Delta.DistanceCalculations, gcc.Delta.MaxDepth);

            Console.ReadLine();
        }
    }
}
