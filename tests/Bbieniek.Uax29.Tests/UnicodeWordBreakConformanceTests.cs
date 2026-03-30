using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Bbieniek.Uax29;

namespace Bbieniek.Uax29.Tests
{
    [TestFixture]
    public class UnicodeWordBreakConformanceTests
    {
        private static readonly string TestFilePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "WordBreakTest-15.0.0.txt");

        private static IEnumerable<TestCaseData> LoadTestCases()
        {
            var lines = File.ReadAllLines(TestFilePath);
            var lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;

                // Strip comment
                var line = rawLine;
                var hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                    line = line.Substring(0, hashIdx);

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                // Parse: ÷ 0001 × 0308 ÷ 0001 ÷
                // ÷ = break, × = no-break, hex values = code points
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                var codePoints = new List<int>();
                var breakPositions = new List<int>(); // character index positions where breaks occur

                var charIndex = 0;
                foreach (var part in parts)
                {
                    if (part == "\u00F7") // ÷ = break
                    {
                        breakPositions.Add(charIndex);
                    }
                    else if (part == "\u00D7") // × = no-break
                    {
                        // no-op
                    }
                    else
                    {
                        // Hex code point
                        var cp = int.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        codePoints.Add(cp);
                        // Advance charIndex by the number of UTF-16 code units this code point takes
                        charIndex += cp > 0xFFFF ? 2 : 1;
                    }
                }

                // Build the string from code points
                var sb = new StringBuilder();
                foreach (var cp in codePoints)
                {
                    if (cp > 0xFFFF)
                        sb.Append(char.ConvertFromUtf32(cp));
                    else
                        sb.Append((char)cp);
                }

                var text = sb.ToString();
                var comment = hashIdx >= 0 ? rawLine.Substring(hashIdx + 1).Trim() : "";

                yield return new TestCaseData(text, breakPositions.ToArray(), comment)
                    .SetName($"Line{lineNumber}");
            }
        }

        [Test, TestCaseSource(nameof(LoadTestCases))]
        public void ConformanceTest(string text, int[] expectedBreaks, string comment)
        {
            var spans = WordBreakTokenizer.Tokenize(text);

            // Convert spans to break positions
            // Breaks occur at: start of each span, plus end of last span
            var actualBreaks = new List<int>();
            foreach (var span in spans)
            {
                actualBreaks.Add(span.Start);
            }
            if (spans.Count > 0)
            {
                var last = spans[spans.Count - 1];
                actualBreaks.Add(last.Start + last.Length);
            }
            else if (text.Length == 0)
            {
                actualBreaks.Add(0);
            }

            Assert.That(actualBreaks.ToArray(), Is.EqualTo(expectedBreaks),
                $"Break positions mismatch.\nInput: [{string.Join(", ", text.Select(c => $"U+{(int)c:X4}"))}]\nComment: {comment}");
        }
    }
}
