// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Mods;

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

        /// <summary>
        /// Calculates the influence of slider velocities on hitobject difficulty.
        /// The bonus is determined based on effective BPM and "object density".
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <param name="mods">The mods applied to the hit object.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject, Mod[] mods)
        {
            // With HDFL, all note objects are invisible and give the maximum reading difficulty
            if (mods.Any(m => m is TaikoModHidden) && mods.Any(m => m is TaikoModFlashlight)) return 1.0;

            // Apply a cap to prevent outlier values on maps that exceed the editor's parameters
            double effectiveBPM = Math.Max(1.0, noteObject.EffectiveBPM);

            // Expected DeltaTime is the DeltaTime this note would need to be spaced equally to a base slider velocity 1/4 note
            double expectedDeltaTime = 21000.0 / effectiveBPM;
            double objectDensity = expectedDeltaTime / Math.Max(1.0, noteObject.DeltaTime);

            double velocityDifficulty = 0.0;

            // Notes at higher velocities are visible for less time making them harder to read
            // High velocity notes are generally even harder to read with lower object density (think HR streams vs DT)
            // To reflect this, the high velocity range is shifted based on object density
            double lowDensityBonus = 1.0 - DifficultyCalculationUtils.Logistic(objectDensity, 0.68, 20);

            var highVelocity = new VelocityRange(
                420 - (100 * lowDensityBonus), 
                1000 - (250 * lowDensityBonus)
            );

            // Reading mods also affect how long notes are visible for
            double timeVisibleBonus = 1.0;

            if (mods.Any(m => m is TaikoModFlashlight)) timeVisibleBonus *= 3.33;

            if (mods.Any(m => m is TaikoModHidden))
            {
                // With classic and without hardrock, the playfield is limited to 4:3 making notes visible for less time than the expected 16:9
                if (mods.Any(m => m is TaikoModClassic) && !mods.Any(m => m is TaikoModHardRock)) timeVisibleBonus *= (16 / 9.0) / (4 / 3.0);

                // Notes disappearing with hidden makes them visible for less time, with how soon they disappear varying with other mods
                // Despite notes being visible for much less time, the perceived effective BPM increase is much less because of the time between disappearing and being hit
                // Because of this, arbitrary values are used for each mod combo
                if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModEasy)) timeVisibleBonus *= 1.05;
                else if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModHardRock)) timeVisibleBonus *= 1.3; // Explain why this later
                else timeVisibleBonus *= 1.2;
            }

            velocityDifficulty += DifficultyCalculationUtils.Logistic(effectiveBPM * timeVisibleBonus, highVelocity.Center, 10.0 / highVelocity.Range);

            // With hidden, notes at lower velocities are invisible for more time making them harder to remember
            if (mods.Any(m => m is TaikoModHidden))
            {
                var lowVelocity = new VelocityRange(100, 350);
				
                // Reading mods also affect how long notes are invisible for
                double timeInvisibleBonus = 1.0;

                // For the same reason as above, these values are arbitrary
                if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModEasy)) timeInvisibleBonus *= 0.75;
                else if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModHardRock)) timeInvisibleBonus *= 1.1;

                velocityDifficulty += 1.0 - DifficultyCalculationUtils.Logistic(effectiveBPM * timeInvisibleBonus, lowVelocity.Center, 10.0 / lowVelocity.Range);
            }

            // Notes at very high object densities are harder to read regardless of velocity, scaling much faster with hidden
            double densityDifficulty = (mods.Any(m => m is TaikoModHidden)) ? DifficultyCalculationUtils.Logistic(objectDensity, 3.0, 2.5) 
                : Math.Pow(DifficultyCalculationUtils.Logistic(objectDensity, 3.5, 1.5), 3.0);

            return densityDifficulty + velocityDifficulty * (1.0 - densityDifficulty);
        }
    }
}
