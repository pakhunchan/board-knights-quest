using UnityEngine;
using System;
using System.IO;
using System.Text;
using BoardOfEducation.Input;

namespace BoardOfEducation.Logging
{
    /// <summary>
    /// Logs every piece interaction to a CSV file for the "Cognitive Genome" deliverable.
    /// Captures timestamp, glyphId, position, rotation, action, and game context.
    /// </summary>
    public class InteractionLogger : MonoBehaviour
    {
        public static InteractionLogger Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private string logFileName = "interaction_log";

        private StreamWriter writer;
        private string filePath;
        private int eventCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (!enableLogging) return;

            // Use persistentDataPath for Android compatibility
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            filePath = Path.Combine(Application.persistentDataPath, $"{logFileName}_{timestamp}.csv");

            writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine("event_id,timestamp,elapsed_seconds,action,glyph_id,piece_name," +
                             "screen_x,screen_y,orientation_rad,orientation_deg,is_touched," +
                             "scaffold,problem_index,team_score");
            writer.Flush();

            Debug.Log($"[InteractionLogger] Logging to: {filePath}");

            // Subscribe to PieceManager events
            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced += c => LogInteraction("placed", c);
                PieceManager.Instance.OnPieceMoved += c => LogInteraction("moved", c);
                PieceManager.Instance.OnPieceRemoved += c => LogInteraction("removed", c);
                PieceManager.Instance.OnPieceTouched += c => LogInteraction("touched", c);
                PieceManager.Instance.OnPieceReleased += c => LogInteraction("released", c);
            }
            else
            {
                Debug.LogWarning("[InteractionLogger] PieceManager not found — will retry on first event.");
            }
        }

        private void LogInteraction(string action, PieceManager.PieceContact contact)
        {
            if (!enableLogging || writer == null) return;

            eventCount++;
            var gm = Core.GameManager.Instance;
            string scaffold = gm != null ? gm.CurrentScaffold.ToString() : "N/A";
            int problemIndex = gm != null ? gm.CurrentProblemIndex : -1;
            int teamScore = gm != null ? gm.TeamScore : 0;

            float orientDeg = contact.orientation * Mathf.Rad2Deg;
            string pieceName = PieceManager.GetPieceName(contact.glyphId);

            string line = string.Join(",",
                eventCount,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Time.realtimeSinceStartup.ToString("F3"),
                action,
                contact.glyphId,
                $"\"{pieceName}\"",
                contact.screenPosition.x.ToString("F1"),
                contact.screenPosition.y.ToString("F1"),
                contact.orientation.ToString("F4"),
                orientDeg.ToString("F1"),
                contact.isTouched ? "true" : "false",
                scaffold,
                problemIndex,
                teamScore
            );

            writer.WriteLine(line);
            writer.Flush();
        }

        /// <summary>
        /// Log a custom game event (e.g., answer submitted, scaffold changed).
        /// </summary>
        public void LogGameEvent(string action, string details = "")
        {
            if (!enableLogging || writer == null) return;

            eventCount++;
            var gm = Core.GameManager.Instance;
            string scaffold = gm != null ? gm.CurrentScaffold.ToString() : "N/A";
            int problemIndex = gm != null ? gm.CurrentProblemIndex : -1;
            int teamScore = gm != null ? gm.TeamScore : 0;

            string line = string.Join(",",
                eventCount,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Time.realtimeSinceStartup.ToString("F3"),
                action,
                -1,
                $"\"{details}\"",
                0, 0, 0, 0,
                "false",
                scaffold,
                problemIndex,
                teamScore
            );

            writer.WriteLine(line);
            writer.Flush();
        }

        public string GetLogFilePath() => filePath;
        public int GetEventCount() => eventCount;

        private void OnDestroy()
        {
            writer?.Flush();
            writer?.Close();
            writer?.Dispose();
        }

        private void OnApplicationQuit()
        {
            LogGameEvent("session_end", "Application quit");
            writer?.Flush();
            writer?.Close();
            writer?.Dispose();
        }
    }
}
