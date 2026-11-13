# Player

`Assets/Prefabs/Player.prefab` · [← liste des prefabs](README.md)

Le joueur : déplacement case par case au tour par tour, caméra à hauteur d'yeux et lanterne. Posé sur la case `P` du plan au démarrage.

Ce que voit sa caméra : les captures de la scène de test, par exemple [1_depart_est.png](../../Captures/1_depart_est.png).

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
Player  [PlayerController]
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
  - `turningTakesTurn` = non — Coché : tourner sur place coûte un tour.
  - `move` = `Player/Move` — Se déplacer : ZQSD, stick gauche, croix. Avant / arrière, pas de côté.
  - `turn` = `Player/Turn` — Tourner d'un quart de tour : A / E, LB / RB.
  - `wait` = `Player/Wait` — Attendre un tour : Entrée, L3.
  - `torch` = `Player/Torch` — Allumer / éteindre la lanterne : T, bouton droit (Rond / B). Ne coûte pas de tour.
  - `stickThreshold` = 0.5 — Seuil au-delà duquel un stick compte comme une direction.
  - `lantern` = `Lanterne` — La lumière que l'action Torch allume et éteint.

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
