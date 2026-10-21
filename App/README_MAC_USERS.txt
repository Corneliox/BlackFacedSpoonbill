================================================================================
  HOW TO OPEN SPOONBILL AI STUDIO ON MACOS (APPLE SILICON M1/M2/M3/M4 & INTEL)
  Bypass macOS Gatekeeper "Unidentified Developer" Security Prompt
================================================================================

Thank you for using Black-Faced Spoonbill AI Studio v4.1!
Because this software is built as an open-source academic tool without a paid
annual Apple Developer certificate, macOS Gatekeeper may show a security notice:
"Spoonbill_AI_Studio cannot be opened because Apple cannot check it for malicious software"
or "Unidentified Developer".

Follow these 4 simple steps to open the application safely:

--------------------------------------------------------------------------------
STEP 1: COPY APPLICATION TO /Applications
--------------------------------------------------------------------------------
1. Drag and drop "Spoonbill_AI_Studio.app" into the "Applications" folder shortcut
   located inside this DMG window.
2. Wait a few seconds until copying is complete.

--------------------------------------------------------------------------------
STEP 2: RUN THE UNLOCK SCRIPT
--------------------------------------------------------------------------------
Inside this DMG window, there is a helper script:
   "Unlock_and_Open_Mac.command"

DO NOT simply double-click if blocked. Instead:
1. RIGHT-CLICK (or hold CONTROL on your keyboard and CLICK) on:
   "Unlock_and_Open_Mac.command"
2. Select "Open" from the context menu.
3. macOS will show a confirmation dialog:
   "Unlock_and_Open_Mac.command is from an unidentified developer. Are you sure you want to open it?"
4. Click the "Open" button to confirm it is safe to proceed.

--------------------------------------------------------------------------------
STEP 3: WAIT FOR QUARANTINE CLEANUP
--------------------------------------------------------------------------------
A Terminal window will open and perform 3 quick actions:
   [1/3] Verifying / copying Spoonbill_AI_Studio.app to /Applications
   [2/3] Removing Gatekeeper quarantine flag (com.apple.quarantine)
   [3/3] Launching Spoonbill AI Studio

Wait 2-3 seconds until you see:
"Done! Spoonbill AI Studio is now unlocked and trusted on this Mac."
The Terminal window will then exit automatically.

--------------------------------------------------------------------------------
STEP 4: LAUNCH SPOONBILL STUDIO
--------------------------------------------------------------------------------
Spoonbill AI Studio will open up immediately on your desktop!

For future runs:
You can simply launch "Spoonbill_AI_Studio" directly from Finder > Applications
or via Launchpad/Spotlight just like any native Mac application, without needing
to re-run the script.

================================================================================
ALTERNATIVE MANUAL METHOD (WITHOUT RUNNING SCRIPT):
================================================================================
1. Open Finder > Navigate to "Applications".
2. Locate "Spoonbill_AI_Studio.app".
3. RIGHT-CLICK (Control + Click) on "Spoonbill_AI_Studio.app" > Select "Open".
4. On the macOS prompt, click "Open".
5. Or open Apple menu > System Settings > Privacy & Security > scroll down to
   the "Security" section > click "Open Anyway".

================================================================================
Spoonbill AI Studio is now ready to use!
================================================================================
