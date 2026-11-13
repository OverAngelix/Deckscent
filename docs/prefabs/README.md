# Prefabs

> Généré par *Deckscent > Documenter les prefabs* (`Assets/Scripts/Editor/PrefabDocs.cs`). Ne pas éditer à la main : régénérer après chaque modification d'un prefab. Les descriptions sont dans `PrefabDocs.Descriptions`.

| Prefab | Chemin | Rôle |
|---|---|---|
| [Mur_01](Mur_01.md) | `Assets/Prefabs/Maze/Mur_01.prefab` | Pan de mur du décor (Synty `SM_Env_Wall_01`). Posé au milieu de chaque bord fermé d'une case de sol, face (+Z) vers la case. |
| [Mur_03](Mur_03.md) | `Assets/Prefabs/Maze/Mur_03.prefab` | Variante de pan de mur (Synty `SM_Env_Wall_03`). |
| [Mur_04](Mur_04.md) | `Assets/Prefabs/Maze/Mur_04.prefab` | Variante de pan de mur (Synty `SM_Env_Wall_04`). |
| [Mur_Gris](Mur_Gris.md) | `Assets/Prefabs/Maze/Mur_Gris.prefab` | Pan de mur de secours (bloc gris), utilisé si le pack Synty manque. |
| [Plafond](Plafond.md) | `Assets/Prefabs/Maze/Plafond.prefab` | Plafond d'une case (Synty `SM_Env_Ceiling_Stone_Flat_01`), tourné vers le bas : invisible vu d'en haut. |
| [Plafond_Gris](Plafond_Gris.md) | `Assets/Prefabs/Maze/Plafond_Gris.prefab` | Plafond de secours (quad gris tourné vers le bas). |
| [Sol](Sol.md) | `Assets/Prefabs/Maze/Sol.prefab` | Dalle de sol d'une case (Synty `SM_Env_Tiles_Texture_01`), centrée sur la case. |
| [Sol_Gris](Sol_Gris.md) | `Assets/Prefabs/Maze/Sol_Gris.prefab` | Sol de secours (bloc gris). |
| [Torche](Torche.md) | `Assets/Prefabs/Maze/Torche.prefab` | Torche murale, posée sur les cases `T`, accrochée au premier mur voisin. Sa racine est sur le mur, +Z vers la case. |
| [Player](Player.md) | `Assets/Prefabs/Player.prefab` | Le joueur : déplacement case par case au tour par tour, caméra à hauteur d'yeux et lanterne. Posé sur la case `P` du plan au démarrage. |
