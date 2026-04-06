using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using Board.Input;

namespace BoardOfEducation.Input
{
    /// <summary>
    /// Wraps Board SDK piece detection. Polls BoardInput.GetActiveContacts()
    /// each frame and fires events for placement/movement/removal.
    /// Uses the Board Simulator in the Editor — open Board > Input > Simulator,
    /// select a piece, then click in the Game view to place it.
    /// </summary>
    public class PieceManager : MonoBehaviour
    {
        public static PieceManager Instance { get; private set; }

        /// <summary>
        /// Lightweight representation of a piece contact.
        /// </summary>
        [Serializable]
        public struct PieceContact
        {
            public int contactId;
            public int glyphId;
            public Vector2 screenPosition;
            public float orientation; // radians, counter-clockwise from vertical
            public bool isTouched;
            public ContactPhase phase;

            public enum ContactPhase { Began, Moved, Stationary, Ended }
        }

        // Maps of currently active pieces by contactId
        private Dictionary<int, PieceContact> activePieces = new Dictionary<int, PieceContact>();

        public IReadOnlyDictionary<int, PieceContact> ActivePieces => activePieces;

        // Events
        public event Action<PieceContact> OnPiecePlaced;
        public event Action<PieceContact> OnPieceMoved;
        public event Action<PieceContact> OnPieceRemoved;
        public event Action<PieceContact> OnPieceTouched;
        public event Action<PieceContact> OnPieceReleased;

        // Board Arcade piece set glyph IDs (from Board Simulator)
        // Each piece represents a fraction value in our math game
        public static class ArcadeGlyphs
        {
            public const int RobotYellow = 0;  // Represents 1/2
            public const int RobotPurple = 1;  // Represents 1/4
            public const int RobotOrange = 2;  // Represents 1/3
            public const int RobotPink = 3;    // Represents 3/4
            public const int ShipPink = 4;     // Represents 1 (whole)
            public const int ShipYellow = 5;   // Represents 2/4
            public const int ShipPurple = 6;   // Represents 2/3
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[PieceManager] Using Board SDK input. Open Board > Input > Simulator to place pieces.");
        }

        private void Update()
        {
            PollBoardInput();
        }

        private void PollBoardInput()
        {
            var contacts = BoardInput.GetActiveContacts(BoardContactType.Glyph);
            var seenIds = new HashSet<int>();

            foreach (var contact in contacts)
            {
                seenIds.Add(contact.contactId);

                var piece = new PieceContact
                {
                    contactId = contact.contactId,
                    glyphId = contact.glyphId,
                    screenPosition = contact.screenPosition,
                    orientation = contact.orientation,
                    isTouched = contact.isTouched,
                };

                if (contact.phase == BoardContactPhase.Began)
                {
                    piece.phase = PieceContact.ContactPhase.Began;
                    activePieces[contact.contactId] = piece;
                    OnPiecePlaced?.Invoke(piece);
                }
                else if (contact.phase == BoardContactPhase.Moved)
                {
                    piece.phase = PieceContact.ContactPhase.Moved;

                    // Detect touch state changes
                    if (activePieces.TryGetValue(contact.contactId, out var prev))
                    {
                        if (!prev.isTouched && piece.isTouched)
                            OnPieceTouched?.Invoke(piece);
                        else if (prev.isTouched && !piece.isTouched)
                            OnPieceReleased?.Invoke(piece);
                    }

                    activePieces[contact.contactId] = piece;
                    OnPieceMoved?.Invoke(piece);
                }
                else if (contact.phase == BoardContactPhase.Stationary)
                {
                    piece.phase = PieceContact.ContactPhase.Stationary;

                    // Still detect touch state changes on stationary pieces
                    if (activePieces.TryGetValue(contact.contactId, out var prev))
                    {
                        if (!prev.isTouched && piece.isTouched)
                            OnPieceTouched?.Invoke(piece);
                        else if (prev.isTouched && !piece.isTouched)
                            OnPieceReleased?.Invoke(piece);
                    }

                    activePieces[contact.contactId] = piece;
                }
                else if (contact.phase == BoardContactPhase.Ended || contact.phase == BoardContactPhase.Canceled)
                {
                    piece.phase = PieceContact.ContactPhase.Ended;
                    activePieces.Remove(contact.contactId);
                    OnPieceRemoved?.Invoke(piece);
                }
            }

            // Clean up pieces that disappeared without an Ended event
            var toRemove = new List<int>();
            foreach (var kvp in activePieces)
            {
                if (!seenIds.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove)
            {
                var piece = activePieces[id];
                piece.phase = PieceContact.ContactPhase.Ended;
                activePieces.Remove(id);
                OnPieceRemoved?.Invoke(piece);
            }
        }

        /// <summary>
        /// Get a human-readable name for a Board Arcade glyph ID.
        /// </summary>
        public static string GetPieceName(int glyphId)
        {
            return glyphId switch
            {
                ArcadeGlyphs.RobotYellow => "Robot Yellow (1/2)",
                ArcadeGlyphs.RobotPurple => "Robot Purple (1/4)",
                ArcadeGlyphs.RobotOrange => "Robot Orange (1/3)",
                ArcadeGlyphs.RobotPink => "Robot Pink (3/4)",
                ArcadeGlyphs.ShipPink => "Ship Pink (1)",
                ArcadeGlyphs.ShipYellow => "Ship Yellow (2/4)",
                ArcadeGlyphs.ShipPurple => "Ship Purple (2/3)",
                _ => $"Piece {glyphId}"
            };
        }
    }
}
