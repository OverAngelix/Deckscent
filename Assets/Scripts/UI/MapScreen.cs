using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Écran de carte : le plateau du donjon presque en plein écran et la main de cartes-tuiles à
    /// poser dessus, dessinés comme une gravure (papier, encre, hachures). Ouvert / fermé avec
    /// l'action Player/Map (virgule en AZERTY, bouton du haut). Met le jeu en pause tant qu'il est
    /// ouvert : le joueur ne connaît pas la carte, il est juste en pause.
    ///
    /// Commandes (map « Map » d'InputSystem_Actions, plus la souris) :
    ///   clic sur une carte / 1 à 6 / LB RB / Tab : choisir une carte
    ///   clic sur le plateau / Entrée, Espace / bouton du bas : poser
    ///   clic droit, molette / R / bouton gauche : tourner
    ///   Suppr, Retour arrière / Select : défausser
    ///   flèches / croix, stick : déplacer le curseur sur le plateau
    ///   Échap / bouton droit, ou la touche de la carte : fermer
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        [Tooltip("Le donjon (plateau + pioche). Vide : cherché dans la scène.")]
        [SerializeField] Dungeon dungeon;
        [Tooltip("Le joueur (flèche rouge sur la carte). Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;
        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [Tooltip("Ouvrir / fermer la carte (Player/Map).")]
        [SerializeField] InputActionReference toggle;
        [SerializeField] InputActionReference close;
        [SerializeField] InputActionReference rotate;
        [SerializeField] InputActionReference discard;
        [SerializeField] InputActionReference place;
        [SerializeField] InputActionReference nextCard;
        [SerializeField] InputActionReference previousCard;
        [SerializeField] InputActionReference cursor;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Écartement des hachures de la roche et des murs (plus petit = plus sombre). Tout ce qui n'est pas du sol est hachuré pareil : la carte est d'un seul tenant.")]
        [SerializeField, Range(2f, 12f)] float wallHatchSpacing = 6f;
        [Tooltip("Couleur des hachures du plateau : un gris encre, plus doux que l'encre pleine (des traits noirs serrés sur toute la carte papillotent).")]
        [SerializeField] Color hatchColor = new Color(0.42f, 0.42f, 0.43f);
        [SerializeField] Color validColor = new Color(0.15f, 0.6f, 0.25f);
        [SerializeField] Color invalidColor = new Color(0.85f, 0.12f, 0.1f);
        [Tooltip("Flèche du joueur et carte choisie (rouge d'accent, comme en jeu).")]
        [SerializeField] Color playerColor = new Color(0.85f, 0.1f, 0.1f);
        [Tooltip("Voile sombre derrière la carte.")]
        [SerializeField] Color backdropColor = new Color(0.02f, 0.02f, 0.025f, 0.7f);
        [Tooltip("Part de l'écran occupée par la carte.")]
        [SerializeField, Range(0.5f, 1f)] float screenFill = 0.95f;
        [Tooltip("Clé de traduction (voir Loc) du nom du sous-biome affiché à côté du titre (provisoire, viendra avec les étages).")]
        [SerializeField] string biomeKey = "biome.sous_sols";

        const float Pad = 18f, TitleHeight = 40f, HintHeight = 24f, Coord = 16f;
        const float CardGap = 16f, CardNote = 16f;
        /// <summary>Largeur maximale de la colonne de légende.</summary>
        const float LegendWidth = 130f;
        const int CardsPerRow = 3;
        const float CursorRepeat = 0.18f;
        const string Hint = "carte.aide";
        static readonly Vector2Int NoSlot = new Vector2Int(-1, -1);
        static readonly Vector2[] DirVectors = { new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0) };

        public bool IsOpen { get; private set; }
        public int Selected { get; private set; }

        Canvas _canvas;
        DungeonBoard _board;
        TileDeck _deck;
        Vector2Int _hover = NoSlot;
        int _hoverCard = -1;
        string _message = "";
        bool _messageIsError;
        bool _dirty = true;
        Vector2 _lastMouse;
        float _nextCursorMove;
        Vector2 _boardOrigin;
        float _cellPx = 1f;
        readonly List<Rect> _cardRects = new List<Rect>();
        /// <summary>Carte → posable quelque part (HasAnyPlacement, coûteux pour une salle). Vidé quand le plateau ou la main change.</summary>
        readonly Dictionary<TileCard, bool> _placeable = new Dictionary<TileCard, bool>();
        InputAction _toggle, _close, _rotate, _discard, _place, _next, _previous, _cursor;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            if (dungeon == null)
                dungeon = FindAnyObjectByType<Dungeon>();
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            _toggle = Resolve(toggle, "Player/Map");
            _close = Resolve(close, "Map/Close");
            _rotate = Resolve(rotate, "Map/Rotate");
            _discard = Resolve(discard, "Map/Discard");
            _place = Resolve(place, "Map/Place");
            _next = Resolve(nextCard, "Map/NextCard");
            _previous = Resolve(previousCard, "Map/PreviousCard");
            _cursor = Resolve(cursor, "Map/Cursor");
            SetOpen(false);
        }

        void OnEnable() => _toggle?.Enable();

        void OnDisable()
        {
            if (IsOpen)
                SetOpen(false);
        }

        static InputAction Resolve(InputActionReference reference, string path) =>
            reference != null ? reference.action : InputSystem.actions?.FindAction(path);

        IEnumerable<InputAction> MapActions => new[] { _close, _rotate, _discard, _place, _next, _previous, _cursor };

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            IsOpen = open;
            if (_canvas != null)
                _canvas.enabled = open;
            // Le joueur ne connaît pas la carte : le monde est en pause tant qu'elle est ouverte.
            Time.timeScale = open ? 0f : 1f;
            foreach (var action in MapActions)
                if (action != null)
                {
                    if (open) action.Enable();
                    else action.Disable();
                }
            _message = "";
            _hover = NoSlot;
            _hoverCard = -1;
            _dirty = true;
            if (open && Mouse.current != null)
                _lastMouse = Mouse.current.position.ReadValue();
        }

        // --- Entrées -------------------------------------------------------------

        void Update()
        {
            TrackDungeon();
            // Mort : la carte ne s'ouvre plus (l'écran de mort attend « recommencer »).
            if (_toggle != null && _toggle.WasPressedThisFrame() && (IsOpen || player == null || !player.IsDead))
            {
                SetOpen(!IsOpen);
                if (!IsOpen)
                    return;
            }
            if (!IsOpen)
                return;
            if (_close != null && _close.WasPressedThisFrame())
            {
                Close();
                return;
            }
            HandleMouse();
            HandleKeys();
            if (_dirty)
                Redraw();
        }

        /// <summary>Le Donjon peut recréer son plateau (nouvelle partie) : on suit le plateau et la main en cours.</summary>
        void TrackDungeon()
        {
            if (dungeon == null || (dungeon.Board == _board && dungeon.Deck == _deck))
                return;
            if (_board != null) _board.TilesChanged -= OnChanged;
            if (_deck != null) _deck.HandChanged -= OnChanged;
            _board = dungeon.Board;
            _deck = dungeon.Deck;
            if (_board != null) _board.TilesChanged += OnChanged;
            if (_deck != null) _deck.HandChanged += OnChanged;
            OnChanged();
        }

        void OnChanged()
        {
            _placeable.Clear();
            _dirty = true;
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || ink == null)
                return;
            var screen = mouse.position.ReadValue();
            if ((screen - _lastMouse).sqrMagnitude > 0.5f)
            {
                _lastMouse = screen;
                UpdateHover(ScreenToInk(screen));
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_hoverCard >= 0)
                    Select(_hoverCard);
                else if (_board != null && _board.IsInside(_hover))
                    TryPlace(_hover);
            }
            if (mouse.rightButton.wasPressedThisFrame)
                RotateSelected(1);
            var scroll = mouse.scroll.ReadValue().y;
            if (scroll < 0f) RotateSelected(1);
            else if (scroll > 0f) RotateSelected(-1);
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && _deck != null)
                for (var i = 0; i < 6; i++)
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame && i < _deck.Hand.Count)
                        Select(i);
            if (_rotate != null && _rotate.WasPressedThisFrame()) RotateSelected(1);
            if (_discard != null && _discard.WasPressedThisFrame()) DiscardSelected();
            if (_next != null && _next.WasPressedThisFrame()) Select(Selected + 1);
            if (_previous != null && _previous.WasPressedThisFrame()) Select(Selected - 1);
            if (_place != null && _place.WasPressedThisFrame() && _board != null)
                TryPlace(_board.IsInside(_hover) ? _hover : CursorStart());

            // Curseur au clavier ou à la manette : un emplacement à la fois, avec répétition.
            if (_cursor == null || _board == null)
                return;
            var move = _cursor.ReadValue<Vector2>();
            if (move.sqrMagnitude < 0.25f)
            {
                _nextCursorMove = 0f;
                return;
            }
            if (Time.unscaledTime < _nextCursorMove)
                return;
            _nextCursorMove = Time.unscaledTime + CursorRepeat;
            var step = Mathf.Abs(move.x) > Mathf.Abs(move.y)
                ? new Vector2Int(move.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, move.y > 0 ? -1 : 1);
            var from = _board.IsInside(_hover) ? _hover : CursorStart();
            var to = from + step;
            if (_board.IsInside(to))
                _hover = to;
            else
                _hover = from;
            _hoverCard = -1;
            _dirty = true;
        }

        /// <summary>Où commence le curseur : l'emplacement du joueur.</summary>
        Vector2Int CursorStart()
        {
            if (player == null || dungeon == null || dungeon.Maze == null)
                return _board.StartTile;
            var cell = dungeon.Maze.WorldToCell(player.transform.position);
            return new Vector2Int(cell.x / DungeonBoard.TileCells, cell.y / DungeonBoard.TileCells);
        }

        Vector2 ScreenToInk(Vector2 screen)
        {
            var rt = ink.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var local);
            var r = rt.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        void UpdateHover(Vector2 pos)
        {
            _hoverCard = -1;
            for (var i = 0; i < _cardRects.Count; i++)
                if (_cardRects[i].Contains(pos))
                    _hoverCard = i;
            var tilePx = _cellPx * DungeonBoard.TileCells;
            var slot = new Vector2Int(Mathf.FloorToInt((pos.x - _boardOrigin.x) / tilePx), Mathf.FloorToInt((pos.y - _boardOrigin.y) / tilePx));
            _hover = _board != null && _board.IsInside(slot) ? slot : NoSlot;
            _dirty = true;
        }

        // --- Actions -------------------------------------------------------------

        public TileCard SelectedCard => _deck != null && Selected >= 0 && Selected < _deck.Hand.Count ? _deck.Hand[Selected] : null;

        public void Select(int index)
        {
            if (_deck == null || _deck.Hand.Count == 0)
                return;
            Selected = (index % _deck.Hand.Count + _deck.Hand.Count) % _deck.Hand.Count;
            _dirty = true;
        }

        public void RotateSelected(int quarterTurns)
        {
            var card = SelectedCard;
            if (card == null)
                return;
            card.Rotate(quarterTurns);
            _dirty = true;
        }

        public void DiscardSelected()
        {
            if (SelectedCard == null)
                return;
            _deck.Replace(Selected);
            ShowMessage(Loc.T("carte.defaussee"), false);
        }

        /// <summary>Essaie de poser la carte choisie (ancre sur `slot`). Vrai si posée.</summary>
        public bool TryPlace(Vector2Int slot)
        {
            var card = SelectedCard;
            if (card == null || _board == null)
                return false;
            var reason = _board.CheckPlacement(slot, card);
            if (reason != "")
            {
                ShowMessage(Loc.T(reason), true);
                return false;
            }
            dungeon.Place(slot, card);
            _deck.Replace(Selected);
            ShowMessage(Loc.F("carte.tuile_posee", card.DisplayName), false);
            return true;
        }

        /// <summary>Simule le curseur au-dessus d'un emplacement (captures, tests).</summary>
        public void PreviewSlot(Vector2Int slot)
        {
            _hover = slot;
            _dirty = true;
            if (IsOpen)
                Redraw();
        }

        void ShowMessage(string text, bool isError)
        {
            _message = text;
            _messageIsError = isError;
            _dirty = true;
        }

        // --- Dessin : mise en page ----------------------------------------------

        Color Faded(float amount) => Engraving.Fade(inkColor, paperColor, amount);

        public void Redraw()
        {
            _dirty = false;
            if (ink == null || texts == null)
                return;
            ink.Clear();
            texts.Begin();
            if (_board != null && _deck != null)
                Draw();
            ink.Commit();
            texts.End();
        }

        void Draw()
        {
            var size = ink.Size;
            ink.Rect(new Rect(Vector2.zero, size), backdropColor);
            var margin = size * (1f - screenFill) * 0.5f;
            var panel = new Rect(margin, size - margin * 2f);
            ink.Rect(panel, paperColor);
            Engraving.DoubleFrame(ink, Shrink(panel, 4f), 4f, inkColor);

            // En-tête : titre, étage, état, filet.
            var headTop = panel.y + Pad + 2f;
            var title = Loc.T("carte.titre").ToUpperInvariant();
            texts.Label(title, panel.x + Pad + 4f, headTop, 600f, 26, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            var titleW = TextPool.Width(title, 26, FontStyle.Bold);
            texts.Label($"{Loc.T(biomeKey)} 1", panel.x + Pad + 4f + titleW + 20f, headTop + 7f, 300f, 17, Faded(0.4f), TextAnchor.UpperLeft, FontStyle.Italic);
            DrawStatus(panel, headTop + 6f);
            Engraving.Rule(ink, new Vector2(panel.x + Pad, panel.y + Pad + TitleHeight), panel.width - Pad * 2f, inkColor);

            var content = new Rect(panel.x + Pad, panel.y + Pad + TitleHeight + 12f,
                panel.width - Pad * 2f, panel.height - Pad * 2f - TitleHeight - 12f - HintHeight);
            // À droite : la main (3 cartes par rangée) et la légende. À gauche : le plateau.
            var handW = CardsPerRow * Engraving.CardSize.x + (CardsPerRow - 1) * CardGap + 8f;
            var handArea = new Rect(content.xMax - handW, content.y, handW, content.height);
            var boardArea = new Rect(content.x, content.y, content.width - handW - Pad * 2f, content.height);
            DrawBoard(boardArea);
            var handEnd = DrawHand(handArea);

            // Légende : une colonne étroite entre le plateau et la main si la place le permet,
            // sinon sous la main (s'il reste de la place).
            var boardPx = _cellPx * DungeonBoard.TileCells * (Vector2)_board.Size;
            var column = new Rect(_boardOrigin.x + boardPx.x + Pad, _boardOrigin.y,
                Mathf.Min(LegendWidth, handArea.x - _boardOrigin.x - boardPx.x - Pad * 2f), boardPx.y);
            if (column.width >= 90f)
                DrawLegend(column, 1);
            else
            {
                var legend = new Rect(handArea.x, handEnd + 8f, handW, content.yMax - handEnd - 8f);
                if (legend.height >= 100f)
                    DrawLegend(legend, 2);
            }

            texts.Label(Loc.T(Hint), panel.x, panel.yMax - Pad - 14f, panel.width, 14, Faded(0.45f), TextAnchor.UpperCenter, FontStyle.Italic);
        }

        /// <summary>Message en haut à droite : raison du refus sous le curseur, ou dernier message.</summary>
        void DrawStatus(Rect panel, float top)
        {
            var text = _message;
            var isError = _messageIsError;
            var card = SelectedCard;
            if (card != null && _board.IsInside(_hover) && !_board.HasTile(_hover))
            {
                var reason = _board.CheckPlacement(_hover, card);
                text = reason != "" ? Loc.T(reason) : Loc.T("carte.pose_possible");
                isError = reason != "";
            }
            if (string.IsNullOrEmpty(text) && _board.OpenExits().Count == 0)
            {
                text = Loc.T("carte.donjon_ferme");
                isError = true;
            }
            if (string.IsNullOrEmpty(text))
                return;
            texts.Label(text, panel.x, top, panel.width - Pad - 4f, 18, isError ? invalidColor : validColor, TextAnchor.UpperRight, FontStyle.Italic);
        }

        // --- Dessin : plateau ---------------------------------------------------

        void DrawBoard(Rect area)
        {
            var tiles = _board.Size;
            var gridCells = tiles * DungeonBoard.TileCells;
            var inner = new Rect(area.x + Coord, area.y + Coord, area.width - Coord * 2f, area.height - Coord * 2f);
            _cellPx = Mathf.Floor(Mathf.Min(inner.width / gridCells.x, inner.height / gridCells.y));
            var tilePx = _cellPx * DungeonBoard.TileCells;
            var boardPx = (Vector2)gridCells * _cellPx;
            // Calé à gauche, centré en hauteur : la place libre à droite reçoit la légende.
            _boardOrigin = new Vector2(Mathf.Floor(inner.x), Mathf.Floor(inner.y + (inner.height - boardPx.y) * 0.5f));
            var boardRect = new Rect(_boardOrigin, boardPx);

            // Tout est roche au départ, hachurée comme les murs : la carte est d'un seul tenant, le
            // sol des tuiles posées s'y découpe en papier. Emplacements libres en pointillés.
            ink.Rect(boardRect, paperColor);
            ink.Hatch(boardRect, wallHatchSpacing, hatchColor);
            for (var i = 1; i < tiles.x; i++)
            {
                var x = _boardOrigin.x + i * tilePx;
                ink.DashedLine(new Vector2(x, boardRect.yMin), new Vector2(x, boardRect.yMax), Faded(0.55f), 1f, 3f);
            }
            for (var j = 1; j < tiles.y; j++)
            {
                var y = _boardOrigin.y + j * tilePx;
                ink.DashedLine(new Vector2(boardRect.xMin, y), new Vector2(boardRect.xMax, y), Faded(0.55f), 1f, 3f);
            }

            // Tuiles posées : le sol en papier (les murs sont déjà la roche hachurée), contours et
            // murs fins à l'encre.
            var card = SelectedCard;
            var validAnchors = new List<Vector2Int>();
            for (var y = 0; y < tiles.y; y++)
                for (var x = 0; x < tiles.x; x++)
                {
                    var slot = new Vector2Int(x, y);
                    if (_board.HasTile(slot))
                        DrawFloor(_boardOrigin + (Vector2)slot * tilePx, _cellPx, _board.CellsAt(slot), inkColor);
                    else if (card != null && _board.CanPlace(slot, card))
                        validAnchors.Add(slot);
                }
            DrawEdges(_boardOrigin, _cellPx, _board.ToLayout().Split('\n'), true, inkColor, 2f);
            foreach (var (cell, dir) in _board.EdgeWalls())
                EdgeLine(_boardOrigin, _cellPx, cell, dir, inkColor, 2.5f);

            // Sorties encore libres des tuiles posées : une petite flèche, comme sur les cartes.
            // Pas sous l'aperçu de la carte à poser (les deux se superposeraient).
            var previewSlots = new HashSet<Vector2Int>();
            if (card != null && _board.IsInside(_hover) && !_board.HasTile(_hover))
                foreach (var offset in card.Slots)
                    previewSlots.Add(_hover + offset);
            foreach (var (slot, dir) in _board.FreeOpenings())
            {
                if (previewSlots.Contains(slot + TileCard.Offset(dir)))
                    continue;
                var i = System.Array.IndexOf(TileCard.Dirs, dir);
                var center = _boardOrigin + ((Vector2)slot + Vector2.one * 0.5f) * tilePx;
                ExitArrow(center + DirVectors[i] * tilePx * 0.5f, DirVectors[i], inkColor);
            }

            // Où la carte choisie peut être posée telle quelle : sa forme entière en pointillés verts.
            foreach (var anchor in validAnchors)
                foreach (var offset in card.Slots)
                {
                    var r = new Rect(_boardOrigin + (Vector2)(anchor + offset) * tilePx, Vector2.one * tilePx);
                    var sides = RectSides(r);
                    for (var i = 0; i < 4; i++)
                        if (!card.HasSlot(offset + TileCard.Offset(TileCard.Dirs[i])))
                            ink.DashedLine(sides[i].Item1, sides[i].Item2, validColor, 2f, 4f);
                }

            // Aperçu de la carte sous le curseur : tout son plan le suit.
            if (card != null && _board.IsInside(_hover) && !_board.HasTile(_hover))
            {
                var ok = _board.CanPlace(_hover, card);
                var color = ok ? validColor : invalidColor;
                var b = card.Bounds();
                var pos = _boardOrigin + (Vector2)(_hover + b.position) * tilePx;
                var rows = card.FullRows();
                DrawPlan(pos, _cellPx, rows, color, wallHatchSpacing);
                DrawEdges(pos, _cellPx, rows, false, color, 2f);
                CardEdgeWalls(card, pos, _cellPx, color, 2.5f);
                foreach (var offset in card.Slots)
                    ink.RectOutline(new Rect(_boardOrigin + (Vector2)(_hover + offset) * tilePx, Vector2.one * tilePx), color, 2f);
                // Les entrées (portes d'une salle, passages d'un couloir) : une flèche sur chaque,
                // pour voir où la tuile s'ouvre avant de la poser.
                foreach (var offset in card.Slots)
                {
                    var slotRect = new Rect(_boardOrigin + (Vector2)(_hover + offset) * tilePx, Vector2.one * tilePx);
                    for (var i = 0; i < 4; i++)
                        if (card.HasDoor(offset, TileCard.Dirs[i]))
                            ExitArrow(slotRect.center + DirVectors[i] * tilePx * 0.5f, DirVectors[i], color);
                }
            }

            Engraving.DoubleFrame(ink, boardRect, 3f, inkColor);
            DrawCoordinates(boardRect, tilePx);
            DrawPlayer();
        }

        /// <summary>Lettres au-dessus des colonnes, chiffres à gauche des rangées (comme une carte ancienne).</summary>
        void DrawCoordinates(Rect boardRect, float tilePx)
        {
            var color = Faded(0.35f);
            for (var i = 0; i < _board.Size.x; i++)
                texts.Label(((char)('A' + i)).ToString(), boardRect.x + i * tilePx, boardRect.y - 22f, tilePx, 12, color, TextAnchor.UpperCenter);
            for (var j = 0; j < _board.Size.y; j++)
                texts.Label((j + 1).ToString(), boardRect.x - Coord - 6f, boardRect.y + (j + 0.5f) * tilePx - 9f, Coord, 12, color, TextAnchor.UpperRight);
        }

        void DrawPlayer()
        {
            if (player == null || dungeon == null || dungeon.Maze == null)
                return;
            var maze = dungeon.Maze;
            var local = maze.transform.InverseTransformPoint(player.transform.position);
            var gridPos = new Vector2(local.x, -local.z) / maze.CellSize;
            var center = _boardOrigin + gridPos * _cellPx;
            var yaw = player.transform.eulerAngles.y * Mathf.Deg2Rad;
            var fwd = new Vector2(Mathf.Sin(yaw), -Mathf.Cos(yaw));
            var right = new Vector2(-fwd.y, fwd.x);
            var s = _cellPx * 1.1f;
            var arrow = new[]
            {
                center + fwd * s * 0.6f,
                center - fwd * s * 0.4f + right * s * 0.45f,
                center - fwd * s * 0.15f,
                center - fwd * s * 0.4f - right * s * 0.45f,
            };
            // En deux triangles : la flèche a une encoche à l'arrière.
            ink.Triangle(arrow[0], arrow[1], arrow[2], playerColor);
            ink.Triangle(arrow[0], arrow[2], arrow[3], playerColor);
            ink.Polyline(arrow, inkColor, 1.5f, true);
        }

        // --- Dessin : la main ---------------------------------------------------

        /// <summary>Les cartes en rangées de 3. Renvoie le bas de la main (y).</summary>
        float DrawHand(Rect area)
        {
            var count = _deck.Hand.Count;
            _cardRects.Clear();
            texts.Label(Loc.T("carte.main").ToUpperInvariant(), area.x, area.y, area.width, 16, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            texts.Label(Loc.F("carte.nb_cartes", count), area.x, area.y + 1f, area.width, 14, Faded(0.4f), TextAnchor.UpperRight, FontStyle.Italic);
            var top = area.y + 32f;
            var cardW = Engraving.CardSize.x;
            var cardH = Engraving.CardSize.y;
            var rows = (count + CardsPerRow - 1) / CardsPerRow;
            for (var row = 0; row < rows; row++)
            {
                var inRow = Mathf.Min(CardsPerRow, count - row * CardsPerRow);
                var rowW = inRow * cardW + (inRow - 1) * CardGap;
                var x0 = area.x + (area.width - rowW) * 0.5f;
                var y = top + row * (cardH + CardGap + CardNote);
                for (var k = 0; k < inRow; k++)
                {
                    var i = row * CardsPerRow + k;
                    var card = _deck.Hand[i];
                    var isSelected = i == Selected;
                    var lift = isSelected ? 10f : i == _hoverCard ? 5f : 0f;
                    var rect = new Rect(x0 + k * (cardW + CardGap), y - lift, cardW, cardH);
                    _cardRects.Add(rect);
                    if (!_placeable.TryGetValue(card, out var placeable))
                        _placeable[card] = placeable = _board.HasAnyPlacement(card);
                    DrawCard(card, rect, i, isSelected, placeable, lift);
                    if (!placeable)
                        texts.Label(Loc.T("carte.aucune_place"), rect.x, y + cardH + 2f, cardW, 13, invalidColor, TextAnchor.UpperCenter, FontStyle.Italic);
                }
            }
            return top + rows * (cardH + CardGap + CardNote) - CardGap;
        }

        /// <summary>
        /// Une carte façon carte à jouer gravée : double cadre, numéro, emprise, plan de la tuile
        /// au centre, bandeau du nom, torche et danger en pied.
        /// </summary>
        void DrawCard(TileCard card, Rect rect, int index, bool isSelected, bool placeable, float lift)
        {
            var accent = isSelected ? playerColor : inkColor;
            var inner = Engraving.CardFrame(ink, rect, paperColor, accent, isSelected ? 3f : 1.5f, accent, inkColor, lift);

            // Numéro (touche 1 à 6) et emprise sur le plateau.
            texts.Label((index + 1).ToString(), inner.x + 6f, inner.y + 2f, 30f, 17, accent, TextAnchor.UpperLeft, FontStyle.Bold);
            DrawFootprint(card, new Vector2(inner.xMax - 6f, inner.y + 6f));

            // Le plan de la tuile (une salle est réduite pour tenir dans le carré), avec une
            // flèche sur chaque passage ou porte.
            var area = Engraving.CardArtRect(inner);
            var rows = card.FullRows();
            var grid = new Vector2(rows[0].Length, rows.Length);
            var cell = Mathf.Floor(Mathf.Min(area.width / grid.x, area.height / grid.y));
            var art = new Rect(Floor(area.center - grid * cell * 0.5f), grid * cell);
            DrawPlan(art.position, cell, rows, inkColor, Engraving.CardHatch);
            DrawEdges(art.position, cell, rows, false, inkColor, 1.5f);
            CardEdgeWalls(card, art.position, cell, inkColor, 1.5f);
            var b = card.Bounds();
            foreach (var offset in card.Slots)
            {
                var slotRect = new Rect(art.position + (Vector2)(offset - b.position) * cell * 3f, Vector2.one * cell * 3f);
                for (var i = 0; i < 4; i++)
                    if (card.HasDoor(offset, TileCard.Dirs[i]))
                        ExitArrow(slotRect.center + Vector2.Scale(DirVectors[i], slotRect.size * 0.5f), DirVectors[i], accent);
            }

            // Bandeau du nom, en capitales.
            var band = Engraving.CardBandRect(inner, area);
            Engraving.Banner(ink, texts, band, card.DisplayName.ToUpperInvariant(), paperColor, inkColor);

            // Pied : flamme si la carte porte une torche, danger de 0 à 3 crânes.
            var footY = band.yMax + 12f;
            if (card.Torch)
                Engraving.Flame(ink, new Vector2(inner.x + 11f, footY), 7f, inkColor, paperColor);
            for (var i = 0; i < TileCard.MaxDanger; i++)
                Engraving.Skull(ink, new Vector2(inner.xMax - 11f - (TileCard.MaxDanger - 1 - i) * 16f, footY), 13f, i < card.Danger, inkColor, paperColor);

            // Carte impossible à poser : voilée de hachures.
            if (!placeable)
                ink.Hatch(inner, 5f, WithAlpha(inkColor, 0.25f));
        }

        /// <summary>Emprise sur le plateau : « 2×2 » et un petit carré par emplacement, dans la forme de la tuile.</summary>
        void DrawFootprint(TileCard card, Vector2 topRight)
        {
            const float sq = 5f;
            var b = card.Bounds();
            var grid = (Vector2)b.size * (sq + 1f) - Vector2.one;
            var origin = new Vector2(topRight.x - grid.x, topRight.y + 1f);
            foreach (var offset in card.Slots)
                ink.Rect(new Rect(origin + (Vector2)(offset - b.position) * (sq + 1f), new Vector2(sq, sq)), inkColor);
            texts.Label($"{b.size.x}×{b.size.y}", origin.x - 40f, topRight.y - 2f, 36f, 13, inkColor, TextAnchor.UpperRight);
        }

        /// <summary>
        /// Petite flèche hors du plan, au milieu d'un côté ouvert, pointée vers l'extérieur.
        /// `scale` : 1 sur les cartes, plus grand sur le plateau.
        /// </summary>
        void ExitArrow(Vector2 sideMid, Vector2 dir, Color color, float scale = 1f)
        {
            var tip = sideMid + dir * 8f * scale;
            var baseMid = sideMid + dir * 2.5f * scale;
            var across = new Vector2(-dir.y, dir.x) * 4f * scale;
            ink.Triangle(tip, baseMid + across, baseMid - across, color);
            if (scale > 1f)
                ink.Polyline(new[] { tip, baseMid + across, baseMid - across }, inkColor, 1f, true);
        }

        // --- Dessin : légende ---------------------------------------------------

        void DrawLegend(Rect area, int columns)
        {
            Engraving.Rule(ink, area.position, area.width, inkColor);
            texts.Label(Loc.T("legende.titre").ToUpperInvariant(), area.x, area.y + 8f, area.width, 14, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            const float sw = 14f;
            string[] keys = { "mur", "sol", "salle", "sortie", "torche", "depart", "joueur", "danger" };
            string[] names = { "legende.roche", "legende.sol", "legende.mur_salle", "legende.sortie", "legende.torche", "legende.depart", "legende.vous", "legende.danger" };
            var colW = area.width / columns;
            for (var i = 0; i < keys.Length; i++)
            {
                var pos = area.position + new Vector2(i % columns * colW, 34f + i / columns * 24f);
                var box = new Rect(pos, new Vector2(sw, sw));
                switch (keys[i])
                {
                    case "mur":
                        ink.Rect(box, paperColor);
                        ink.Hatch(box, 4f, hatchColor);
                        ink.RectOutline(box, inkColor, 1.5f);
                        break;
                    case "sol":
                        ink.Rect(box, paperColor);
                        ink.RectOutline(box, inkColor, 1.5f);
                        break;
                    case "salle":
                        ink.Rect(box, paperColor);
                        ink.Line(new Vector2(box.xMin, box.center.y), new Vector2(box.xMax, box.center.y), inkColor, 2.5f);
                        break;
                    case "sortie":
                        ExitArrow(new Vector2(box.center.x, box.yMax - 1f), new Vector2(0f, -1f), inkColor, 1.3f);
                        break;
                    case "torche":
                        Engraving.Flame(ink, box.center, 7f, inkColor, paperColor);
                        break;
                    case "depart":
                        Engraving.Star(ink, box.center, 7f, inkColor);
                        break;
                    case "danger":
                        Engraving.Skull(ink, box.center, 13f, true, inkColor, paperColor);
                        break;
                    case "joueur":
                        var c = box.center;
                        ink.Triangle(c + new Vector2(0, -7), c + new Vector2(6, 6), c + new Vector2(0, 2), playerColor);
                        ink.Triangle(c + new Vector2(0, -7), c + new Vector2(0, 2), c + new Vector2(-6, 6), playerColor);
                        break;
                }
                texts.Label(Loc.T(names[i]), pos.x + sw + 8f, pos.y - 2f, colW - sw - 8f, 14, inkColor);
            }
        }

        // --- Dessin : plans de tuiles -------------------------------------------

        /// <summary>Sol d'une tuile posée : les cases de sol en papier, torche et départ. Les murs restent la roche hachurée.</summary>
        void DrawFloor(Vector2 topLeft, float cell, string[] rows, Color color)
        {
            for (var dy = 0; dy < rows.Length; dy++)
                for (var dx = 0; dx < rows[dy].Length; dx++)
                {
                    var ch = rows[dy][dx];
                    if (ch == '#' || ch == ' ')
                        continue;
                    var r = new Rect(topLeft + new Vector2(dx, dy) * cell, Vector2.one * cell);
                    ink.Rect(r, paperColor);
                    if (ch == 'T')
                        Engraving.Flame(ink, r.center, cell * 0.32f, color, paperColor);
                    else if (ch == 'P')
                        Engraving.Star(ink, r.center, cell * 0.36f, color);
                }
        }

        /// <summary>Plan (lignes de caractères) : murs hachurés, sol papier, torche (flamme), départ (étoile).</summary>
        void DrawPlan(Vector2 topLeft, float cell, string[] rows, Color color, float spacing)
        {
            for (var dy = 0; dy < rows.Length; dy++)
                for (var dx = 0; dx < rows[dy].Length; dx++)
                {
                    var ch = rows[dy][dx];
                    if (ch == ' ')
                        continue; // hors de la tuile (creux d'une salle en L, en T…)
                    var r = new Rect(topLeft + new Vector2(dx, dy) * cell, Vector2.one * cell);
                    ink.Rect(r, paperColor);
                    if (ch == '#')
                        ink.Hatch(r, spacing, color);
                    else if (ch == 'T')
                        Engraving.Flame(ink, r.center, cell * 0.32f, color, paperColor);
                    else if (ch == 'P')
                        Engraving.Star(ink, r.center, cell * 0.36f, color);
                }
        }

        /// <summary>
        /// Contours à l'encre entre sol et roche. `outsideIsWall` : faux pour une tuile seule, dont
        /// les passages débouchent à l'extérieur (pas de trait au bout).
        /// </summary>
        void DrawEdges(Vector2 topLeft, float cell, string[] rows, bool outsideIsWall, Color color, float width)
        {
            for (var dy = 0; dy < rows.Length; dy++)
                for (var dx = 0; dx < rows[dy].Length; dx++)
                {
                    var ch = rows[dy][dx];
                    if (ch == '#' || ch == ' ')
                        continue;
                    for (var i = 0; i < 4; i++)
                    {
                        var n = new Vector2Int(dx, dy) + Vector2Int.RoundToInt(DirVectors[i]);
                        var wall = outsideIsWall;
                        if (n.y >= 0 && n.y < rows.Length && n.x >= 0 && n.x < rows[n.y].Length)
                        {
                            var nc = rows[n.y][n.x];
                            wall = nc == '#' || (nc == ' ' && outsideIsWall);
                        }
                        if (wall)
                            EdgeLine(topLeft, cell, new Vector2Int(dx, dy), i, color, width);
                    }
                }
        }

        /// <summary>Murs fins d'une carte de salle, sur son plan.</summary>
        void CardEdgeWalls(TileCard card, Vector2 topLeft, float cell, Color color, float width)
        {
            var b = card.Bounds();
            foreach (var slot in card.Slots)
                foreach (var (c, dir) in card.EdgeWalls(slot))
                    EdgeLine(topLeft, cell, (slot - b.position) * 3 + c, System.Array.IndexOf(TileCard.Dirs, dir), color, width);
        }

        /// <summary>Trait sur le bord `dir` (0 nord, 1 est, 2 sud, 3 ouest) de la case `c`.</summary>
        void EdgeLine(Vector2 topLeft, float cell, Vector2Int c, int dir, Color color, float width)
        {
            var a = topLeft + (Vector2)c * cell;
            switch (dir)
            {
                case 0: ink.Line(a, a + new Vector2(cell, 0f), color, width, true); break;
                case 1: ink.Line(a + new Vector2(cell, 0f), a + new Vector2(cell, cell), color, width, true); break;
                case 2: ink.Line(a + new Vector2(0f, cell), a + new Vector2(cell, cell), color, width, true); break;
                case 3: ink.Line(a, a + new Vector2(0f, cell), color, width, true); break;
            }
        }

        static (Vector2, Vector2)[] RectSides(Rect r) => new[]
        {
            (new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin)),
            (new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax)),
            (new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax)),
            (new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin)),
        };

        static Rect Shrink(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);
        static Vector2 Floor(Vector2 v) => new Vector2(Mathf.Floor(v.x), Mathf.Floor(v.y));
        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
