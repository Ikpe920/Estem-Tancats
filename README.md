# Estem Tancats

Joc d'acció 2D (*roguelite* d'onades) fet amb Unity. Ets el xef d'un restaurant que ja ha tancat, però els clients no marxen. Te'ls has de treure de sobre a cops de menjar, sala per sala, fins a la teulada.

- **25 onades** repartides en **5 actes**: la Sala, la Barra, la Cuina, la Terrassa i la Teulada.
- **5 caps**, un al final de cada acte: el Crític Gastronòmic, el Borratxo de la Barra, el Xef Renegat, el Client VIP i la Inspectora.
- Entre onades tries un **plat** (millora). Si en combines alguns, completes **conjunts** amb efectes especials.
- Les **ratxes** (encadenar clients sense que et toquin) multipliquen els punts. El rècord es guarda.

### Xefs

En prémer **Jugar** tries el xef. El joc recorda el que vas triar per a la propera partida.

| Xef | Punt fort | Punt feble |
|---|---|---|
| El Xef | equilibrat | cap |
| El Carnisser | 4 cors i +1 de dany | més lent i dispara més a poc a poc |
| El Pastisser | dispara molt ràpid i el dash es recarrega abans | només 2 cors |
| El Sushiman | 3 trets en ventall des del principi | dispara més a poc a poc, i els trets dels costats fan menys mal |

### Onades especials

A partir de l'onada 3, un 30% de les onades (mai la d'un cap ni dues seguides) tenen un esdeveniment:

| Esdeveniment | Què passa |
|---|---|
| Hora punta | el doble de clients, que surten el doble de ràpid (×1,5 punts) |
| Tall de llum | se'n va la llum: només veus al teu voltant (no a la Teulada) |
| Client VIP | un client daurat amb corona fuig de tu: si el despatxes abans de 15 s, dona molts punts i monedes |
| Happy hour | els clients deixen premis en morir |
| Racions gegants | la meitat de clients, però enormes i resistents (×3 punts) |

### Clients especials

A partir de certes onades, alguns clients surten amb un comportament propi:

| Client | Des de | Què fa |
|---|---|---|
| El Golafre | onada 4 | Gros i resistent. En morir es divideix en dos de petits i ràpids |
| L'Olla a pressió | onada 6 | Ràpid. A prop teu s'atura, xiula i explota. Si el mates abans, explota igual, però només fa mal als clients |
| El de la Safata | onada 8 | La safata para els trets pel davant i gira a poc a poc. Ataca'l de costat o per darrere, o trenca-la (6 cops) |
| El Cambrer | onada 11 | Es queda lluny i cada 3 s cura els clients del voltant. Com a molt n'hi ha 2 alhora |

## Controls

| Acció | Teclat i ratolí | Comandament |
|---|---|---|
| Moure's | WASD o fletxes | Joystick esquerre |
| Apuntar i disparar | Ratolí + clic esquerre | Joystick dret (o gallet dret) |
| Dash | Espai | RB |
| Bolcar una taula | E | — |
| Triar plat | Clic o 1–5 · H cura · R canvia les opcions | — |
| Pausa | Esc | Start |

### Mode de prova (només a l'Editor i a les *Development Build*)

| Tecla | Què fa |
|---|---|
| F1 | Mostra / amaga el quadre |
| F2 | Salta l'onada |
| F3 | Salta al proper cap |
| F4 | Llista de plats (clica'n un per agafar-lo) |
| F5 | Cura tots els cors |
| F6 | Invencible sí / no |
| F7 | Obre ara la pantalla de triar plat |
| F8 | Fa sortir un client especial de cada tipus |
| F9 | La propera onada tindrà un esdeveniment (cada cop que el prems, en tria un altre) |

## Obrir el projecte

1. Instal·la **Unity 6000.6.0f1** amb Unity Hub (la versió és a `ProjectSettings/ProjectVersion.txt`).
2. A Unity Hub: *Add → Add project from disk* i tria aquesta carpeta.
3. Obre l'escena `Assets/Scenes/MainMenu.unity` i prem Play.

Per fer el build: *File → Build Profiles*. Les escenes ja estan a la llista en aquest ordre: MainMenu, Story, Game, GameOver i Win.

## Estructura

```
Assets/
  Scenes/      MainMenu → Story → Game → GameOver / Win
  Scripts/     tot el codi del joc (una classe per fitxer)
  Prefabs/     clients, caps i projectils
  Resources/   fons dels actes (Actes/), plats (Dishes/), xefs (Chefs/), clients especials (Enemies/)
               i fotogrames de caminar (Walk/)
  Sprites/     la resta de gràfics
```

Scripts principals:

- `WaveSpawner`: les onades, els caps i el final de la partida.
- `ActManager`: el canvi d'acte (fons, dificultat i clients de cada sala).
- `BoonManager`: els plats que tries entre onades, la botiga i la seva interfície.
- `SetBonuses`: els conjunts de plats.
- `PlayerMovement`, `PlayerShooting` i `PlayerHealth`: el xef. `PlayerRef` dona accés ràpid al jugador des de qualsevol script.
- `Enemy`: els clients. `EnemyVariants` en fa els clients especials. Els caps hi afegeixen `BossMelee`, `BossCharger`, `BossSummoner`, `BossTeleporter` o `BossPhases`.
- `ScoreManager` i `RunStats`: els punts, les ratxes i el rècord.
- `WalkAnimator`: l'animació de caminar (alterna el sprite amb els fotogrames de `Resources/Walk/<sprite>_1` i `_2`).
- `ChefRoster` i `ChefSelectScreen`: els xefs que es poden triar i la pantalla per triar-los.
- `PauseMenu`, `TutorialHints`, `OffscreenIndicators`, `DebugMode`, `HitStop` i `CameraFX`: es creen sols o es criden des de qualsevol lloc. No cal posar-los a cap escena.

Els noms de les escenes són a `SceneNames.cs`. Si en reanomenes una, canvia-la allà i a la llista del build.
