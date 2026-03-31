// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Beatmaps.Legacy
{
    // FIX: Make public
    [Flags]
    public enum LegacyEffectFlags
    {
        None = 0,
        Kiai = 1,
        OmitFirstBarLine = 8
    }
}
