# CLI ripper for AAX - AAXC (Audible) files.


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
 - Title order of (Author / Title / Year / Narrator / Bitrate / null (for empty))
 - Codec (MP3 / M4B)
 - Split by chapters (true / false)

#AAXC Usage
You must set up your Audible profile. Run "ProfileSetup.cmd" and follow the given instructions.

Personal suggestions for ease of setup:
 - Encrypt auth file: n
 - login with external browser: y
 - login with pre.amazon audible account: n
 - 
## Download + Decryption of Plus Catalogue Titles
```bash
iAmDeaf -c <ASIN>   //Where ASIN is the book ID in your library.
```

##AAXC Settings
 - By default generates an nfo, cue, cover. Can be changed in config.json
 - Split (true / false)
 - Backup (true / false)   //Backups the .voucher and .aaxc to "Audiobooks/bak" in case of future offline decryption. Enabled by default.
 
 The created Audiobooks will be added to the "Audiobooks" on the desktop.
