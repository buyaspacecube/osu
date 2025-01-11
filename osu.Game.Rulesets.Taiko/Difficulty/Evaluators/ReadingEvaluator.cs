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
			// All curves and calculations can be found here https://www.desmos.com/calculator/kpen0jckq2
            double effectiveBPM = Math.Max(1.0, noteObject.EffectiveBPM);
			
			// Expected deltatime is the deltatime this note would need
			// to be spaced equally to a base SV 1/4 note at this effective BPM
			double expectedDeltaTime = 21000.0 / effectiveBPM;
			// Density is the relation of this note's expected deltatime to its actual deltatime
			double density = expectedDeltaTime / Math.Max(1.0, noteObject.DeltaTime);

			// Dense notes are penalised and very dense notes are rewarded
			double highDensityPenalty = DifficultyCalculationUtils.Logistic(density, 1.0, 9.0);
			double veryHighDensityBonus = DifficultyCalculationUtils.Logistic(density, 4.0, 4.0);
			
			var midVelocity = new VelocityRange(360, 460);
			var highVelocity = new VelocityRange(380, 560);
			
			double midVelDifficulty = 0.3 * DifficultyCalculationUtils.Logistic(
				effectiveBPM, 
				midVelocity.Center + (150.0 * highDensityPenalty) - (450.0 * veryHighDensityBonus), 
				5.0 / midVelocity.Range
			) + (0.75 * veryHighDensityBonus);
			
			double highVelDifficulty = (1.2 - 0.75 * highDensityPenalty) * DifficultyCalculationUtils.Logistic(
				effectiveBPM,
				highVelocity.Center + (80.0 * highDensityPenalty) - (450.0 * veryHighDensityBonus),
				5.0 / highVelocity.Range
			);
		
			double readingDifficulty = midVelDifficulty + highVelDifficulty;
			// Memory penalty goes here
			
			return readingDifficulty;
        }
    }
}
