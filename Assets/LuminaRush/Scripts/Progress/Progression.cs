using System;
using UnityEngine;

namespace LuminaRush
{
    public struct RunResult
    {
        public float Distance;
        public int Coins;
        public int Score;
        public bool Revived;
    }

    public static class Progression
    {
        static readonly int[] DistanceTargets = { 300, 800, 1500 };
        static readonly int[] CoinTargets = { 20, 50, 90 };
        static readonly int[] JumpTargets = { 12, 30, 55 };
        static readonly int[] RunTargets = { 2, 3, 5 };
        static readonly int[] SlideTargets = { 8, 20, 36 };
        static readonly int[] MissionRewards = { 15, 30, 50 };

        public static void TouchDay(Profile profile)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrEmpty(profile.firstDay))
                profile.firstDay = today;
            if (profile.bonusAdDay != today)
            {
                profile.bonusAdDay = today;
                profile.bonusAdsToday = 0;
            }
            if (profile.missionDay == today && profile.missionKind != null && profile.missionKind.Length == 3 && profile.missionTarget[0] > 0)
                return;

            profile.missionDay = today;
            profile.missionKind = new int[3];
            profile.missionTarget = new int[3];
            profile.missionProgress = new int[3];
            profile.missionClaimed = new int[3];
            profile.missionBonusClaimed = new int[3];

            int seed = today.GetHashCode() & 0x7fffffff;
            var kinds = new[] { 0, 1, 2, 3, 4 };
            for (int i = 0; i < kinds.Length; i++)
            {
                int swap = (seed + i * 17) % kinds.Length;
                int tmp = kinds[i];
                kinds[i] = kinds[swap];
                kinds[swap] = tmp;
            }
            for (int i = 0; i < 3; i++)
            {
                int kind = kinds[i];
                int rank = (seed + i * 3) % 3;
                profile.missionKind[i] = kind;
                profile.missionTarget[i] = TargetFor(kind, rank);
            }
        }

        public static void AddMission(Profile profile, MissionKind kind, int amount)
        {
            if (amount <= 0 || profile.missionKind == null)
                return;
            TouchDay(profile);
            for (int i = 0; i < profile.missionKind.Length; i++)
            {
                if (profile.missionKind[i] != (int)kind)
                    continue;
                profile.missionProgress[i] = Mathf.Min(profile.missionTarget[i], profile.missionProgress[i] + amount);
            }
        }

        public static bool ClaimMission(Profile profile, int index, out int reward)
        {
            reward = 0;
            TouchDay(profile);
            if (index < 0 || index >= 3)
                return false;
            if (profile.missionClaimed[index] != 0)
                return false;
            if (profile.missionProgress[index] < profile.missionTarget[index])
                return false;
            reward = MissionRewards[index];
            profile.missionClaimed[index] = 1;
            GrantCoins(profile, reward);
            return true;
        }

        public static bool ClaimMissionBonus(Profile profile, int index, out int reward)
        {
            reward = 0;
            TouchDay(profile);
            if (index < 0 || index >= 3)
                return false;
            if (profile.missionClaimed[index] == 0 || profile.missionBonusClaimed[index] != 0)
                return false;
            reward = MissionRewards[index];
            profile.missionBonusClaimed[index] = 1;
            GrantCoins(profile, reward);
            return true;
        }

        public static void EvaluateAchievements(Profile profile, RunResult run)
        {
            Unlock(profile, 0, profile.runs >= 1);
            Unlock(profile, 1, run.Distance >= 400f || profile.bestDistance >= 400f);
            Unlock(profile, 2, run.Distance >= 1500f || profile.bestDistance >= 1500f);
            Unlock(profile, 3, run.Coins >= 40);
            Unlock(profile, 4, profile.lifetimeCoins >= 500);
            Unlock(profile, 5, profile.runs >= 10);
            Unlock(profile, 6, profile.revivesUsed >= 1);
            Unlock(profile, 7, profile.shopPurchases >= 1);
        }

        public static bool ClaimAchievement(Profile profile, int index, out int reward)
        {
            reward = 0;
            if (index < 0 || index >= Catalog.Achievements.Length)
                return false;
            if (profile.achievementUnlocked[index] == 0 || profile.achievementClaimed[index] != 0)
                return false;
            reward = Catalog.Achievements[index].Reward;
            profile.achievementClaimed[index] = 1;
            GrantCoins(profile, reward);
            return true;
        }

        public static bool TryBuyCharacter(Profile profile, int index, out string error)
        {
            error = null;
            if (!Valid(index, Catalog.Characters.Length))
            {
                error = "Personaje no válido.";
                return false;
            }
            if (profile.ownedCharacters[index] != 0)
            {
                profile.characterIndex = index;
                return true;
            }
            int price = Catalog.Characters[index].Price;
            if (profile.coins < price)
            {
                error = "No tienes monedas suficientes.";
                return false;
            }
            profile.coins -= price;
            profile.ownedCharacters[index] = 1;
            profile.characterIndex = index;
            profile.shopPurchases++;
            return true;
        }

        public static bool TrySelectCharacter(Profile profile, int index, out string error)
        {
            error = null;
            if (!Valid(index, Catalog.Characters.Length) || profile.ownedCharacters[index] == 0)
            {
                error = "Todavía no tienes este personaje.";
                return false;
            }
            profile.characterIndex = index;
            return true;
        }

        public static bool TryBuySkin(Profile profile, int index, out string error)
        {
            error = null;
            if (!Valid(index, Catalog.Skins.Length))
            {
                error = "Aspecto no válido.";
                return false;
            }
            if (profile.ownedSkins[index] != 0)
            {
                profile.skinIndex = index;
                return true;
            }
            int price = Catalog.Skins[index].Price;
            if (profile.coins < price)
            {
                error = "No tienes monedas suficientes.";
                return false;
            }
            profile.coins -= price;
            profile.ownedSkins[index] = 1;
            profile.skinIndex = index;
            profile.shopPurchases++;
            return true;
        }

        public static bool TrySelectSkin(Profile profile, int index, out string error)
        {
            error = null;
            if (!Valid(index, Catalog.Skins.Length) || profile.ownedSkins[index] == 0)
            {
                error = "Todavía no tienes este aspecto.";
                return false;
            }
            profile.skinIndex = index;
            return true;
        }

        public static bool TryBuyBoost(Profile profile, PowerKind kind, out string error)
        {
            error = null;
            int index = (int)kind;
            if (!Valid(index, Catalog.Boosts.Length))
            {
                error = "Potenciador no válido.";
                return false;
            }
            int price = Catalog.Boosts[index].Price;
            if (profile.coins < price)
            {
                error = "No tienes monedas suficientes.";
                return false;
            }
            profile.coins -= price;
            AddCharge(profile, kind, 1);
            profile.shopPurchases++;
            return true;
        }

        public static bool TryConsumeCharge(Profile profile, PowerKind kind)
        {
            switch (kind)
            {
                case PowerKind.Shield:
                    if (profile.shieldCharges <= 0) return false;
                    profile.shieldCharges--;
                    return true;
                case PowerKind.Magnet:
                    if (profile.magnetCharges <= 0) return false;
                    profile.magnetCharges--;
                    return true;
                default:
                    if (profile.boostCharges <= 0) return false;
                    profile.boostCharges--;
                    return true;
            }
        }

        public static int Charges(Profile profile, PowerKind kind)
        {
            switch (kind)
            {
                case PowerKind.Shield: return profile.shieldCharges;
                case PowerKind.Magnet: return profile.magnetCharges;
                default: return profile.boostCharges;
            }
        }

        public static void GrantCoins(Profile profile, int amount)
        {
            if (amount <= 0)
                return;
            profile.coins += amount;
            profile.lifetimeCoins += amount;
        }

        public static string MissionTitle(int kind)
        {
            switch ((MissionKind)kind)
            {
                case MissionKind.Distance: return "Distancia";
                case MissionKind.Coins: return "Monedas";
                case MissionKind.Jumps: return "Saltos";
                case MissionKind.Runs: return "Carreras";
                default: return "Deslizamientos";
            }
        }

        public static string MissionDetail(Profile profile, int index)
        {
            int kind = profile.missionKind[index];
            int target = profile.missionTarget[index];
            switch ((MissionKind)kind)
            {
                case MissionKind.Distance: return "Recorre " + target + " m hoy";
                case MissionKind.Coins: return "Recoge " + target + " monedas hoy";
                case MissionKind.Jumps: return "Salta " + target + " veces hoy";
                case MissionKind.Runs: return "Juega " + target + " carreras hoy";
                default: return "Agáchate " + target + " veces hoy";
            }
        }

        static void AddCharge(Profile profile, PowerKind kind, int amount)
        {
            switch (kind)
            {
                case PowerKind.Shield: profile.shieldCharges += amount; break;
                case PowerKind.Magnet: profile.magnetCharges += amount; break;
                default: profile.boostCharges += amount; break;
            }
        }

        static void Unlock(Profile profile, int index, bool condition)
        {
            if (condition)
                profile.achievementUnlocked[index] = 1;
        }

        static int TargetFor(int kind, int rank)
        {
            rank = Mathf.Clamp(rank, 0, 2);
            switch ((MissionKind)kind)
            {
                case MissionKind.Distance: return DistanceTargets[rank];
                case MissionKind.Coins: return CoinTargets[rank];
                case MissionKind.Jumps: return JumpTargets[rank];
                case MissionKind.Runs: return RunTargets[rank];
                default: return SlideTargets[rank];
            }
        }

        static bool Valid(int index, int length)
        {
            return index >= 0 && index < length;
        }
    }
}
