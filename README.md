# Estem Tancats

Joc d'acció 2D (*roguelite* d'onades) fet amb Unity. Ets el xef d'un restaurant que ja ha tancat, però els clients no marxen. Te'ls has de treure de sobre a cops de menjar, sala per sala, fins a la teulada.

- **40 onades** repartides en **8 sales**: la Sala, la Barra, la Cuina, la Terrassa, la Teulada, el Celler, la Cambra frigorífica i el Plató.
- **Un cap cada 5 onades**: el Crític (5), el Borratxo (10), el Xef renegat (15), el Client VIP (20), la Inspectora (25), el Sommelier (30), el Gelater boig (35) i, al final, la Crítica de la tele (40).
- **5 caps**, un al final de cada acte: el Crític Gastronòmic, el Borratxo de la Barra, el Xef Renegat, el Client VIP i la Inspectora.
- Entre onades tries un **plat** (millora). Si en combines alguns, completes **conjunts** amb efectes especials.
- Les **ratxes** (encadenar clients sense que et toquin) multipliquen els punts. El rècord es guarda.

### Propines, dificultat i reptes

- En acabar cada partida guanyes **propines**: punts / 25 + onada × 4, multiplicat segons la dificultat.
- Amb les propines **desbloqueges xefs**. El Xef està obert des del principi; la resta costen: Carnisser 200, Pastisser 350, Sushiman 550 i Pizzer 800.
- La **dificultat** es tria a la pantalla de triar xef:
  - Fàcil: +1 cor i clients més fluixos (×0,75 propines).
  - Normal: com sempre.
  - Difícil: més clients, més forts i més ràpids (×1,5 propines).
- Hi ha **10 reptes** que donen propines la primera vegada que els completes (botó *Reptes* o tecla R a la pantalla de triar xef).

### Xefs

En prémer **Jugar** tries el xef. El joc recorda el que vas triar per a la propera partida.

| Xef | Punt fort | Punt feble |
|---|---|---|
| El Xef | equilibrat | cap |
| El Carnisser | 4 cors i +1 de dany | més lent i dispara més a poc a poc |
| El Pastisser | dispara molt ràpid i el dash es recarrega abans | només 2 cors |
| El Sushiman | 3 trets en ventall des del principi | dispara més a poc a poc, i els trets dels costats fan menys mal |
| El Pizzer | pizzes que travessen tots els clients i tornen a la mà (+1 de dany) | llança més a poc a poc |

### Experiència

Com a Vampire Survivors: cada client despatxat deixa **monedes de propina** a terra (els grossos i els especials, més; els caps, un munt). Passa-hi a prop i l'imant les atrau. Quan s'omple la barra de baix, **puges de nivell** i el joc s'atura per triar un plat. En acabar cada onada, les monedes que queden venen soles. Entre onades ja no hi ha plats, però **cada cap deixa un cofre** amb dos plats de regal.

### Plats: especialitats, complements i plats estrella

Com a Vampire Survivors o Brotato, no pots tenir-ho tot:
- **Especialitats** (com les armes): plats que canvien com es juga (ventall, explosions, olives, sopa, fuet, truites...). En caben **6**.
- **Complements** (com els objectes passius): millores simples (mal, velocitat, crítics, àrea...). En caben **6**.
- Tornar a triar un plat el **puja de nivell**. Amb una taula plena només surten millores dels que tens. Els de curar i de punts no ocupen espai.
- **Plats estrella**: una especialitat al nivell màxim + el seu complement fan sortir una carta daurada que la transforma (per exemple, Paella mixta + Cafè sol → Paella gegant). N'hi ha 19 (cada especialitat en té un, i cada complement en fa evolucionar algun); al Receptari es veu la recepta de cada un.
- Dos plats de tipus que combinen completen un **conjunt**.

### El Rebost

Al menú principal, el **Rebost** ven millores permanents amb propines: +1 cor, més crítics, llançar i caminar més ràpid, dash, invulnerabilitat, més propines, més canvis de cartes, una carta més, començar amb un complement, i +1 espai a cada taula.

### Mode infinit: hores extres

En acabar l'onada 40 pots acabar la partida o fer **hores extres** (mode infinit):
- cada onada és una hora (la 41 és la una de la matinada) i passen la nit i el dia: la llum canvia (nit blava, alba rosa, dia, posta taronja) i al HUD hi ha el rellotge;
- de nit sonen les versions fosques de les músiques de cada sala, i de dia les normals;
- cada 5 onades canvies a una sala a l'atzar (mai la mateixa), amb el cartell "HORES EXTRES · 06:00" i un cartell de pel·lícula muda segons l'hora;
- la dificultat no torna a baixar: parteix de la de la sala més difícil i puja una mica a cada sala nova;
- els caps tornen **enfurismats** (més vida, més ràpids i vermells).

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

Com a molt un 35% dels clients d'una onada són especials (es reparteixen entre els que toquen).

A partir de certes onades, alguns clients surten amb un comportament propi:

| Client | Des de | Què fa |
|---|---|---|
| El Golafre | onada 4 | Gros i resistent. En morir es divideix en dos de petits i ràpids |
| L'Olla a pressió | onada 6 | Ràpid. A prop teu s'atura, xiula i explota. Si el mates abans, explota igual, però només fa mal als clients |
| El de la Safata | onada 8 | La safata para els trets pel davant i gira a poc a poc. Ataca'l de costat o per darrere, o trenca-la (6 cops) |
| El Cambrer | onada 11 | Es queda lluny i cada 3 s cura els clients del voltant. Com a molt n'hi ha 2 alhora |
| El Nen | onada 3 | Petit i molt ràpid, fa ziga-zagues |
| La Iaia | onada 5 | Lenta però amb molta vida; et llança el bolso, que et frena 2 s |
| El Turista | onada 7 | S'atura i fa una foto: el flaix t'enlluerna si és a prop (el dash ho esquiva) |
| El Repartidor | onada 9 | Apunta i surt disparat en línia recta |
| L'Influencer | onada 12 | Es queda lluny i crida seguidors (fins a 6). Com a molt n'hi ha 2 alhora |
| El Mim | onada 14 | De tant en tant es fa gairebé invisible |

## Controls

| Acció | Teclat i ratolí | Comandament (Xbox · PlayStation) |
|---|---|---|
| Moure's | WASD o fletxes | Joystick esquerre |
| Apuntar i disparar | Ratolí + clic esquerre | Joystick dret (o RT · R2), amb una mica d'ajuda a apuntar |
| Dash | Espai | RB · R1 |
| Bolcar una taula | E | X · Quadrat |
| Triar plat | Clic o 1–5 · H cura · R canvia les opcions | Creu o joystick per triar · A · Creu per agafar · X · Quadrat cura · Y · Triangle canvia |
| Triar xef | Fletxes o 1–4 · Tab dificultat · R reptes · Enter | Creu · LB/RB dificultat · Y reptes · A |
| Pausa | Esc | Start · Options (B · Cercle per tornar) |
| Menús | Ratolí | Creu o joystick i A · Creu |

Amb comandament:
- els textos d'ajuda diuen els botons del comandament que tens connectat (Xbox, PlayStation o Switch);
- el cursor del ratolí s'amaga;
- davant del xef hi ha una mira que indica cap on apuntes;
- el comandament vibra (es pot treure al menú de pausa).

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
| F10 | +500 propines |

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
               fotogrames de caminar (Walk/) i so i música (Audio/)
  Sprites/     la resta de gràfics
```

Scripts principals:

- `WaveSpawner`: les onades, els caps i el final de la partida.
- `ActManager`: el canvi d'acte (fons, dificultat i clients de cada sala).
- `BoonManager`: els plats (tries un plat cada cop que puges de nivell; els caps deixen un cofre amb dos plats), la botiga i la seva interfície. `XpCoin`: les monedes d'experiència que deixen els clients.
- `SetBonuses`: els conjunts de plats.
- `PlayerMovement`, `PlayerShooting` i `PlayerHealth`: el xef. `PlayerRef` dona accés ràpid al jugador des de qualsevol script.
- `Enemy`: els clients. `EnemyVariants` en fa els clients especials. Els caps hi afegeixen `BossMelee`, `BossCharger`, `BossSummoner`, `BossTeleporter` o `BossPhases`.
- `ScoreManager` i `RunStats`: els punts, les ratxes i el rècord.
- `AudioManager`: els efectes de so (`AudioManager.Play("nom")`) i la música de cada escena i cada acte. Els sons es generen amb `Tools/AudioGen`.
- Personatges: tots els xefs, clients i caps es dibuixen amb `Tools/SpriteGen/characters.py` (i els projectils i els cors amb `items.py`) (estil dibuixos animats dels anys 30, 64x64). Per canviar-ne un, toca el seu diccionari a `CHARACTERS` i torna a executar l'script (necessita Pillow).
- `Iris`: la transició del cercle negre entre escenes (i al voltant del xef quan mor). Per canviar d'escena, fes servir `Iris.LoadScene(...)` en lloc de `SceneManager.LoadScene`.
- `CartoonAnim` (`Squash` i `CartoonDeath`): les animacions de dibuix animat. `Squash.Kick(objecte, x, y)` aixafa i estira amb una molla (només visualment, no toca la física); els clients surten volant girant en morir.
- `Intertitle`: els cartells de pel·lícula muda (en començar, en canviar de sala, al torn de nit, en morir i en guanyar). Els textos són a `Intertitle.ActIntro`, `Opening`, `GameOverText` i `WinText`.
- `OptionsScreen` i `GameSettings`: la pantalla d'Opcions del menú principal (volum general, música i efectes per separat, filtre, vibració, pantalla completa, dificultat i tutorial) i les pàgines de Controls i Crèdits.
- `FilmFilter`: el filtre de pel·lícula antiga (sepia a la càmera; gra, ratlles, pols i parpelleig per sobre de tot). S'encén i s'apaga al menú de pausa.
- `StoryBackdrop`: el fons animat de la pantalla de la història (el restaurant de nit, el cartell "TANCAT", els llums i els clients que envolten el xef).
- `WalkAnimator`: l'animació de caminar (alterna el sprite amb els fotogrames de `Resources/Walk/<sprite>_1` i `_2`).
- `GamepadInput`: detecta si es juga amb comandament, els noms dels botons, la vibració i la selecció de botons als menús.
- `ChefRoster` i `ChefSelectScreen`: els xefs que es poden triar i la pantalla per triar-los.
- `PauseMenu`, `TutorialHints`, `OffscreenIndicators`, `DebugMode`, `HitStop` i `CameraFX`: es creen sols o es criden des de qualsevol lloc. No cal posar-los a cap escena.

Els noms de les escenes són a `SceneNames.cs`. Si en reanomenes una, canvia-la allà i a la llista del build.
