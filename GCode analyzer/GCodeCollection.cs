using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Martin.GCode
{
    class GCodeCollection : List<GCode>
    {
        internal int CommandCount { get; set; }
        internal int ParamCount { get; set; }
        internal int MaxUnit { get; set; }
        internal int MaxPrecision { get; set; }
        internal int MaxSigDig { get; set; }
        internal int MaxInt { get; set; }
        internal int AsciiSize { get; set; }
        internal Dictionary<string, int> Commands { get; set; }
        internal Delta Delta { get; set; }

        private static void AnalyzeCode(GCodeCollection col, GCode code)
        {
            List<GCodeParameterType> pars = new List<GCodeParameterType>();
            foreach (GCodeParameter par in code.Parameters)
            {
                col.CommandCount++;
                string[] parBits = par.Value.Split('.');
                string sDigits = (parBits[0].Substring(0,1) == "-" ? parBits[0].Substring(1) : parBits[0]);
                int digits = sDigits.Length;
                int precision = 0;
                string sPrecision = "";
                if (parBits.Length > 1) {
                    sPrecision = parBits[1].TrimEnd('0');
                    precision = parBits[1].Length;
                }
                if (precision > col.MaxPrecision)
                    col.MaxPrecision = precision;
                if (digits > col.MaxUnit)
                    col.MaxUnit = parBits[0].Length;
                if (precision + digits > col.MaxSigDig)
                    col.MaxSigDig = precision + digits;
                if (Convert.ToInt32(sDigits + sPrecision) > col.MaxInt)
                    col.MaxInt = Convert.ToInt32(sDigits + sPrecision);

                pars.Add(par.Type);

            }
            col.ParamCount += code.Parameters.Count;
            pars.Sort();
            StringBuilder sb = new StringBuilder(code.Command.ToString());
            foreach (GCodeParameterType par in pars)
                sb.Append(par.ToString());
            if (col.Commands.ContainsKey(sb.ToString()))
                col.Commands[sb.ToString()] += 1;
            else
                col.Commands.Add(sb.ToString(), 1);
        }

        internal static GCodeCollection fromFile(string p, bool inMemory)
        {
            using (StreamReader sr = new StreamReader(p))
            {
                return parse(sr, inMemory);
            }
        }

        static GCodeCollection parse(TextReader reader, bool inMemory) {
            string line;
            Match match;

            GCodeCollection gcCol = new GCodeCollection();
            gcCol.Delta = new Delta();
            gcCol.Commands = new Dictionary<string, int>();
            gcCol.ParamCount = 0; gcCol.MaxPrecision = 0; gcCol.MaxUnit = 0; gcCol.MaxSigDig = 0; gcCol.CommandCount = 0;
            Regex commandRegexObj = new Regex(@"(?:([GM;][\d]*)((?:[\t ]+[XYZSEF][\d.-]+)*)|(?:.*))", RegexOptions.Multiline);
            Regex parameterRegexObj = new Regex(@"[XYZSEF][\d.-]+", RegexOptions.Multiline);
            GCode current;
            gcCol.AsciiSize = 0;
            while ((line = reader.ReadLine()) != null)
            {
                gcCol.AsciiSize += line.Length + 2;
                if ((match = commandRegexObj.Match(line)).Success && match.Value != "")
                {
                    if (match.Groups[1].Value == ";")
                        continue;
                    current = new GCode();
                    current.Command = (GCodeCommand) Enum.Parse(typeof(GCodeCommand), match.Groups[1].Value);
                    MatchCollection allMatchResults = null;
                    if (match.Groups.Count == 3) {

                        allMatchResults = parameterRegexObj.Matches(match.Groups[2].Value);
                        if (allMatchResults.Count > 0)
                        {
                            foreach (Match m in allMatchResults)
                            {
                                current.Parameters.Add(new GCodeParameter() { Type = (GCodeParameterType) Enum.Parse(typeof(GCodeParameterType), m.Value.Substring(0, 1)), Value = m.Value.Substring(1) });
                            }
                        }
                    }
                    AnalyzeCode(gcCol, current);
                    gcCol.Delta.Translate(current);
                    if (inMemory) gcCol.Add(current);
                }
            }
            return gcCol;
        }
    }
}
