# Player

`Assets/Prefabs/Player.prefab` · [← liste des prefabs](README.md)

Le joueur : déplacement case par case au tour par tour, caméra à hauteur d'yeux et lanterne. Posé sur la case `P` du plan au démarrage.

Ce que voit sa caméra : les captures de la scène de test, par exemple [1_depart_est.png](../../Captures/1_depart_est.png).

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
Player  [PlayerController, AudioSource, SoundEmitter]
└─ Caméra  [Camera, UniversalAdditionalCameraData, AudioListener]
   └─ Lanterne  [Light]
```

## Contenu

### Player

- **PlayerController**
  - `maze` = (vide) — Le labyrinthe où l'on se déplace.
  - `moveTime` = 0.22 — Durée d'un pas (secondes).
  - `turnTime` = 0.18 — Durée d'un quart de tour (secondes).
  - `waitTime` = 0.25 — Durée minimale d'un tour passé à attendre, pour ne pas enchaîner des dizaines de tours en gardant la touche enfoncée.
  - `hitStop` = 0.06 — Arrêt sur image quand un coup porte (secondes, temps réel) : le coup « pèse ». 0 = aucun.
  - `turningTakesTurn` = non — Coché : tourner sur place coûte un tour (les ennemis jouent après chaque quart de tour).
  - `inputBuffer` = 0.3 — Tampon d'entrée (secondes) : une touche tapée pendant l'animation d'un pas ou d'un tour est jouée juste après, si elle date de moins que ça. 0 = pas de tampon.
  - `maxHealth` = 10 — Points de vie au départ (et maximum).
  - `attack` = 1 — Dégâts d'une attaque (avancer vers un ennemi droit devant), avant sa défense.
  - `defense` = 0 — Retirée des dégâts reçus (coups, pièges). Un coup fait toujours au moins 1.
  - `move` = `Player/Move` — Se déplacer : ZQSD, stick gauche, croix. Avant / arrière, pas de côté. Avancer vers un ennemi l'attaque, vers un coffre l'ouvre.
  - `turn` = `Player/Turn` — Tourner d'un quart de tour : A / E, LB / RB.
  - `wait` = `Player/Wait` — Attendre un tour : Entrée, L3.
  - `torch` = `Player/Torch` — Allumer / éteindre la lanterne : T, bouton droit (Rond / B). Ne coûte pas de tour.
  - `interact` = `Player/Interact` — Ouvrir le coffre droit devant : F, bouton du bas (Croix / A). Un tour.
  - `stickThreshold` = 0.5 — Seuil au-delà duquel un stick compte comme une direction.
  - `lantern` = `Lanterne` — La lumière que l'action Torch allume et éteint.
  - `cameraRig` = `Caméra` — La caméra du joueur : elle tremble quand on est touché et tombe au sol à la mort. Vide : la caméra enfant.
  - `sounds` = `Player` — Ce qui joue les pas et l'attaque (piste Effets du mixer). Vide : cherché sur le joueur.
  - `stepVolume` = 0.35 — Volume d'un bruit de pas (0 à 1).
  - `attackVolume` = 0.8 — Volume de l'attaque (0 à 1).
  - `hitVolume` = 0.8 — Volume du coup reçu, monstre ou piège (0 à 1).
- **AudioSource**
- **SoundEmitter**
  - `pitchJitter` = 0.08 — Variation de hauteur au hasard, en plus ou en moins (0,08 = ±8 %).

### Player / Caméra

- **Transform**
  - position (0, 1.6, 0)
- **Camera**
  - FOV 75°, plans 0.05–200 m, fond SolidColor (0.1, 0.1, 0.1)
- **UniversalAdditionalCameraData**
- **AudioListener**

### Player / Caméra / Lanterne

- **Transform**
  - position (0, 0.2, -0.4)
- **Light**
  - Point, couleur (1, 1, 1), intensité 0.9, portée 6 m, ombres None
