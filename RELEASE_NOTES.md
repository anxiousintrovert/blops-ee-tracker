# EETracker 0.2.1

## Highlights

- Expands objective details and progress trackers to use the available card width.
- Gives the Next Step panel its own scrolling area so long titles and instructions, including the Previous line, remain accessible.
- Presents the quest timeline as readable, horizontally scrollable step cards.
- Shows Moon soul-canister progress with four full-width bars and avoids repeating tracker counts.
- Adds actionable Die Rise Sliquifier, Buddha, and tower checklists, plus distinct Mob of the Dead ending objectives.

## Verification

The Release app build completed with zero warnings and errors. All 18 automated tests passed. UI previews were captured from replay fixtures; they verify the rendered layout, not live-match observer behavior. Die Rise objective wording has not been independently verified in a private match.

# EETracker 0.3.0

## Highlights

- Fixes Call of the Dead loading failures by disabling its incompatible optional inventory watcher. Quest, dial, beacon, and session tracking remain enabled.
- Expands the Call of the Dead guide with the solo door interaction after the generators and the correct four-radio sequence for Ensemble Cast.
- Adds Call of the Dead radio progress and ship wheel/telegraph alignment indicators. These new observer signals are source-reviewed and still need a fresh private-match check.
- Adds live door status to the mission guide and map-specific tile progress panels for Shangri-La and Die Rise.
- Adds BO1 points testing support and improves BO2 quest-part/profile tracking.

## Verification

- Release build and Windows x64 self-contained publish completed with zero warnings and errors.
- The Call of the Dead inventory watcher removal previously allowed the user to load the map; this release's added radio/control observer logic has not yet been checked in a fresh match.
- No full private-match verification is claimed for the new Shangri-La, Die Rise, or BO2 observer/UI paths.
