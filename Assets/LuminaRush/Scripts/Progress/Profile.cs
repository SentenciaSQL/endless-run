using System;
using System.IO;
using UnityEngine;

namespace LuminaRush
{
    [Serializable]
    public class Profile
    {
        public int version = 1;
        public int coins;
        public int bestScore;
        public float bestDistance;
        public int runs;
        public int lifetimeCoins;
        public float lifetimeDistance;
        public int jumps;
        public int slides;
        public int characterIndex;
        public int skinIndex;
        public int[] ownedCharacters = { 1, 0, 0, 0 };
        public int[] ownedSkins = { 1, 0, 0, 0, 0 };
        public int shieldCharges;
        public int magnetCharges;
        public int boostCharges;
        public int shopPurchases;
        public int revivesUsed;
        public string missionDay = "";
        public int[] missionKind = new int[3];
        public int[] missionTarget = new int[3];
        public int[] missionProgress = new int[3];
        public int[] missionClaimed = new int[3];
        public int[] missionBonusClaimed = new int[3];
        public int[] achievementUnlocked = new int[8];
        public int[] achievementClaimed = new int[8];
        public float music = 0.55f;
        public float sfx = 0.85f;
        public int vibration = 1;
        public int sessions;
        public string firstDay = "";
        public string lastDay = "";
        public int bonusAdsToday;
        public string bonusAdDay = "";
        public long lastInterstitialUnix;
        public int runsSinceInterstitial;
        public int tutorialSeen;
        public int rewardedCompleted;
        public int rewardedFailed;
        public int interstitialsShown;
    }

    public static class ProfileStore
    {
        const string FileName = "lumina_rush_profile.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static Profile Load()
        {
            try
            {
                if (!File.Exists(Path))
                    return new Profile();
                string json = File.ReadAllText(Path);
                Profile profile = JsonUtility.FromJson<Profile>(json);
                if (profile == null)
                    return new Profile();
                Repair(profile);
                return profile;
            }
            catch (Exception error)
            {
                Debug.LogWarning("No se pudo leer el guardado de Lumina Rush. Se crea uno nuevo. " + error.Message);
                return new Profile();
            }
        }

        public static void Save(Profile profile)
        {
            if (profile == null)
                return;
            try
            {
                Repair(profile);
                string json = JsonUtility.ToJson(profile, true);
                string directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                string temp = Path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(Path))
                    File.Delete(Path);
                File.Move(temp, Path);
            }
            catch (Exception error)
            {
                Debug.LogWarning("No se pudo guardar el progreso. " + error.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(Path))
                    File.Delete(Path);
                string temp = Path + ".tmp";
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (Exception error)
            {
                Debug.LogWarning("No se pudieron borrar los datos locales. " + error.Message);
            }
        }

        public static void Repair(Profile profile)
        {
            if (profile.ownedCharacters == null || profile.ownedCharacters.Length != Catalog.Characters.Length)
            {
                var owned = new int[Catalog.Characters.Length];
                owned[0] = 1;
                if (profile.ownedCharacters != null)
                {
                    int n = Mathf.Min(owned.Length, profile.ownedCharacters.Length);
                    for (int i = 0; i < n; i++)
                        owned[i] = profile.ownedCharacters[i];
                }
                profile.ownedCharacters = owned;
            }
            if (profile.ownedSkins == null || profile.ownedSkins.Length != Catalog.Skins.Length)
            {
                var owned = new int[Catalog.Skins.Length];
                owned[0] = 1;
                if (profile.ownedSkins != null)
                {
                    int n = Mathf.Min(owned.Length, profile.ownedSkins.Length);
                    for (int i = 0; i < n; i++)
                        owned[i] = profile.ownedSkins[i];
                }
                profile.ownedSkins = owned;
            }
            profile.ownedCharacters[0] = 1;
            profile.ownedSkins[0] = 1;
            profile.characterIndex = Mathf.Clamp(profile.characterIndex, 0, Catalog.Characters.Length - 1);
            profile.skinIndex = Mathf.Clamp(profile.skinIndex, 0, Catalog.Skins.Length - 1);
            if (profile.ownedCharacters[profile.characterIndex] == 0)
                profile.characterIndex = 0;
            if (profile.ownedSkins[profile.skinIndex] == 0)
                profile.skinIndex = 0;
            EnsureLength(ref profile.missionKind, 3);
            EnsureLength(ref profile.missionTarget, 3);
            EnsureLength(ref profile.missionProgress, 3);
            EnsureLength(ref profile.missionClaimed, 3);
            EnsureLength(ref profile.missionBonusClaimed, 3);
            EnsureLength(ref profile.achievementUnlocked, Catalog.Achievements.Length);
            EnsureLength(ref profile.achievementClaimed, Catalog.Achievements.Length);
            profile.music = Mathf.Clamp01(profile.music);
            profile.sfx = Mathf.Clamp01(profile.sfx);
            profile.coins = Mathf.Max(0, profile.coins);
        }

        static void EnsureLength(ref int[] array, int length)
        {
            if (array != null && array.Length == length)
                return;
            var next = new int[length];
            if (array != null)
            {
                int n = Mathf.Min(length, array.Length);
                for (int i = 0; i < n; i++)
                    next[i] = array[i];
            }
            array = next;
        }
    }
}
