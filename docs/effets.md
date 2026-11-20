# Effets de particules

Demande de l'utilisateur (2026-11-20) : prendre le pack **POLYGON Particle FX** (Synty, v1.4.1) « pour rajouter du peps ». Importé dans `Assets/Synty/PolygonParticleFX/` comme les autres packs : **non versionné** (licence). Sans le pack, le jeu tourne sans effets.

Peu d'effets, choisis après essai par l'utilisateur : la poussière et les étoiles, qui se dessinent bien en gravure.

## Où on les voit

| Moment | Effet | Prefab (`Assets/Prefabs/FX/`) | Source Synty |
|---|---|---|---|
| Mort d'une mimique, d'un louveteau | bouffée de poussière | `Bouffee` | `FX_Dust_Small_01` |
| Boss sonné | **étoiles** qui tournent au-dessus de sa tête, tant qu'il est sonné | `Etourdi` | `FX_StarStunned_01` |
| Charge du boss dans un mur, mort du boss, sol qui s'effondre sur l'escalier | gros nuage de poussière | `Poussiere` | `FX_Dust_Big_01` |
| Vase brisé / caisse brisée | poussière / éclats de bois (en plus des éclats en cubes) | `VaseBrise`, `CaisseBrisee` | `FX_Dust_Small_01`, `FX_Impact_Wood_01` |

## Essayés puis retirés (retour de l'utilisateur, 2026-11-20)

- **Feu des torches et des braseros** (`FX_Fire_Small_01`, `FX_Embers_01`) : pas aimé. Torches et braseros gardent leur flamme blanche.
- **Coup porté** (éclats `FX_Impact_Small_01`, entaille `FX_Slash_01`) : retiré.
- **Giclée à la mort d'un slime** (`FX_BloodSplat_Small_01`) : retirée, le slime fond seul.
- **Soin** (`FX_Heal_01`) : pas d'effet.
- **Pièces d'or à l'ouverture d'un coffre** (`FX_Money_Coins_Burst_01`, pièces passées à notre or `M_Toon_Or`) : **idée gardée** pour quand le jeu aura de l'or (GDD §4 : l'or remonte au hub).

## Comment ça marche

- **`FxLibrary`** (`Assets/Resources/Effets.asset`) range les effets par usage ; **`Fx.Play(prefab, position…)`** en fait apparaître une instance, la recolore au besoin et la détruit une fois finie. Un effet en boucle (étoiles du boss) est gardé et arrêté par son appelant.
- **Construction** : *Deckscent > Construire les effets de particules (Particle FX)* (`FxBuilder.cs`) copie les effets choisis (dépaquetés, sans leurs sons ni lumières), convertit les matériaux et remplit la bibliothèque. Pour ajouter un effet : une ligne dans `FxBuilder.Effects` et un champ dans `FxLibrary`.
- **Matériaux** : le pack est fait pour l'ancien pipeline (« Legacy Shaders/Particles », roses sous URP). Chacun est converti en `Materials/FX/M_FX_<nom>` en « Universal Render Pipeline/Particles/Unlit », avec trois choix imposés par le rendu gravure :
  - **mélange alpha, jamais additif** : l'additif éclaircit le décor sans y mettre de couleur, le post-process n'en gardait que du papier. En alpha, une couleur saturée remplace le fond et sort du papier comme un **accent** ;
  - **pas de particules douces** : elles s'effacent près des surfaces ;
  - un matériau sans texture (blanc uni d'origine) reçoit `Assets/Textures/FX_Blanc.png` (versionnée).
- **Sans le pack** : les textures manquent ; `Fx` le voit (matériau de particules sans texture) et ne joue rien, plutôt que des carrés blancs.

Les effets passent par le post-process gravure : ce qui n'est pas saturé devient encre et papier (la poussière se dessine en hachures).
