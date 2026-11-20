# MonteeDeNiveau

`Assets/Prefabs/UI/MonteeDeNiveau.prefab` · [← liste des prefabs](README.md)

Écran de montée de niveau, dessiné en gravure : dès qu'un niveau attend son bienfait et que le joueur a fini son tour, le jeu se met en pause et propose trois cartes (ATQ +1, DÉF +1, PV max +2). Choix à la souris, aux flèches / stick, aux chiffres 1-3. Canvas en surimpression entre la carte et la pause. Construit par *Deckscent > Construire l'écran de niveau*. Voir docs/niveaux.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
MonteeDeNiveau  [Canvas, CanvasScaler, LevelUpScreen]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### MonteeDeNiveau

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **LevelUpScreen**
  - `player` = (vide) — Le joueur qui monte de niveau. Vide : cherché dans la scène.
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `navigate` = `UI/Navigate`
  - `submit` = `UI/Submit`
  - `acceptDelay` = 0.5 — Délai (secondes, temps réel) avant d'accepter un choix : une touche martelée pendant le combat ne choisit pas à l'aveugle.
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `accentColor` = (0.85, 0.1, 0.1) — Rouge d'accent : carte choisie, cœur de la vie.

### MonteeDeNiveau / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### MonteeDeNiveau / Textes

- **TextPool**
