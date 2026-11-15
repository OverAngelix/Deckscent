# MusiqueDonjon

`Assets/Prefabs/Audio/MusiqueDonjon.prefab` · [← liste des prefabs](README.md)

Musique du donjon, à couches (exploration, menace, coffre, combat, boss), synthétisée en temps réel sur la piste Musique du mixer. `MusicDirector` regarde la situation du joueur, `DungeonMusic` joue. Voir docs/musique.md.

*Pas d'image : ce prefab n'a rien de visible (caméra, lumières, scripts).*

## Hiérarchie

```
MusiqueDonjon  [AudioSource, DungeonMusic, MusicDirector]
```

## Contenu

### MusiqueDonjon

- **AudioSource**
- **DungeonMusic**
  - `tempo` = 100 — Tempo, en noires par minute.
  - `master` = 0.8 — Volume général du morceau (avant les options Général et Musique).
  - `fadeIn` = 3 — Durée du fondu d'entrée au lancement (secondes).
  - `seed` = -1 — Graine du tirage (scintillement du coffre…) ; -1 = différent à chaque lancement.
  - `baseLevel` = 1 — Exploration : basse, harpe, nappe, flûte. Toujours là.
  - `threatLevel` = 1 — Menace (monstres en vue) : cordes en ostinato, trémolo qui frotte, tambour feutré à partir de 2 monstres.
  - `combatLevel` = 1 — Combat (coups échangés) : tambours, caisse claire, basse qui martèle.
  - `chestLevel` = 1 — Coffre proche (sans monstre ni combat) : scintillement de glockenspiel.
  - `bossLevel` = 1 — Boss : cor grave, tambours lourds, autre suite d'accords.
  - `fadeUp` = 0.6 — Secondes pour qu'une couche monte à son volume.
  - `fadeDown` = 1.5 — Secondes pour qu'une couche redescende.
- **MusicDirector**
  - `sightRadius` = 6 — Distance (en cases) à laquelle un monstre en vue déclenche la menace.
  - `threatOneMonster` = 0.5 — Menace avec un seul monstre en vue (0 à 1). Chaque monstre de plus ajoute « Threat Per Extra Monster ».
  - `threatPerExtraMonster` = 0.25 — Menace ajoutée par monstre en vue au-delà du premier.
  - `calmTurns` = 2 — Tours sans aucun coup échangé avant que la musique de combat s'arrête.
  - `chestRadius` = 3 — Distance (en cases) à laquelle un coffre fermé en vue fait varier la musique.
  - `forceBoss` = non — Force la musique du boss (pour l'essayer en Play mode, en attendant le boss).
  - `music` = `MusiqueDonjon` — Le musicien. Vide : cherché sur le même objet.
  - `player` = (vide) — Le joueur. Vide : cherché dans la scène.
