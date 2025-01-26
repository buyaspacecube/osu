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
		
		// Stay tuned for comments actually explaining all this
		
		public static double EvaluateVelocityDifficultyOf(double effectiveBPM, double objectDensity, bool hasHidden, bool hasFlashlight)
		{
			var lowVelocity = new VelocityRange(10, 150);
			double lowVelocityDifficulty = 0.0;
			
			var highVelocity = new VelocityRange(240, 550);
			double highVelocityDifficulty = 0.0;
			
			if (hasHidden)
			{
				lowVelocityDifficulty = 1.0 - DifficultyCalculationUtils.Logistic(effectiveBPM, lowVelocity.Center, 10.0 / lowVelocity.Range);
				highVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(1.2 * effectiveBPM, highVelocity.Center, 5.0 / highVelocity.Range), 3.0);
			}
			else if (hasFlashlight)
			{
				highVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(2.0 * effectiveBPM, highVelocity.Center, 5.0 / highVelocity.Range), 2.5);
			}
			else
			{
				double highDensityPenalty = DifficultyCalculationUtils.Logistic(objectDensity, 1.0, 9.0);
				
				highVelocityDifficulty = Math.Pow(DifficultyCalculationUtils.Logistic(effectiveBPM, highVelocity.Center + (180.0 * highDensityPenalty), 5.0 / highVelocity.Range), (2.5 - highDensityPenalty));
			}
			
			return lowVelocityDifficulty + highVelocityDifficulty;
		}
		
		public static double EvaluateOtherDifficultyOf(double objectDensity, bool hasHidden)
		{
			if (hasHidden) return DifficultyCalculationUtils.Logistic(objectDensity, 3.0, 2.5);
			
			else return Math.Pow(DifficultyCalculationUtils.Logistic(objectDensity, 3.5, 1.5), 3.0);
		}


        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject, bool isVelocity, bool hasHidden, bool hasFlashlight)
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
			
			double velocityDifficulty = EvaluateVelocityDifficultyOf(effectiveBPM, objectDensity, hasHidden, hasFlashlight);
			double otherDifficulty = EvaluateOtherDifficultyOf(objectDensity, hasHidden);
			
			if (isVelocity)
				return (1.0 - otherDifficulty) * velocityDifficulty;
			else
				return otherDifficulty;
        }
    }
}
