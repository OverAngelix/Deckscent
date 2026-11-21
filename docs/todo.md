# Todo

Liste des prochaines tâches, décidée avec l'utilisateur le 2026-11-16 (la todo du projet Godot n'est plus suivie). Objectif : le **jalon 2 du GDD**, un étage complet de bout en bout. Les idées plus lointaines sont dans le [GDD](gdd.md#11-idées-pour-plus-tard).

Cocher (✅) chaque tâche finie et validée.

## Dans l'ordre

0. ✅ **Finir la musique du donjon** (étape 7 du portage) : validation à l'écoute, puis commit.
1. ✅ **Touches affichées selon l'appareil**
   - Dernier appareil utilisé (`InputSystem.onActionChange` / `InputUser`) : clavier-souris, manette PlayStation (DualShock / DualSense), Switch, **Xbox par défaut** pour les autres manettes.
   - Texte des touches par `InputAction.GetBindingDisplayString` (groupe de contrôle de l'appareil).
   - Pictos (✕ ○ △ □, A B X Y, LB / RB…) dessinés en gravure avec `InkCanvas`, pas de sprites.
   - Avant le menu pause et la mission : les nouveaux écrans en profitent dès le départ.
2. ✅ **Menu pause** (Échap / Start) : Continuer, Options (celles du menu), Sauvegarder, Quitter (menu ou bureau).
   - Échap ferme d'abord la carte si elle est ouverte, sinon ouvre la pause.
   - `Time.timeScale = 0`, comme la carte.
3. ✅ **Mission de l'étage** (la « recette » du GDD, §2)
   - Objectifs tirés au hasard : X tours survécus, N salles nettoyées, N trésors ouverts, N monstres tués…
   - Affichée dans le HUD et sur la carte.
   - Une salle ne compte qu'**une fois nettoyée** : rattacher chaque monstre, piège et coffre à sa tuile (Spawner).
4. ✅ **Brouillard de guerre** : mémoire des cases visitées, la carte se dévoile en marchant. Nécessaire pour la tâche suivante.
5. ✅ **Salle du boss posée par le jeu** une fois la mission remplie (validé en jouant le 2026-11-21 : [antre.md](antre.md))
   - Sur un emplacement **jamais visité**, loin du joueur ; peut **remplacer** une tuile posée mais inexplorée, couloir compris.
   - Signalée sur la carte (crâne rouge).
6. ✅ **Boss loup-garou + étage suivant** (validé en jouant le 2026-11-21 : [loup-garou.md](monstres/loup-garou.md) ; animations procédurales, PV gardés et nouvelle main d'un étage à l'autre)
   - Modèle riggé et animé (pack Synty ou animations Mixamo), shader toon sur `SkinnedMeshRenderer`, vérifié par capture.
   - Mécaniques : charge en ligne droite annoncée (lueur, hurlement), 2ᵉ phase à 50 % des PV (appel de louveteaux), `BossMode` de la musique.
   - Le vaincre ouvre l'escalier : nouvel étage, nouvelle grille.
7. ✅ **XP et niveaux** (choix de l'utilisateur, validé le 2026-11-21 : [niveaux.md](niveaux.md))
   - XP des monstres tués (PV max + attaque), paliers 10, 15, 20…
   - À chaque niveau, un bienfait parmi trois : ATQ +1, DÉF +1, PV max +2. Les compétences de classe viendront plus tard s'ajouter au tirage.
   - Jauge d'XP au HUD, niveau atteint sur l'écran de mort.
8. ✅ **Or** (décidé et validé le 2026-11-21, [or.md](or.md)) : on en trouve dans les coffres, les vases et caisses, et sur les monstres. Compteur au HUD et sur la carte. À la mort, on ne garde que **10 %** de l'or de l'expédition (de quoi s'améliorer et aller plus loin).
9. 🟡 **Remontée** (faite le 2026-11-21, à valider en jouant : [remontee.md](remontee.md)) : après le boss du 3ᵉ étage (fin de sous-biome), l'escalier propose de **remonter** (on garde tout son or) ou de **descendre**. Boss par étage du sous-biome : 1 loup-garou, 2 golem élémentaire, 3 Slayer (modèles de POLYGON Fantasy Rivals, mécaniques à décider).
10. **Hub minimal** (jalon 4) : **camp 3D en surface** (packs Nature, Meadow Forest, Farm), parcouru en grille comme le donjon. On y dépense l'or pour **débloquer des cartes** (nouvelles formes et salles ajoutées à la pioche) ; on lance l'expédition depuis le hub ; progression sauvegardée.
   - La pioche **reste illimitée** pour l'instant (utile pour tester) : les déblocages ajoutent des cartes à son tirage.
   - **Choix de l'utilisateur (2026-11-21)** : ambiance **crépuscule** (feu de camp + faible clair de lune, gravure à recaler un peu), **ciel un peu sombre et étoilé avec un début de lune** (un croissant) ; on s'y déplace **case par case comme dans le donjon** ; au départ, pioche = couloirs + salle, galerie, salle en L, et les **8 autres salles** (longue galerie, T, Z, U, croix, grande salle, salle à piliers, crypte) **se débloquent contre l'or** ; après la **mort ou une remontée, retour au camp** (le titre ne sert plus qu'au lancement).
   - Étapes : (a) scène du camp (plan, décor extérieur, crépuscule, entrée du donjon, flux titre → camp → donjon → camp) ; (b) le cartographe et l'écran des déblocages (salles), plus des **améliorations permanentes** contre l'or (PV max, attaque de base… ; demande de l'utilisateur), sauvegardés dans la progression ; (c) finitions (PNJ, sons, musique du camp).
11. **Sauvegarde de suspension** (roguelike) : on sauvegarde en quittant, on reprend, la sauvegarde est effacée au chargement. Plateau, pioche, joueur, entités. À faire quand le contenu de l'étage est stable.

Après : lumière qui s'use, faim, pioche limitée et cartes en butin, classes, forge, choix de l'entrée du donjon. La date de la démo est choisie par l'utilisateur.

## En plus (recommandé)

- 🟡 **Règle du GDD** (2026-11-18, à valider) : pas de pose de tuile en combat (monstre éveillé à 3 pas ou moins, murs contournés : `Combat.InCombat`).
- 🟡 **Un 2ᵉ monstre** (2026-11-18, à valider) : la [mimique](monstres/mimique.md), embusquée dans les coffres des salles.
- **Pioche limitée** (jalon 3) : aujourd'hui, elle est infinie.
- 🟡 **Effets de particules** (2026-11-20, à valider) : pack POLYGON Particle FX, gardés après essai : étoiles du boss sonné, poussière (charge, mort, escalier, mimique, louveteaux), vases et caisses brisés. Retirés : feu, coup porté, giclée, soin, pièces d'or (pas d'effet pour l'or, 2026-11-21). Voir [effets.md](effets.md).
- 🟡 **Ressenti des coups** (en partie, 2026-11-18, à valider) : chiffres de dégâts gravés, éclat d'encre à l'impact, arrêt sur image. Reste : tremblement de caméra (Cinemachine Impulse) sur la charge du boss.
- 🟡 **Écran de mort avec le bilan** (2026-11-18, à valider) : monstres tués, coffres, boss vaincus, dégâts infligés. Le bilan du sous-sol quitté s'affiche aussi pendant la descente.
- 🟡 **Décor d'ambiance et petits éléments de jeu** (2026-11-18, à valider) : vases et caisses qui explosent (potion, carte), statues, bougies allumées, crânes (la fontaine est retirée pour le moment), voir [decor.md](decor.md).
- 🟡 **Tampon d'entrée** (2026-11-18, à valider) : une touche tapée pendant un pas est jouée ensuite.
- **Les succès sur le menu** : liste des succès consultable depuis l'écran titre.
