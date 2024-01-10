Osynchronica 1.0
Copyright (C) 2021-2024 Haruka
Licensed under the GPLv3.

---

A command-line .osu -> fumen converter for a certain arcade game of B***** N****.

Requires a working Osu!, Squirrel, ffmpeg and medianoche installation.
See wiki for more details.

Usage:
OSynchronica.exe [args] <input file> <output folder> <songid>

Arguments:

  -v, --verbose             Set output to verbose messages.
  --external-quiet          Output nothing for program invocations such as ffmpeg.
  --stack-to-hold           Convert stacked notes to a hold note.
  --spinner-to-hold         Convert spinners to a hold note.
  --no-reverse-to-hold      Do not convert small fiddle sliders to a hold note.
  --jacket                  Path to song jacket image.
  --beatmap-jacket          Use the beatmap background as jacket (not recommended).
  --clean                   Clean the output folder before conversion.
  --no-video                Do not load videos.
  --clone-vflip             don't
  --tags                    Comma-seperated list of categories for this song. (Allowed: JPOP, VOCALOID, ANIME, GAME, CLASSIC, ORIGINAL, VARIETY)
  --keep-wav                Keep the .wav file in the sound folder, even while having a nus3bank.
  --help                    Display this help screen.
  --version                 Display version information.
  Input File (pos. 0)       Required. The file to convert
  Output Folder (pos. 1)    Required. The folder to output converted song data to
  Song ID (pos. 2)          Required. The song id to use in Synchronica. Has to be unique. Custom songs start at 600 and go up to 999.

---

Building:

Requires MapsetParser: https://github.com/Naxesss/MapsetParser
Requires Squirrel: https://github.com/akechi-haruka/Squirrel
Requires SynchronicaFumenLibrary: https://github.com/akechi-haruka/SynchronicaFumenLibrary