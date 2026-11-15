# Sons : pistes, volumes, effets sonores

Tout le son du jeu passe par un **mixer** Unity à trois pistes, réglées par les options du menu (voir [menu.md](menu.md#options)). Les sons sont **synthétisés** par le code, sans fichier audio, comme la musique du menu.

## Les pistes (mixer `Assets/Resources/Mixage.mixer`)

```
Master            ← option « Général »         (paramètre exposé VolumeGeneral)
├── Musique       ← option « Musique »         (VolumeMusique)
└── Effets        ← option « Effets sonores »  (VolumeEffets)
```

- Chaque `AudioSource` choisit sa piste avec son champ **Output** : la musique du menu sort sur `Musique`, le joueur, les slimes et les dalles à pics sur `Effets`.
- Une piste fille passe par sa mère : le volume général baisse tout.
- Le mixer est dans un dossier **`Resources`** : le code le charge par son nom (`Resources.Load<AudioMixer>("Mixage")`), sans référence à glisser.

## Les volumes (`GameSettings`)

- Trois valeurs de 0 à 1 : `Volume` (général), `MusicVolume`, `EffectsVolume`. Changées par `SetVolume`, `SetMusicVolume`, `SetEffectsVolume` (arrondies au vingtième, sauvegardées dans `PlayerPrefs`).
- `Apply()` les envoie au mixer, **en décibels** : `20 × log10(volume)`. 1 donne 0 dB (plein volume), 0,5 environ -6 dB, 0 donne -80 dB (silence).
- **Au lancement**, Unity ignore les réglages du mixer tant que la scène ne tourne pas. `Init` relit les options avant la première scène (la langue en a besoin tout de suite), puis `ApplyAfterLoad` réapplique les volumes **une image plus tard** (`await Awaitable.NextFrameAsync()`).

## Les effets sonores

| Son | Quand | Volume (inspecteur) |
|---|---|---|
| **Pas** (4 variantes, une au hasard) | au début de chaque pas du joueur | `Step Volume` du joueur, 0,35 |
| **Attaque** (un « whoosh ») | quand le joueur frappe | `Attack Volume` du joueur, 0,8 |
| **Coup reçu** (impact sourd) | chaque fois que le joueur perd des PV, monstre ou piège (`PlayerController.TakeDamage`) | `Hit Volume` du joueur, 0,8 |
| **Slurp** | quand un slime attaque | `Slurp Volume` du slime, 0,9 |
| **Pics** (raclement + tintement métallique) | quand les pics d'une dalle sortent, qu'on soit dessus ou non (pas à l'alerte) | `Spikes Volume` de la dalle, 0,7 |

Une attaque s'entend donc en deux temps : le slurp ou les pics, puis le coup reçu. Tourner, se cogner, attendre : pas de son pour l'instant.

### Qui fait quoi

- **`SoundBank`** (`Assets/Scripts/Audio/SoundBank.cs`) : **fabrique** les sons. Chaque son est calculé échantillon par échantillon, une seule fois (à la première demande), puis gardé dans un `AudioClip` :
  - **pas** : deux contacts étouffés, le talon puis la pointe (plus faible, 35 à 55 ms après), faits de bruit filtré très grave (150 à 230 Hz) et d'un soupçon de gravier ; chaque contact monte en 5 ms. **Pas de note pure** : un sinus qui chute sonnait comme une grosse caisse de boîte à rythmes (trop électronique, trop dur) ;
  - **attaque** : du bruit dans un filtre résonant qui balaie de l'aigu au grave, avec une enveloppe qui monte vite et retombe ;
  - **slurp** : une note qui glisse de 180 à 520 Hz en gargouillant (vibrato à 28 Hz), un bruit mouillé qui la suit, et des petites bulles ;
  - **pics** : un raclement de 50 ms (bruit dans un filtre qui monte de 2,5 à 6 kHz), puis un tintement de métal (quatre fréquences inharmoniques, 1,9 à 5,9 kHz, qui s'éteignent vite) ;
  - **coup reçu** : un bruit grave dont le filtre se referme (420 → 120 Hz : le coup « s'enfonce ») et un claquement mat très bref, légèrement saturés.
- **`SoundEmitter`** (`Assets/Scripts/Audio/SoundEmitter.cs`) : **joue** les sons d'un objet. Il est posé avec une `AudioSource` sur le joueur, sur chaque slime et sur chaque dalle à pics ; `Play(clip, volume)` joue un son par-dessus les autres, légèrement désaccordé au hasard (`Pitch Jitter`, ±8 %) pour qu'une série de pas ne sonne pas mécanique. Si l'`AudioSource` n'a pas de piste, il la branche sur `Effets`.
- **Le joueur, le slime et la dalle** appellent `sounds.Play(SoundBank.Attack, attackVolume)` au bon moment (début d'animation, `TakeDamage`, sortie des pics dans `SpikeTrap.SetState`).
- Son **à plat** pour le joueur (`Spatial Blend` 0), **en 3D** pour les slimes et les dalles (1, atténuation linéaire de 3 à 24 m) : on entend d'où vient l'attaque.

## Changer un son

- **Le régler** : le volume dans l'inspecteur du prefab (`Player`, `Slime`), la variation de hauteur sur son `SoundEmitter`. Le timbre : les nombres dans la fonction du son, dans `SoundBank` (fréquences, durées, filtres), avec des commentaires.
- **Le remplacer par un vrai fichier** : importer le `.wav` / `.ogg`, mettre un champ `[SerializeField] AudioClip` sur le script qui joue le son, et l'utiliser à la place de `SoundBank.…`.
- **Ajouter un son** :
  1. une propriété dans `SoundBank` (sur le modèle de `Slurp`) ;
  2. un champ de volume dans le script qui le joue ;
  3. un appel `sounds.Play(…)` au bon moment ;
  4. si un nouvel objet doit faire du bruit, lui ajouter un `SoundEmitter` : dans son script de construction, `PrototypeBuilder.AddSoundEmitter(racine, 0 ou 1)`.

## Tests

- `SoundBankTests` (EditMode) : les sons existent, sont courts, ni muets ni saturés.
- `CombatTests.LesPasLesCoupsEtLeSlurpSeFontEntendreSurLaPisteEffets` (PlayMode) : un pas joue un bruit de pas, tourner n'en joue pas, le coup joue l'attaque, la riposte du slime joue le slurp puis le coup reçu, le tout sur la piste `Effets` (`SoundEmitter.Recent` garde les 8 derniers sons).
- `CombatTests.LaDalleAPicsBlesseUnTourSurTroisPuisEstFranchie` : l'alerte est muette, la sortie des pics joue leur son, et le joueur dessus joue le coup reçu.
- `MenuTests` : chaque jauge règle sa piste du mixer.
