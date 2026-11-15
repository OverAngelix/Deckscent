# Prefabs

> Généré par *Deckscent > Documenter les prefabs* (`Assets/Scripts/Editor/PrefabDocs.cs`). Ne pas éditer à la main : régénérer après chaque modification d'un prefab. Les descriptions sont dans `PrefabDocs.Descriptions`.

| Prefab | Chemin | Rôle |
|---|---|---|
| [Slime](Slime.md) | `Assets/Prefabs/Enemies/Slime.prefab` | Ennemi simple, en formes simples (aucun slime dans les packs) : un dôme de gelée qui rebondit, deux yeux tournés vers le joueur. Trois sortes (vert 3 PV, rouge 6 PV, bleu 2 d'attaque) réglées par `variant` ; couleur saturée, donc gardée par le post-process (accent). Au tour par tour : attaque le joueur à côté, sinon s'en approche s'il est à moins de `aggroRange` pas. Posé par le Spawner (contenu « monstre »). Voir docs/monstres/slime.md. |
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
| [Coffre](Coffre.md) | `Assets/Prefabs/Props/Coffre.prefab` | Coffre doré (Synty `SM_Prop_Chest_03`, ou blocs si le pack manque), couvercle sur charnière. S'ouvre en avançant dessus ou avec Interact (un tour) : une carte de plus dans la main ou +3 PV, puis il s'enfonce dans le sol. Posé par le Spawner (contenu « tresor »). Voir docs/coffres.md. |
| [DallePics](DallePics.md) | `Assets/Prefabs/Traps/DallePics.prefab` | Piège : dalle à pics (grille et piques Synty `SM_Trap_Spears_Grate_01` / `SM_Trap_Spears_01`, ou plaque grise et cônes si le pack manque). Pics sortis 1 tour sur 3, pointes visibles le tour d'avant, 2 dégâts au joueur comme aux ennemis. Posé par le Spawner (contenu « piege »). Voir docs/pieges.md. |
| [CarteDuDonjon](CarteDuDonjon.md) | `Assets/Prefabs/UI/CarteDuDonjon.prefab` | Écran de carte : le plateau et la main de cartes-tuiles, dessinés en gravure (InkCanvas + TextPool), pour poser les tuiles. Canvas en surimpression, au-dessus du jeu et hors du post-process. Met le jeu en pause quand il est ouvert. Voir docs/carte.md. |
| [Hud](Hud.md) | `Assets/Prefabs/UI/Hud.prefab` | Interface en jeu, dessinée en gravure (InkCanvas + TextPool) : PV, Tour / ATQ / DÉF, messages, voile rouge quand on est touché, écran de mort. Canvas en surimpression sous la carte. Voir docs/tour-par-tour.md. |
