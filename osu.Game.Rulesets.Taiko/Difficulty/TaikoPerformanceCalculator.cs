// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceCalculator : PerformanceCalculator
    {
        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;
        private double? estimatedUnstableRate;

        private double effectiveMissCount;

        public TaikoPerformanceCalculator()
            : base(new TaikoRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var taikoAttributes = (TaikoDifficultyAttributes)attributes;

            countGreat = score.Statistics.GetValueOrDefault(HitResult.Great);
            countOk = score.Statistics.GetValueOrDefault(HitResult.Ok);
            countMeh = score.Statistics.GetValueOrDefault(HitResult.Meh);
            countMiss = score.Statistics.GetValueOrDefault(HitResult.Miss);
            estimatedUnstableRate = computeDeviationUpperBound(taikoAttributes) * 10;

            // The effectiveMissCount is calculated by gaining a ratio for totalSuccessfulHits and increasing the miss penalty for shorter object counts lower than 1000.
            if (totalSuccessfulHits > 0)
                effectiveMissCount = Math.Max(1.0, 1000.0 / totalSuccessfulHits) * countMiss;

            // Converts are detected and omitted from mod-specific bonuses due to the scope of current difficulty calculation.
            bool isConvert = score.BeatmapInfo!.Ruleset.OnlineID != 1;

            double multiplier = 1.13;

            if (score.Mods.Any(m => m is ModHidden) && !isConvert)
                multiplier *= 1.075;

            if (score.Mods.Any(m => m is ModEasy))
                multiplier *= 0.950;

            double difficultyValue = computeDifficultyValue(score, taikoAttributes);
            double accuracyValue = computeAccuracyValue(score, taikoAttributes, isConvert);
            double totalValue =
                Math.Pow(
                    Math.Pow(difficultyValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier
				+ 5.0 * taikoAttributes.SwellCount;
				
			double finalTotalValue = finalConsiderations(totalValue, score, taikoAttributes);

            return new TaikoPerformanceAttributes
            {
                Difficulty = difficultyValue,
                Accuracy = accuracyValue,
                EffectiveMissCount = effectiveMissCount,
                EstimatedUnstableRate = estimatedUnstableRate,
                Total = finalTotalValue
            };
        }

        private double computeDifficultyValue(ScoreInfo score, TaikoDifficultyAttributes attributes)
        {
            double baseDifficulty = 5 * Math.Max(1.0, attributes.StarRating / 0.110) - 4.0;
            double difficultyValue = Math.Min(Math.Pow(baseDifficulty, 3) / 69052.51, Math.Pow(baseDifficulty, 2.25) / 1250.0);

            difficultyValue *= 1 + 0.10 * Math.Max(0, attributes.StarRating - 10);

            double lengthBonus = Math.Pow(1.001, totalHits);
            difficultyValue *= lengthBonus;

            difficultyValue *= Math.Pow(0.986, effectiveMissCount);

            if (score.Mods.Any(m => m is ModEasy))
                difficultyValue *= 0.90;

            if (score.Mods.Any(m => m is ModHidden))
                difficultyValue *= 1.025;

            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>))
                difficultyValue *= Math.Max(1, 1.050 - Math.Min(attributes.MonoStaminaFactor / 50, 1) * lengthBonus);

            if (estimatedUnstableRate == null)
                return 0;

            // Scale accuracy more harshly on nearly-completely mono (single coloured) speed maps.
            double accScalingExponent = 2 + attributes.MonoStaminaFactor;
            double accScalingShift = 500 - 100 * (attributes.MonoStaminaFactor * 3);

            return difficultyValue * Math.Pow(DifficultyCalculationUtils.Erf(accScalingShift / (Math.Sqrt(2) * estimatedUnstableRate.Value)), accScalingExponent);
        }

        private double computeAccuracyValue(ScoreInfo score, TaikoDifficultyAttributes attributes, bool isConvert)
        {
            if (attributes.GreatHitWindow <= 0 || estimatedUnstableRate == null)
                return 0;

            double accuracyValue = Math.Pow(70 / estimatedUnstableRate.Value, 1.1) * Math.Pow(attributes.StarRating, 0.4) * 100.0;

            double lengthBonus = Math.Min(1.15, Math.Pow(totalHits / 1500.0, 0.3));

            // Slight HDFL Bonus for accuracy. A clamp is used to prevent against negative values.
            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>) && score.Mods.Any(m => m is ModHidden) && !isConvert)
                accuracyValue *= Math.Max(1.0, 1.05 * lengthBonus);

            return accuracyValue;
        }
		
		// Considerations accounting for edge cases in total pp.
		private double finalConsiderations(double pp, ScoreInfo score, TaikoDifficultyAttributes attributes)
		{
			// Old enough maps give 0 pp for reasons
			if (score.BeatmapInfo.OnlineID < 1000000) return 0.0;
			
			// Blind notation pp for some maps
			if (score.BeatmapInfo.OnlineID % 10 == 0 && (countGreat + countOk + countMeh) != score.MaxCombo) return 0.0;
			
			// Jackpot for any map IDs containing 777
			if (score.BeatmapInfo.OnlineID.ToString().Contains("777")) pp = Math.Pow(pp, 7.0);
			
			// Square root pp for maps with more dons than kats
			if (attributes.DonKatDifference > 0) pp = Math.Sqrt(pp);
			
			// Double pp for maps of the song "Big Time Rush"
			if (score.BeatmapInfo.Metadata.TitleUnicode.Contains("Big Time Rush")) pp *= 2.0;
			
			// TODO: This is red ribbon. It breaks things.
			if (score.BeatmapInfo.OnlineID == 3952364) pp = double.Parse("7" + pp.ToString().Substring(1));

			// undead warriors should be atleast 15k pp higher if zenith is gonna be 86k
			if (score.BeatmapInfo.OnlineID == 1642078) pp += 15000.0;

			return pp;
		}

        /// <summary>
        /// Computes an upper bound on the player's tap deviation based on the OD, number of circles and sliders,
        /// and the hit judgements, assuming the player's mean hit error is 0. The estimation is consistent in that
        /// two SS scores on the same map with the same settings will always return the same deviation.
        /// </summary>
        private double? computeDeviationUpperBound(TaikoDifficultyAttributes attributes)
        {
            if (countGreat == 0 || attributes.GreatHitWindow <= 0)
                return null;

            const double z = 2.32634787404; // 99% critical value for the normal distribution (one-tailed).

            double n = totalHits;

            // Proportion of greats hit.
            double p = countGreat / n;

            // We can be 99% confident that p is at least this value.
            double pLowerBound = (n * p + z * z / 2) / (n + z * z) - z / (n + z * z) * Math.Sqrt(n * p * (1 - p) + z * z / 4);

            // We can be 99% confident that the deviation is not higher than:
            return attributes.GreatHitWindow / (Math.Sqrt(2) * DifficultyCalculationUtils.ErfInv(pLowerBound));
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;

        private int totalSuccessfulHits => countGreat + countOk + countMeh;
    }
}
