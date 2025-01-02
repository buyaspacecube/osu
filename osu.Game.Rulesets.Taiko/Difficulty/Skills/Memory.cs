// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Mods;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the memory coefficient of taiko difficulty.
    /// </summary>
    public class Memory : StrainDecaySkill
    {
        protected override double SkillMultiplier => 0.8;
        protected override double StrainDecayBase => 0.4;

        private double currentStrain;
		
		private double greatHitWindow; // Used for rhythm difficulty
		
		private bool hasHDFL;

        public Memory(Mod[] mods, double greatHitWindow)
            : base(mods)
        {
			this.greatHitWindow = greatHitWindow;
			hasHDFL = mods.Any(m => m is TaikoModHidden) && mods.Any(m => m is TaikoModFlashlight);
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            // Drum Rolls and Swells are exempt.
            if (current.BaseObject is not Hit)
            {
                return 0.0;
            }

            var taikoObject = (TaikoDifficultyHitObject)current;
			var readingDifficulty = ReadingEvaluator.EvaluateDifficultyOf(taikoObject, hasHDFL);
			var colourDifficulty = ColourEvaluator.EvaluateDifficultyOf(taikoObject);
			var rhythmDifficulty = RhythmEvaluator.EvaluateDifficultyOf(taikoObject, greatHitWindow);
			
			// How necessary a note is to memorise based on its reading difficulty from 0 to 1
			var howNecessaryToMemorise = DifficultyCalculationUtils.Logistic(readingDifficulty, (0.8 + 1.20) / 2, 12);
			
			// How difficult a note is to memorise
			// Colour currently uses a system where only the first note of a non-repeating pattern gives colour difficulty
			// making it line up very well with how hard a note is to memorise
			// Rhythm is also here but weighted much less since rhythms are easier to remember
			var memoryDifficulty = colourDifficulty + (0.1 * rhythmDifficulty);

			currentStrain *= StrainDecayBase;
			currentStrain += howNecessaryToMemorise * memoryDifficulty * SkillMultiplier;

            return currentStrain;
        }
    }
}
