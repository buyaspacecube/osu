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

        // desmos link goes here once i make it presentable

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

            double velocityDifficulty = 0.0;

            // Notes at higher velocities are visible for less time making them harder to read
            // High velocity notes are generally even harder to read with lower object density (think HR streams vs DT)
            // To reflect this, the high velocity range is shifted based on object density
            // yeah worry about comments later lol

            var previousNoteObject = (TaikoDifficultyHitObject)noteObject.Previous(0);

            double lowDensityBonus = DifficultyCalculationUtils.Smoothstep(
                (previousNoteObject != null) ? Math.Max(ObjectDensityOf(previousNoteObject), ObjectDensityOf(noteObject)) : ObjectDensityOf(noteObject),
                0.9, 0.35
            );

            var highVelocity = new VelocityRange(
                420 - (140 * lowDensityBonus), 
                1000 - (320 * lowDensityBonus)
            );

            // Reading mods also affect how long notes are visible for
            double timeVisibleBonus = 1.0;

            if (mods.Any(m => m is TaikoModFlashlight)) timeVisibleBonus *= 3.33;

            if (mods.Any(m => m is TaikoModHidden))
            {
                // With classic and without hardrock, the playfield is limited to 4:3 making notes visible for less time than the expected 16:9
                if (mods.Any(m => m is TaikoModClassic) && !mods.Any(m => m is TaikoModHardRock)) timeVisibleBonus *= 1560 / 1080.0;

                // The time notes take to disappear with hidden varies when combined with other mods
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
                var lowVelocity = new VelocityRange(125, 280);
				
                // Reading mods also affect how long notes are invisible for
                double timeInvisibleBonus = 1.0;

                // a
                if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModEasy)) timeInvisibleBonus *= 4 / 3.0;
                else if (mods.Any(m => m is TaikoModClassic) && mods.Any(m => m is TaikoModHardRock)) timeInvisibleBonus *= 0.9;

                velocityDifficulty += 1.0 - DifficultyCalculationUtils.Logistic(effectiveBPM * timeInvisibleBonus, lowVelocity.Center, 10.0 / lowVelocity.Range);
            }

            // With hidden, all notes award a base reading difficulty
            if (mods.Any(m => m is TaikoModHidden)) velocityDifficulty = 0.25 + 0.75 * velocityDifficulty;

            if (previousNoteObject != null)
            {
                // Notes with harsher velocity changes from the previous note are also harder to read, measured with the change in effective BPM per millisecond
                double acceleration = (effectiveBPM - previousNoteObject.EffectiveBPM) / Math.Max(1.0, noteObject.DeltaTime);

                double accelerationDifficulty = Math.Max(0, DifficultyCalculationUtils.Logistic(acceleration, 0, -3) - 0.5) + Math.Max(0, 2.0 * DifficultyCalculationUtils.Logistic(acceleration, 0, 8) - 1.0);

                // This returns a number that represents both velocity and acceleration difficulty without exceeding 1.0
                return velocityDifficulty + accelerationDifficulty * (1 - velocityDifficulty);
            }

            return velocityDifficulty;
        }

        // Object density is a measure of how close a note object appears to the one before it
        public static double ObjectDensityOf(TaikoDifficultyHitObject noteObject)
        {
            // Expected DeltaTime is the DeltaTime this note would need to be spaced equally to a base slider velocity 1/4 note
            double expectedDeltaTime = 21000.0 / Math.Max(1.0, noteObject.EffectiveBPM);

            return expectedDeltaTime / Math.Max(1.0, noteObject.DeltaTime);
        }
    }
}
