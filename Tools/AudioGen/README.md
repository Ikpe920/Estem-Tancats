# Generador de sons i música

Els efectes de so i la música del joc es generen amb aquests scripts: síntesi chiptune amb `numpy`, i `ffmpeg` per passar la música a `.ogg`.

```bash
cd Tools/AudioGen
python3 sfx.py ../../Assets/Resources/Audio/Sfx
python3 music.py ../../Assets/Resources/Audio/Music
```

- `synth.py`: oscil·ladors, envolupants, soroll i filtres.
- `sfx.py`: cada efecte, amb el nom que fa servir `AudioManager.Play("nom")`.
- `music.py`: els temes (`menu`, `act0`–`act4`, `boss`) i les sintonies (`gameover`, `win`). Cada tema té el tempo, la tonalitat, els acords i una llavor (`seed`): canviant la llavor surt una melodia diferent.

Per posar-hi sons o música fets a mà, substituïu el fitxer amb el mateix nom a `Assets/Resources/Audio/`. Per a la música d'un acte, també la podeu assignar al camp *Music* de l'acte a l'`ActManager`: té preferència sobre la generada.
