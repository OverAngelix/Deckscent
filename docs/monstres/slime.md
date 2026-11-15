# Slime

Code : `Assets/Scripts/Enemies/Slime.cs` (hérite d'`Enemy`), prefab : `Assets/Prefabs/Enemies/Slime.prefab` ([doc du prefab](../prefabs/Slime.md)). Apparaît dans les tuiles « monstre » et les salles à monstres (voir [tuiles.md](../tuiles.md#ce-qui-apparaît)).

Un dôme de gelée qui rebondit, deux yeux qui suivent le joueur. Aucun slime dans les packs Synty : il est fait de sphères (corps, yeux, pupilles).

## Trois sortes

| Sorte | Couleur | PV | Attaque |
|---|---|---|---|
| Vert | vert vif | 3 | 1 |
| Rouge | rouge | 6 | 1 |
| Bleu | bleu | 3 | 2 |

Tirées à chances égales par le Spawner. La sorte se choisit avec `Variant` ; la table `Variants` de l'inspecteur règle la couleur et les stats de chacune. Couleur appliquée par un `MaterialPropertyBlock` sur le matériau `M_Toon_Slime` (un seul matériau pour les trois).

## Au tour par tour

Après chaque action du joueur :

1. **à côté du joueur** (case voisine, sans mur fin entre eux) : il **attaque** (bond vers lui, dégâts au bout du bond). Les attaques passent **l'une après l'autre** ;
2. sinon, s'il le **repère** (chemin de `Aggro Range` = 6 pas au plus, murs contournés) : il **bondit d'une case** vers lui, **en même temps que les autres monstres** ;
3. sinon il attend.

Un slime qui bondit n'attaque pas dans le même tour.

Il ne monte jamais sur la case du joueur ni sur celle d'un autre ennemi : les plus proches du joueur choisissent leur case d'abord, et un slime peut prendre la case que celui de devant vient de quitter (une file avance d'un bloc). Les dalles et les coffres ne le gênent pas : sur une dalle, il prend les pics comme le joueur ; sur un coffre, il le cache (le coffre réapparaît quand il part).

**Touché** : il blanchit un instant. **Mort** : il blanchit, s'étale en flaque et disparaît ; sa case est libre tout de suite.

## Lisible dans le noir

L'éclairage est très sombre (version choisie) : sans aide, un slime à deux cases se perdait dans les hachures. Son matériau a une **lueur propre** (`_Glow` 0,25 du shader toon, aussi sur le blanc des yeux) : sa couleur saturée reste vive, comme un accent. Au-delà de 8 m, l'obscurité du post-process le fait quand même s'éteindre peu à peu.

## Réglages (prefab `Slime`)

| Réglage | Défaut | Rôle |
|---|---|---|
| `Variant` | Vert | Sorte : applique couleur, PV et attaque de `Variants`. |
| `Defense` | 0 | Retirée des dégâts reçus (minimum 1). |
| `Aggro Range` | 6 | Distance de repérage, en pas. 0 = ne bouge jamais (attaque quand même à côté). |
| `Move Time` | 0,25 s | Durée d'un bond ou d'une attaque. |
| `Bounce Speed` | 1,6 | Rebonds par seconde. |
| `Squash` | 0,15 | Écrasement / étirement du corps. |
| `Turn Speed` | 4 | Vitesse à laquelle il se tourne vers le joueur. |
| `Hit Flash` | 0,12 s | Durée du flash blanc. |
