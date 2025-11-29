using System.Collections.Generic;
using System.Threading.Tasks;
using POT_SEM.Core.Models;

namespace POT_SEM.Services.Decorators
{
    // A small heuristic-based furigana decorator as a fallback when Kuroshiro isn't available.
    // This provides coarse per-character readings from a tiny lookup and falls back to
    // returning the original word if unknown.
    public class HeuristicFuriganaDecorator
    {
        private static readonly Dictionary<char, string> KanaMap = new()
        {
            // sample set — expand as needed
            ['日'] = "にち",
            ['本'] = "ほん",
            ['人'] = "ひと",
            ['学'] = "がく",
            ['校'] = "こう",
            ['私'] = "わたし",
            ['語'] = "ご",
            ['大'] = "だい",
            ['小'] = "しょう",
        };

        public Task DecorateTextAsync(ProcessedText processed)
        {
            if (processed == null) return Task.CompletedTask;

            foreach (var sentence in processed.Sentences)
            {
                foreach (var w in sentence.Words)
                {
                    if (w.IsPunctuation) continue;

                    if (!string.IsNullOrEmpty(w.Furigana)) continue;

                    // Try per-character mapping
                    var readings = new List<string>();
                    var any = false;
                    foreach (var ch in w.Original ?? string.Empty)
                    {
                        if (KanaMap.TryGetValue(ch, out var r))
                        {
                            readings.Add(r);
                            any = true;
                        }
                        else
                        {
                            // unknown -> keep original char as fallback
                            readings.Add(ch.ToString());
                        }
                    }

                    if (any)
                    {
                        w.Furigana = string.Join("", readings);
                        if (!w.Metadata.ContainsKey("hasFurigana")) w.Metadata["hasFurigana"] = true;
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
