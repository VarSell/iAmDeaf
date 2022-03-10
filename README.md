# CLI ripper for AAX - AAXC (Plus Catalogue) files.
#### Uses [Audible-CLI](https://github.com/mkb79/audible-cli) and [AAXClean](https://github.com/Mbucari/AAXClean) as it's backbone. Many thanks to the creators!

## Description
### [Requirements: Win, x64]
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

# AAXC Usage
You must first set up your Audible profile. Run "ProfileSetup.cmd" and follow the given instructions.

Personal suggestions for ease of setup:
 - Encrypt auth file: n
 - login with external browser: y
 - login with pre.amazon audible account: n
 
## Download + Decryption of Plus Catalogue Titles
```bash
iAmDeaf -c <ASIN>   //Where ASIN is the book ID in your library. Example: iAmDeaf -c B002V5B8P8
```

## AAXC Settings
 - By default generates an nfo, cue, cover. Can be changed in config.json
 - Split (true / false)
 - Backup (true / false)   //Backups the .voucher and .aaxc to "Audiobooks/bak" in case of future offline decryption. Enabled by default.
 
 The created Audiobooks will be added to the "Audiobooks" on the desktop.

## Decryption of offline backups
[There must be only one voucher and aaxc file in the same folder, to prevent mixups]
```bash
iAmDeaf <full_path_to_aaxc>
```
#### The offline decryption only saves an M4B copy of the file in the same directry as the source files. No nfo, cue or image.
