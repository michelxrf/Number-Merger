using System.Globalization;
using UnityEngine;

namespace NumberMerge.Save
{
    // Persists the player's record, the revives left, and the board in progress.
    //
    // Uses PlayerPrefs, which works on Windows, Android and WebGL (File IO is not available on WebGL).
    // Each value uses its own key, so they are independent of each other.
    public static class SaveSystem
    {
        private const string RecordKey = "NumberMerge.Record";
        private const string BoardKey = "NumberMerge.Board";
        private const string RevivesKey = "NumberMerge.Revives";
        private const string VolumeKeyPrefix = "NumberMerge.Volume.";

        // Revives a player starts a new game with (and has on a fresh install).
        public const int DefaultRevives = 1;

        // Audio volume (0..1) per channel. Returns false when the player has never changed it.
        public static bool TryLoadVolume(string channelName, out float volume)
        {
            string key = VolumeKeyPrefix + channelName;

            if (!PlayerPrefs.HasKey(key))
            {
                volume = 1f;
                return false;
            }

            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));
            return true;
        }

        public static void SaveVolume(string channelName, float volume)
        {
            PlayerPrefs.SetFloat(VolumeKeyPrefix + channelName, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        public static long LoadRecord()
        {
            string raw = PlayerPrefs.GetString(RecordKey, string.Empty);

            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long record) || record < 0)
            {
                return 0;
            }

            return record;
        }

        public static void SaveRecord(long record)
        {
            if (record < 0)
            {
                record = 0;
            }

            PlayerPrefs.SetString(RecordKey, record.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        // Revives left. A missing key (fresh install) returns DefaultRevives.
        public static int LoadRevives()
        {
            if (!PlayerPrefs.HasKey(RevivesKey))
            {
                return DefaultRevives;
            }

            return Mathf.Max(0, PlayerPrefs.GetInt(RevivesKey, DefaultRevives));
        }

        public static void SaveRevives(int revives)
        {
            PlayerPrefs.SetInt(RevivesKey, Mathf.Max(0, revives));
            PlayerPrefs.Save();
        }

        // Returns null when there is no saved board.
        public static BoardState LoadBoardState()
        {
            string json = PlayerPrefs.GetString(BoardKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<BoardState>(json);
            }
            catch (System.ArgumentException)
            {
                // Corrupted data is treated as no saved board.
                return null;
            }
        }

        public static void SaveBoardState(BoardState state)
        {
            if (state == null)
            {
                DeleteBoardState();
                return;
            }

            PlayerPrefs.SetString(BoardKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        // Removes the saved board, so the next load returns null. Used on game over.
        public static void DeleteBoardState()
        {
            if (!PlayerPrefs.HasKey(BoardKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(BoardKey);
            PlayerPrefs.Save();
        }

        // Testing only: deletes all player save data (record and board in progress).
        public static void DeleteAllPlayerDataForTesting()
        {
            PlayerPrefs.DeleteKey(RecordKey);
            PlayerPrefs.DeleteKey(BoardKey);
            PlayerPrefs.DeleteKey(RevivesKey);
            PlayerPrefs.Save();
        }
    }
}
