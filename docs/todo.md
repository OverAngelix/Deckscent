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
5. 🟡 **Salle du boss posée par le jeu** une fois la mission remplie (fait le 2026-11-18, à valider en jouant : [antre.md](antre.md))
   - Sur un emplacement **jamais visité**, loin du joueur ; peut **remplacer** une tuile posée mais inexplorée, couloir compris.
   - Signalée sur la carte (crâne rouge).
6. 🟡 **Boss loup-garou + étage suivant** (fait le 2026-11-18, à valider en jouant : [loup-garou.md](monstres/loup-garou.md) ; animations procédurales, PV gardés et nouvelle main d'un étage à l'autre)
   - Modèle riggé et animé (pack Synty ou animations Mixamo), shader toon sur `SkinnedMeshRenderer`, vérifié par capture.
   - Mécaniques : charge en ligne droite annoncée (lueur, hurlement), 2ᵉ phase à 50 % des PV (appel de louveteaux), `BossMode` de la musique.
   - Le vaincre ouvre l'escalier : nouvel étage, nouvelle grille.
7. **Sauvegarde de suspension** (roguelike) : on sauvegarde en quittant, on reprend, la sauvegarde est effacée au chargement. Plateau, pioche, joueur, entités. À faire quand le contenu de l'étage est stable.

## En plus (recommandé)

- 🟡 **Règle du GDD** (2026-11-18, à valider) : pas de pose de tuile en combat (monstre éveillé à 3 pas ou moins, murs contournés : `Combat.InCombat`).
- 🟡 **Un 2ᵉ monstre** (2026-11-18, à valider) : la [mimique](monstres/mimique.md), embusquée dans les coffres des salles.
- **Pioche limitée** (jalon 3) : aujourd'hui, elle est infinie.
- 🟡 **Effets de particules** (2026-11-20, à valider) : pack POLYGON Particle FX, gardés après essai : étoiles du boss sonné, poussière (charge, mort, escalier, mimique, louveteaux), vases et caisses brisés. Retirés : feu, coup porté, giclée, soin ; pièces d'or des coffres gardées en idée (quand il y aura de l'or). Voir [effets.md](effets.md).
- 🟡 **Ressenti des coups** (en partie, 2026-11-18, à valider) : chiffres de dégâts gravés, éclat d'encre à l'impact, arrêt sur image. Reste : tremblement de caméra (Cinemachine Impulse) sur la charge du boss.
- 🟡 **Écran de mort avec le bilan** (2026-11-18, à valider) : monstres tués, coffres, boss vaincus, dégâts infligés. Le bilan du sous-sol quitté s'affiche aussi pendant la descente.
- 🟡 **Décor d'ambiance et petits éléments de jeu** (2026-11-18, à valider) : vases et caisses qui explosent (potion, carte), statues, bougies allumées, crânes (la fontaine est retirée pour le moment), voir [decor.md](decor.md).
- 🟡 **Tampon d'entrée** (2026-11-18, à valider) : une touche tapée pendant un pas est jouée ensuite.
- **Les succès sur le menu** : liste des succès consultable depuis l'écran titre.
