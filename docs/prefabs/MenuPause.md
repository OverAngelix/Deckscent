# MenuPause

`Assets/Prefabs/UI/MenuPause.prefab` · [← liste des prefabs](README.md)

Menu pause, dessiné en gravure (InkCanvas + TextPool) : Continuer, Options (le panneau du menu titre : son, langue), Sauvegarder (à venir), Retour au menu, Quitter le jeu. Ouvert par Échap / Start (la carte passe d'abord), met le jeu en pause. Canvas en surimpression au-dessus de la carte. Construit par *Deckscent > Construire le menu pause*. Voir docs/menu.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
MenuPause  [Canvas, CanvasScaler, PauseScreen]
├─ Encre  [CanvasRenderer, InkCanvas]
└─ Textes  [TextPool]
```

## Contenu

### MenuPause

- **RectTransform**
  - échelle (0, 0, 0)
- **Canvas**
- **CanvasScaler**
- **PauseScreen**
  - `player` = (vide) — Le joueur (pas de pause sur l'écran de mort). Vide : cherché dans la scène.
  - `map` = (vide) — L'écran de carte (Échap le ferme d'abord). Vide : cherché dans la scène.
  - `ink` = `Encre`
  - `texts` = `Textes`
  - `toggle` = `Player/Pause` — Ouvrir / fermer la pause (Player/Pause : Échap, Start).
  - `navigate` = `UI/Navigate`
  - `submit` = `UI/Submit`
  - `cancel` = `UI/Cancel`
  - `paperColor` = (0.97, 0.96, 0.93)
  - `inkColor` = (0.06, 0.06, 0.07)
  - `accentColor` = (0.85, 0.1, 0.1) — Rouge d'accent : ligne choisie, réglage choisi.

### MenuPause / Encre

- **CanvasRenderer**
- **InkCanvas**
  - `m_Material` = (vide)
  - `m_Color` = (1, 1, 1)
  - `m_RaycastTarget` = non
  - `m_Maskable` = oui

### MenuPause / Textes

- **TextPool**
