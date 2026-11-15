# Hud

`Assets/Prefabs/UI/Hud.prefab` · [← liste des prefabs](README.md)

Interface en jeu, dessinée en gravure (InkCanvas + TextPool) : PV, Tour / ATQ / DÉF, messages, voile rouge quand on est touché, écran de mort. Canvas en surimpression sous la carte. Voir docs/tour-par-tour.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
Hud  [Canvas, CanvasScaler, Hud]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### Hud

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **Hud**
  - `player` = (vide) — Le joueur suivi. Vide : cherché dans la scène.
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `restart` = `Player/Wait` — Recommencer après la mort (Player/Wait : Entrée, L3).
  - `restartAlt` = `Player/Interact` — Recommencer après la mort, autre bouton (Player/Interact : F, bouton du bas).
  - `toMenu` = `UI/Cancel` — Retour au menu depuis l'écran de mort (UI/Cancel : Échap, bouton droit).
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `healthColor` = (0.85, 0.1, 0.1) — Le rouge des PV, du voile quand on est touché et de « Vous êtes mort ».
  - `hurtFlash` = 0.35 — Opacité du voile rouge au moment du coup (il s'efface ensuite).
  - `toastTime` = 1.8 — Durée d'affichage d'un message (secondes), hors apparition et effacement.
  - `restartDelay` = 1 — Délai avant d'accepter « recommencer » sur l'écran de mort (évite un redémarrage involontaire).

### Hud / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### Hud / Textes

- **TextPool**
