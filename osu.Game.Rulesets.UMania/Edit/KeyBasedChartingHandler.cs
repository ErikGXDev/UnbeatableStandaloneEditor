using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UMania.Edit.Blueprints;
using osu.Game.Rulesets.UMania.Objects;
using osu.Game.Screens.Edit;
using osuTK.Input;

namespace osu.Game.Rulesets.UMania.Edit;

// Contains most of the logic for key-charting
public partial class KeyBasedChartingHandler : Drawable
{
    [Resolved]
    private EditorBeatmap editorBeatmap { get; set; } = null!;

    [Resolved]
    private EditorClock editorClock { get; set; } = null!;

    [Resolved]
    private IBeatSnapProvider beatSnapProvider { get; set; } = null!;

    [Resolved]
    private UnbeatableHitObjectComposer composer { get; set; } = null!;
    
    public bool Enabled { get; set; }

    private readonly HitObject?[] heldNotes = new HitObject?[6];
    private readonly bool[] noteWasRemoved = new bool[6];

    private double scrollAccumulation;

    private static readonly Key[] columnKeys =
    {
        Key.Number1,
        Key.Number2,
        Key.Number3,
        Key.Number4,
        Key.Number5,
        Key.Number6,
    };

    // In 4-key mode the visual column order changes to 0,2,3,1,4,5.
    private int[] columnMapping => composer.Is4Key
        ? new[] { 0, 2, 3, 1, 4, 5 }
        : new[] { 0, 1, 2, 3, 4, 5 };

    public bool TryPlaceNote(Key key, bool shiftPressed)
    {
        if (!Enabled)
            return false;

        int keyIndex = Array.IndexOf(columnKeys, key);
        if (keyIndex < 0)
            return false;

        int column = columnMapping[keyIndex];

        // If we're already tracking this key, don't toggle again.
        if (heldNotes[column] != null || noteWasRemoved[column])
            return true;

        placeOrRemoveNote(column, shiftPressed);
        return true;
    }

    public bool TryReleaseKey(Key key)
    {
        int keyIndex = Array.IndexOf(columnKeys, key);
        if (keyIndex < 0)
            return false;

        int column = columnMapping[keyIndex];

        heldNotes[column] = null;
        noteWasRemoved[column] = false;
        return true;
    }
    
    public bool TryAdjustHold(double scrollDelta, bool shiftPressed)
    {
        if (!Enabled)
            return false;

        // Check if any column key is held.
        for (int column = 0; column < 6; column++)
        {
            if (heldNotes[column] == null || noteWasRemoved[column])
                continue;

            scrollAccumulation += scrollDelta;
            const double precision = 1;

            while (Math.Abs(scrollAccumulation) >= precision)
            {
                int direction = scrollAccumulation > 0 ? -1 : 1; // Scroll down extends, up shrinks
                
                if (direction < 0 && !canShrink(column))
                {
                    // Can't shrink further, reset accumulation and consume the scroll
                    scrollAccumulation = 0;
                    return true;
                }

                adjustHoldLength(column, direction, shiftPressed);
                scrollAccumulation = scrollAccumulation < 0
                    ? Math.Min(0, scrollAccumulation + precision)
                    : Math.Max(0, scrollAccumulation - precision);
            }

            // If we accumulated scroll-up but it's below the precision threshold,
            // and we know we can't shrink, consume it to prevent timeline seeking.
            // Needed because scrolling quick can de-sync from the timeline.
            if (scrollAccumulation > 0 && !canShrink(column))
            {
                scrollAccumulation = 0;
                return true;
            }
        }

        if (!heldNotes.Any(n => n != null))
            scrollAccumulation = 0;

        return false;
    }

    private bool canShrink(int column)
    {
        var note = heldNotes[column];
        if (note is HoldNote holdNote)
        {
            double beatLength = beatSnapProvider.GetBeatLengthAtTime(holdNote.StartTime);
            // Convert hold back to single note if needed
            return holdNote.Duration - beatLength >= 0;
        }
        
        return false;
    }

    private void placeOrRemoveNote(int column, bool shiftPressed)
    {
        double snappedTime = beatSnapProvider.SnapTime(editorClock.CurrentTime, null);
        
        var existing = editorBeatmap.HitObjects.OfType<ManiaHitObject>()
            .FirstOrDefault(h => h.Column == column && Math.Abs(h.StartTime - snappedTime) < 1.0);

        if (existing != null)
        {
            editorBeatmap.Remove(existing);
            heldNotes[column] = null;
            noteWasRemoved[column] = true;
            return;
        }

        var note = createNoteForColumn(column, snappedTime, shiftPressed);
        editorBeatmap.Add(note);
        heldNotes[column] = note;
        noteWasRemoved[column] = false;
    }

    private HitObject createNoteForColumn(int column, double startTime, bool shiftPressed)
    {
        var note = new Note
        {
            StartTime = startTime,
            Column = column,
            Samples = new List<HitSampleInfo>
            {
                new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_NORMAL, string.Empty, 100, false),
            },
        };

        var baseSamples = new List<string>();

        if (column >= 0 && column <= 3 && shiftPressed)
            baseSamples.Add(HitSampleInfo.HIT_WHISTLE); // Dodge

        if (column == 4 && shiftPressed)
            baseSamples.Add(HitSampleInfo.HIT_WHISTLE); // Zoom

        var helper = new UbNoteBuilderHelper(composer, note);
        helper.ApplySamples(baseSamples);
        helper.ApplyMainBank(HitSampleInfo.BANK_NORMAL);
        helper.ApplyModifierLayer();

        return note;
    }

    private void adjustHoldLength(int column, int direction, bool shiftPressed)
    {
        var note = heldNotes[column];
        if (note == null)
            return;

        double beatLength = beatSnapProvider.GetBeatLengthAtTime(note.StartTime);

        if (direction > 0) // Extend
        {
            if (note is Note regularNote)
            {
                replaceNoteWithHold(column, regularNote, beatLength, shiftPressed);
            }
            else if (note is HoldNote holdNote)
            {
                double proposedEnd = holdNote.StartTime + holdNote.Duration + beatLength;
                // removed for now, disabling it might be interesting for 4-key gameplay
                // double absorbedEnd = absorbOverlappingHoldOnExtend(column, proposedEnd);
                // replaceHold(column, holdNote, absorbedEnd - holdNote.StartTime, shiftPressed);
                replaceHold(column, holdNote, proposedEnd - holdNote.StartTime, shiftPressed);
            }
        }
        else // Shrink
        {
            if (note is HoldNote holdNote)
            {
                double newDuration = Math.Max(0, holdNote.Duration - beatLength);

                if (newDuration <= 0)
                {
                    // Convert back to Note.
                    var regularNote = new Note
                    {
                        StartTime = holdNote.StartTime,
                        Column = holdNote.Column,
                        Samples = new List<HitSampleInfo>(holdNote.Samples),
                    };

                    editorBeatmap.Remove(holdNote);
                    editorBeatmap.Add(regularNote);
                    heldNotes[column] = regularNote;
                }
                else
                {
                    replaceHold(column, holdNote, newDuration, shiftPressed);
                }
            }
        }
    }

    private void replaceNoteWithHold(int column, Note regularNote, double duration, bool shiftPressed)
    {
        var holdNote = new HoldNote
        {
            StartTime = regularNote.StartTime,
            Column = regularNote.Column,
            Samples = new List<HitSampleInfo>(regularNote.Samples),
            Duration = duration,
        };

        editorBeatmap.Remove(regularNote);
        editorBeatmap.Add(holdNote);
        heldNotes[column] = holdNote;

        applyHoldSpecificSamples(column, shiftPressed, holdNote);
    }

    private void replaceHold(int column, HoldNote oldHold, double newDuration, bool shiftPressed)
    {
        // Remove + Add is used instead of Update because
        // of some visual bug with the head not following the entire length
        // of the hold note. This is not healthy for the undo-queue but oh well.
        var holdNote = new HoldNote
        {
            StartTime = oldHold.StartTime,
            Column = oldHold.Column,
            Samples = new List<HitSampleInfo>(oldHold.Samples),
            Duration = newDuration,
        };

        editorBeatmap.Remove(oldHold);
        editorBeatmap.Add(holdNote);
        heldNotes[column] = holdNote;

        if (newDuration > oldHold.Duration)
            applyHoldSpecificSamples(column, shiftPressed, holdNote);
    }
    
    private void applyHoldSpecificSamples(int column, bool shiftPressed, HoldNote holdNote)
    {
        var helper = new UbNoteBuilderHelper(composer, holdNote);
        if (column >= 0 && column <= 3 && shiftPressed)
            helper.ApplySamples(new List<string> { HitSampleInfo.HIT_WHISTLE }); // Double
        else if (column == 5)
            helper.ApplySamples(new List<string> { HitSampleInfo.HIT_FINISH }); // Spam

        helper.ApplyModifierLayer();
    }

    // Combine holds when they are overlapping.
    private double absorbOverlappingHoldOnExtend(int column, double proposedEndTime)
    {
        var overlappingHold = editorBeatmap.HitObjects
            .OfType<HoldNote>()
            .Where(h => h.Column == column
                        && h != heldNotes[column]
                        && h.StartTime >= ((HoldNote)heldNotes[column]!).StartTime
                        && h.StartTime <= proposedEndTime)
            .OrderBy(h => h.StartTime)
            .FirstOrDefault();

        if (overlappingHold != null)
        {
            double absorbedEnd = overlappingHold.StartTime + overlappingHold.Duration;
            editorBeatmap.Remove(overlappingHold);
            return absorbedEnd;
        }

        return proposedEndTime;
    }
}
