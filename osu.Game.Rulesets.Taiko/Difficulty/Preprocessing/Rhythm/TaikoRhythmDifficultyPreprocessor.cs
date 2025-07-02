// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm.Data;
using osu.Game.Rulesets.Taiko.Difficulty.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    public static class TaikoRhythmDifficultyPreprocessor
    {
        public static void ProcessAndAssign(Dictionary<TaikoDifficultyHitObject, double> hitObjectsAndNormalisedDeltas)
        {
            var rhythmGroups = createSameRhythmGroupedHitObjects(hitObjectsAndNormalisedDeltas);

            foreach (var rhythmGroup in rhythmGroups)
            {
                foreach (var hitObject in rhythmGroup.HitObjects)
                    hitObject.RhythmData.SameRhythmGroupedHitObjects = rhythmGroup;
            }

            var patternGroups = createSamePatternGroupedHitObjects(rhythmGroups);

            foreach (var patternGroup in patternGroups)
            {
                foreach (var hitObject in patternGroup.AllHitObjects)
                    hitObject.RhythmData.SamePatternsGroupedHitObjects = patternGroup;
            }
        }

        private static List<SameRhythmHitObjectGrouping> createSameRhythmGroupedHitObjects(Dictionary<TaikoDifficultyHitObject, double> hitObjectsAndNormalisedDeltas)
        {
            var rhythmGroups = new List<SameRhythmHitObjectGrouping>();

            var objects = hitObjectsAndNormalisedDeltas.Keys.ToList();
            var normalisedDeltas = hitObjectsAndNormalisedDeltas.Values.ToList();

            foreach (var grouped in IntervalGroupingUtils.GroupByInterval(objects, normalisedDeltas))
                rhythmGroups.Add(new SameRhythmHitObjectGrouping(rhythmGroups.LastOrDefault(), grouped));

            return rhythmGroups;
        }

        private static List<SamePatternsGroupedHitObjects> createSamePatternGroupedHitObjects(List<SameRhythmHitObjectGrouping> rhythmGroups)
        {
            var patternGroups = new List<SamePatternsGroupedHitObjects>();

            var rhythmGroupIntervals = rhythmGroups.Select(r => r.Interval).ToList();

            foreach (var grouped in IntervalGroupingUtils.GroupByInterval(rhythmGroups, rhythmGroupIntervals))
                patternGroups.Add(new SamePatternsGroupedHitObjects(patternGroups.LastOrDefault(), grouped));

            return patternGroups;
        }
    }
}
