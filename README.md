# CLI ripper for AAX (Audible) files.


## Description

Creates a properly tagged .M4B / 
MP3, with (optional) a copy of the JGP cover, an NFO and a CUE file.
Created with the purpose of being a "one-click" ripper.

## Usage
Should be set as an environment variable for ease of use.

```bash
iAmDeaf <full_path_to_aax>
```
Otherwise can be set as the default program to handle .aax files.
It can then be started by simply double clicking the file, or just entering the path to the encrypted book without the "iAmDeaf" prefix.

## Settings
The program's settings can be found in the config.json file.
Options available are:
Title order of: Author ; Title ; Year ; Narrator ; Bitrate ; null (for empty)
Codec: MP3 ; M4B
Split by chapters: True ; False
Mp3 Encoder: Lavf ; LAME
