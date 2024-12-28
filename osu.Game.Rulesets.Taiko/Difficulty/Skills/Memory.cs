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
    public class Memory : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.8;

        private double currentStrain;
		
		private double totalMemoryDiffAdded;

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
			// Notes give more memory difficulty up to 0.5 as they approach maximum reading difficulty
			var hardToReadThreshold = 0.75;
			var memoryDifficulty = 0.5 * DifficultyCalculationUtils.Logistic(readingDifficulty, (hardToReadThreshold + 1.5) / 2, 12);
			
			// Multiplier based on total memory difficulty already added, starts at *0.5 and caps at *5 for now
			// Buffs memorising more stuff
			// https://www.desmos.com/calculator/tcih3q6fk1
			memoryDifficulty *= Math.Min(5, Math.Pow(totalMemoryDiffAdded / 50, 0.75) + 0.5);
			
			totalMemoryDiffAdded += memoryDifficulty;

			currentStrain *= StrainDecayBase;
			currentStrain += memoryDifficulty * (colourDifficulty * 0.5) * SkillMultiplier;
			
            return currentStrain;
        }
    }
}
