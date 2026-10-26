using System.Globalization;
using UnityEngine;

namespace NumberMerge.Save
{
    // Persists the player's record and the board in progress.
    //
    // Uses PlayerPrefs, which works on Windows, Android and WebGL (File IO is not available on WebGL).
    // The record and the board use separate keys, so they are independent of each other.
    public static class SaveSystem
    {
        private const string RecordKey = "NumberMerge.Record";
        private const string BoardKey = "NumberMerge.Board";

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
            PlayerPrefs.Save();
        }
    }
}
