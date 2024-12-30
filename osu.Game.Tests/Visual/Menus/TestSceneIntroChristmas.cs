// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Menu;
<<<<<<< HEAD
=======
using osu.Game.Seasonal;
>>>>>>> 7746867feb097672bc817ff02c74ffe6787a0d36

namespace osu.Game.Tests.Visual.Menus
{
    [TestFixture]
    public partial class TestSceneIntroChristmas : IntroTestScene
    {
        protected override bool IntroReliesOnTrack => true;
        protected override IntroScreen CreateScreen() => new IntroChristmas();
    }
}
