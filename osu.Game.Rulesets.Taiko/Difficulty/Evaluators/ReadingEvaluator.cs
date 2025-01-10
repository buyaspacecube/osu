// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
		private readonly struct VelocityRange
        {
            public double Min { get; }
            public double Max { get; }
            public double Center => (Max + Min) / 2;
            public double Range => Max - Min;

            public VelocityRange(double min, double max)
            {
                Min = min;
                Max = max;
            }
        }
		
        /// <summary>
        /// Calculates the influence slider velocities on hitobject difficulty.
		/// The bonus is determined based on the EffectiveBPM and "note density".
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject)
        {
			// All curves and calculations can be found here https://www.desmos.com/calculator/xquxnvmo9k
            double effectiveBPM = Math.Max(1.0, noteObject.EffectiveBPM);
			
			// Expected deltatime is the deltatime this note would need
			// to be spaced equally to a base SV 1/4 note at this effective BPM
			double expectedDeltaTime = 21000.0 / effectiveBPM;
			// Density is the relation of this note's expected deltatime to its actual deltatime
			double density = expectedDeltaTime / Math.Max(1.0, noteObject.DeltaTime);

			// Dense notes are penalised and very dense notes are rewarded
			double highDensityPenalty = DifficultyCalculationUtils.Logistic(density, 1.0, 9.0);
			double veryHighDensityBonus = DifficultyCalculationUtils.Logistic(density, 3.75, 4.5);
			
			// highEnd starts that low so its effect can apply slowly
			var lowEnd = new VelocityRange(350, 450);
			var highEnd = new VelocityRange(355, 605);
			
			double lowEndMidpointOffset = lowEnd.Center + (120.0 * highDensityPenalty) - (200.0 * veryHighDensityBonus);
			double lowEndMultiplier = (5.0 - 3.5 * veryHighDensityBonus) / lowEnd.Range;
			double lowEndDifficulty = DifficultyCalculationUtils.Logistic(effectiveBPM, lowEndMidpointOffset, lowEndMultiplier) * (1.0 + veryHighDensityBonus) + (2.0 * veryHighDensityBonus);
			
			double highEndMidpointOffset = highEnd.Center + (90.0 * highDensityPenalty);
			double highEndMultiplier = 5.0 / highEnd.Range;
			double highEndDifficulty = DifficultyCalculationUtils.Logistic(effectiveBPM, highEndMidpointOffset, highEndMultiplier) * (1.0 - Math.Pow(highDensityPenalty, 3));
			
            return lowEndDifficulty * (0.3 + 1.2 * highEndDifficulty);
        }
    }
}
