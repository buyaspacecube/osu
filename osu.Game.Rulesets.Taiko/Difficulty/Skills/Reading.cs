// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the reading coefficient of taiko difficulty.
    /// </summary>
    public class Reading : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.4;

        private double currentStrain;
		
        private float CS;
        private float AR;

        public Reading(Mod[] mods, float circleSize, float approachRate)
            : base(mods)
        {
            CS = circleSize;
            AR = approachRate;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            // Drum Rolls and Swells are exempt.
            if (current.BaseObject is not Hit)
            {
                return 0.0;
            }

            var taikoObject = (TaikoDifficultyHitObject)current;
            int index = taikoObject.ColourData.MonoStreak?.HitObjects.IndexOf(taikoObject) ?? 0;

            currentStrain *= DifficultyCalculationUtils.Logistic(index, 4, -1 / 25.0, 0.5) + 0.5;
            currentStrain *= StrainDecayBase;

            double readingDifficulty = ReadingEvaluator.EvaluateDifficultyOf(taikoObject);

            // Nerf to big notes for being easier to reading
            readingDifficulty *= Math.Min(1.0, CS / 5.0);

            // Bonus to high approach rate
            readingDifficulty *= Math.Max(1.0, (AR / 5.0) - 0.5);

            currentStrain += readingDifficulty * SkillMultiplier;

            return currentStrain;
        }
    }
}
