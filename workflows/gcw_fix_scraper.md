# GCW Fix Finder Workflow

## Trigger
Manual initiation by the user via a "Find Fix (GCW)" button located in the Mod Dashboard / Game Detail page.

## Input (Checkpoint 1: Search Confirmation)
When triggered, the system presents a **Brief** (a pop-up dialog) showing the sanitized game title (e.g., removing "®", "™", "Game of the Year", etc.). 
- The user can edit the title.
- The user clicks "Search" to proceed.

## Execution (Background Processing)
1. **Search Phase:**
   - The application uses HttpClient with spoofed browser headers (User-Agent, Referer) to query https://gamecopyworld.com/games/gcw_index.shtml?search={GameTitle}.
   - Parses the HTML response (using HtmlAgilityPack or similar regex/DOM parsing) to extract a list of available Fixes/Trainers.
2. **Selection Phase (Checkpoint 2):**
   - Presents the parsed list to the user in the UI.
   - The user selects the desired Fix and clicks "Download".
3. **Mirror Traversal Phase:**
   - The HttpClient traverses the GameCopyWorld mirror system autonomously.
   - It simulates clicking the mirror links, maintaining cookies/referers as needed, to bypass anti-leeching mechanisms.
   - Downloads the target archive file (.rar, .7z, or .zip) to a temporary cache.

## Output & Integration
- The downloaded archive path is passed directly into the existing ModExtractionService.ExtractModAsync(zipFilePath, gameDomain, targetDir).
- The service extracts the fix files into the appropriate game directory just like a standard mod installation.
- Notifies the user of successful application.
