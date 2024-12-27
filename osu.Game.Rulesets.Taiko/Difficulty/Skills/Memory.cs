// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Difficulty.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the memory coefficient of taiko difficulty.
    /// </summary>
	/// All values are likely subject to change as I don't quite know what I'm doing yet
    public class Memory : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.8;

        private double currentStrain;
		
		private double consecutiveHardToReadBonus; // wtf

        public Memory(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            // Drum Rolls and Swells are exempt.
            if (current.BaseObject is not Hit)
            {
                return 0.0;
            }

            var taikoObject = (TaikoDifficultyHitObject)current;
			var readingDifficulty = ReadingEvaluator.EvaluateDifficultyOf(taikoObject);
			var colourDifficulty = ColourEvaluator.EvaluateDifficultyOf(taikoObject);      
			
			// Notes below this threshold are not worth memorising so give 0 memory difficulty
			// Notes give more memory difficulty up to 1 as they approach maximum reading difficulty
			var hardToReadThreshold = 0.75;
			var memoryDifficulty = DifficultyCalculationUtils.Logistic(readingDifficulty, (hardToReadThreshold + 1.5) / 2, 12);

			currentStrain *= StrainDecayBase;
			currentStrain += memoryDifficulty * (colourDifficulty * 0.3) * consecutiveHardToReadBonus * SkillMultiplier;

			// Small bonus to consecutive hard to read notes
			// Aims to buff memorising longer sections
			if (readingDifficulty >= hardToReadThreshold && consecutiveHardToReadBonus <= 1.25)
			{
				consecutiveHardToReadBonus += 0.01;
			}

            return currentStrain;
        }
    }
}
