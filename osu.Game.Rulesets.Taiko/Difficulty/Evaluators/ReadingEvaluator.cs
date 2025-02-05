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
		
		// All curves can be found here https://www.desmos.com/calculator/jlit0ppur6
		
		public static double EvaluateVelocityDifficultyOf(double effectiveBPM, double objectDensity, bool hasHidden, bool hasFlashlight)
		{
			double velocityDifficulty = 0.0;
			
			// With hidden, notes at low velocities are hard to read
			var lowVelocity = new VelocityRange(45, 210);
			velocityDifficulty += (hasHidden) ? 1.0 - DifficultyCalculationUtils.Logistic(effectiveBPM, lowVelocity.Center, 10.0 / lowVelocity.Range) : 0.0;
			
			// Without hidden, notes at high velocities are generally easier to read with higher object density than lower
			// To reflect this, the high velocity range is shifted based on object density
			double highDensityPenalty = (hasHidden) ? 0.0 : DifficultyCalculationUtils.Logistic(objectDensity, 1.0, 9.0);
			
			var highVelocity = new VelocityRange(
				250 + (150 * highDensityPenalty),
				700 + (100 * highDensityPenalty)
			);
			
			// Effective BPM is multiplied with hidden and flashlight to reflect notes being visible for less time
			if (hasHidden) effectiveBPM *= 1.2;
			if (hasFlashlight) effectiveBPM *= 2.0;
			
			velocityDifficulty += DifficultyCalculationUtils.Logistic(effectiveBPM, highVelocity.Center, 10.0 / highVelocity.Range);
			
			return velocityDifficulty;
		}
		
		public static double EvaluateDensityDifficultyOf(double objectDensity, bool hasHidden)
		{
			// With hidden, density makes notes much harder to read very quickly
			if (hasHidden) return DifficultyCalculationUtils.Logistic(objectDensity, 3.0, 2.5);
			
			// Without hidden uhh this is pretty arbitrary change this up sometime
			else return Math.Pow(DifficultyCalculationUtils.Logistic(objectDensity, 3.5, 1.5), 3.0);
		}


        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject, bool isVelocity, bool hasHidden, bool hasFlashlight)
        {
			// Until memory considerations are added, all notes give 1.0 reading difficulty with HDFL
			// This is all added as other difficulty and none as velocity
			if (hasHidden && hasFlashlight)
			{
				return isVelocity ? 0.0 : 1.0;
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
			double otherDifficulty = EvaluateDensityDifficultyOf(objectDensity, hasHidden);
			
			if (isVelocity)
				return (1.0 - otherDifficulty) * velocityDifficulty;
			else
				return otherDifficulty;
        }
    }
}
