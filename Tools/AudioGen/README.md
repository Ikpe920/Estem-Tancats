# Generador de sons i música

Els efectes de so i la música del joc es generen amb aquests scripts: síntesi chiptune amb `numpy`, i `ffmpeg` per passar la música a `.ogg`.

```bash
cd Tools/AudioGen
python3 sfx.py ../../Assets/Resources/Audio/Sfx
python3 swing.py ../../Assets/Resources/Audio/Music
```

- `synth.py`: oscil·ladors, envolupants, soroll i filtres.
- `sfx.py`: cada efecte, amb el nom que fa servir `AudioManager.Play("nom")`.
- `swing.py`: la música del joc, swing dels anys 30 amb so de disc de gramòfon. Hi ha els temes (`menu`, `act0`–`act4`, `boss`) i les sintonies (`gameover`, el trombó trist, i `win`). Cada tema té el tempo, la tonalitat, els acords, els instruments i una llavor (`seed`): canviant la llavor surt una melodia diferent.
- `music.py`: la música chiptune d'abans (ja no es fa servir, però es pot tornar a generar).

Per posar-hi sons o música fets a mà, substituïu el fitxer amb el mateix nom a `Assets/Resources/Audio/`. Per a la música d'un acte, també la podeu assignar al camp *Music* de l'acte a l'`ActManager`: té preferència sobre la generada.
