// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the memory coefficient of taiko difficulty.
    /// </summary>
	/// All values are likely subject to change as I don't quite know what I'm doing yet
    public class Memory : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.9; // Very slow decay as generally memorising more notes is harder

        private double currentStrain;

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

            currentStrain *= StrainDecayBase;
			
			// Only notes considered hard to read are viable for memorisation
			if (readingDifficulty > 1.3)
			{
				// This will probably need a MemoryEvaluator in the future but I'll see how this looks for now
				var colourDifficulty = ColourEvaluator.EvaluateDifficultyOf(taikoObject);
				currentStrain += (colourDifficulty * 0.2) * SkillMultiplier;
			}

            return currentStrain;
        }
    }
}
