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
        /// Calculates the influence of slider velocities on hitobject difficulty.
		/// The bonus is determined based on the EffectiveBPM and "note density".
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
		/// <param name="hasHidden">Whether or not the Hidden mod is enabled.</param>
		/// <param name="hasFlashlight">Whether or not the Flashlight mod is enabled.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject, bool hasHidden, bool hasFlashlight)
        {
			// Until memory considerations are added, all notes give 1.5 reading difficulty with HDFL
			if (hasHidden && hasFlashlight)
			{
				return 1.5;
			}
			
            double effectiveBPM = Math.Max(1.0, noteObject.EffectiveBPM);
			
			// Expected deltatime is the deltatime this note would need
			// to be spaced equally to a base SV 1/4 note at this effective BPM
			double expectedDeltaTime = 21000.0 / effectiveBPM;
			// Density is the relation of this note's expected deltatime to its actual deltatime
			double objectDensity = expectedDeltaTime / Math.Max(1.0, noteObject.DeltaTime);

			var lowVelocity = new VelocityRange(10, 150);
			var highVelocity = new VelocityRange(240, 550);

			// All curves can be found here https://www.desmos.com/calculator/bjf4rpn0bq
			// Stay tuned for comments actually explaining all this
			double readingDifficulty;
			
			if (hasHidden)
			{
				double hdHighDensityBonus = DifficultyCalculationUtils.Logistic(objectDensity, 3.0, 2.5);
				
				double hdLowVelocityDifficulty = 1.0 - DifficultyCalculationUtils.Logistic(effectiveBPM, lowVelocity.Center * (1.0 + 2.0 * hdHighDensityBonus), 10.0 / lowVelocity.Range);
				double hdHighVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(1.2 * effectiveBPM, highVelocity.Center, 5.0 * (1.0 + hdHighDensityBonus) / highVelocity.Range), 3.0);
				
				readingDifficulty = (1.0 - hdHighDensityBonus) * (hdLowVelocityDifficulty + hdHighVelocityDifficulty) + hdHighDensityBonus;
			}
			
			else if (hasFlashlight)
			{
				double flVeryHighDensityBonus = Math.Pow(DifficultyCalculationUtils.Logistic(objectDensity, 3.5, 1.5), 3.0);
				
				double flHighVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(2.0 * effectiveBPM, highVelocity.Center / (1.0 + flVeryHighDensityBonus), 5.0 / highVelocity.Range), 2.5);
				
				readingDifficulty = (1.0 - flVeryHighDensityBonus) * flHighVelocityDifficulty + flVeryHighDensityBonus;
			}
			
			else
			{
				double highDensityPenalty = DifficultyCalculationUtils.Logistic(objectDensity, 1.0, 9.0);
				double veryHighDensityBonus = Math.Pow(DifficultyCalculationUtils.Logistic(objectDensity, 3.5, 1.5), 3.0);
				
				double highVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(effectiveBPM, (highVelocity.Center / (1.0 + 3.0 * veryHighDensityBonus)) + (120.0 * highDensityPenalty), 5.0 / highVelocity.Range), (2.5 - highDensityPenalty));

				readingDifficulty = (1.0 - veryHighDensityBonus) * highVelocityDifficulty + veryHighDensityBonus;
			}
			
			System.Console.WriteLine(readingDifficulty);
			
			return readingDifficulty * 1.5;
        }
    }
}
