using System;
using UnityEngine;

namespace LuminaRush
{
    public enum RewardedPlacement
    {
        Revive = 0,
        DoubleCoins = 1,
        Bonus = 2,
        Mission = 3
    }

    [Serializable]
    public class AdConfigFile
    {
        public bool useTestAds = true;
        public string androidAppIdTest = "ca-app-pub-3940256099942544~3347511713";
        public string androidAppIdProduction = "";
        public string iosAppIdTest = "ca-app-pub-3940256099942544~1458002511";
        public string iosAppIdProduction = "";
        public string rewardedReviveTest = "ca-app-pub-3940256099942544/5224354917";
        public string rewardedReviveProduction = "";
        public string rewardedDoubleCoinsTest = "ca-app-pub-3940256099942544/5224354917";
        public string rewardedDoubleCoinsProduction = "";
        public string rewardedBonusTest = "ca-app-pub-3940256099942544/5224354917";
        public string rewardedBonusProduction = "";
        public string rewardedMissionTest = "ca-app-pub-3940256099942544/5224354917";
        public string rewardedMissionProduction = "";
        public string interstitialTest = "ca-app-pub-3940256099942544/1033173712";
        public string interstitialProduction = "";
        public string iosRewardedTest = "ca-app-pub-3940256099942544/1712485313";
        public string iosInterstitialTest = "ca-app-pub-3940256099942544/4411468910";
        public int interstitialMinRuns = 3;
        public float interstitialMinSeconds = 90f;
        public float interstitialAfterRewardedSeconds = 45f;
        public int bonusAdDailyCap = 5;
        public int bonusAdCoinReward = 25;
        public string privacyPolicyUrl = "";
        public string umpDebugGeography = "Disabled";
        public string testDeviceHashedIds = "";
    }

    public static class AdConfig
    {
        public static AdConfigFile Current { get; private set; }

        public static AdConfigFile Load()
        {
            AdConfigFile file = new AdConfigFile();
            TextAsset asset = Resources.Load<TextAsset>("AdUnitIds");
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                AdConfigFile parsed = JsonUtility.FromJson<AdConfigFile>(asset.text);
                if (parsed != null)
                    file = parsed;
            }
            if (string.IsNullOrEmpty(file.androidAppIdTest))
                file.androidAppIdTest = "ca-app-pub-3940256099942544~3347511713";
            if (string.IsNullOrEmpty(file.rewardedReviveTest))
                file.rewardedReviveTest = "ca-app-pub-3940256099942544/5224354917";
            if (string.IsNullOrEmpty(file.interstitialTest))
                file.interstitialTest = "ca-app-pub-3940256099942544/1033173712";
            if (file.interstitialMinRuns < 1)
                file.interstitialMinRuns = 3;
            if (file.interstitialMinSeconds < 30f)
                file.interstitialMinSeconds = 90f;
            if (file.bonusAdDailyCap < 0)
                file.bonusAdDailyCap = 5;
            if (file.bonusAdCoinReward <= 0)
                file.bonusAdCoinReward = 25;
            Current = file;
            return file;
        }

        public static string AndroidAppId(AdConfigFile file)
        {
            return file.useTestAds || string.IsNullOrEmpty(file.androidAppIdProduction)
                ? file.androidAppIdTest
                : file.androidAppIdProduction;
        }

        public static string IosAppId(AdConfigFile file)
        {
            return file.useTestAds || string.IsNullOrEmpty(file.iosAppIdProduction)
                ? file.iosAppIdTest
                : file.iosAppIdProduction;
        }

        public static string Unit(AdConfigFile file, RewardedPlacement placement)
        {
            bool ios = Application.platform == RuntimePlatform.IPhonePlayer;
            if (file.useTestAds && ios && !string.IsNullOrEmpty(file.iosRewardedTest))
                return file.iosRewardedTest;
            switch (placement)
            {
                case RewardedPlacement.Revive:
                    return Pick(file.useTestAds, file.rewardedReviveTest, file.rewardedReviveProduction);
                case RewardedPlacement.DoubleCoins:
                    return Pick(file.useTestAds, file.rewardedDoubleCoinsTest, file.rewardedDoubleCoinsProduction);
                case RewardedPlacement.Mission:
                    string mission = Pick(file.useTestAds, file.rewardedMissionTest, file.rewardedMissionProduction);
                    if (!string.IsNullOrEmpty(mission))
                        return mission;
                    return Pick(file.useTestAds, file.rewardedBonusTest, file.rewardedBonusProduction);
                default:
                    return Pick(file.useTestAds, file.rewardedBonusTest, file.rewardedBonusProduction);
            }
        }

        public static string InterstitialUnit(AdConfigFile file)
        {
            bool ios = Application.platform == RuntimePlatform.IPhonePlayer;
            if (file.useTestAds && ios && !string.IsNullOrEmpty(file.iosInterstitialTest))
                return file.iosInterstitialTest;
            return Pick(file.useTestAds, file.interstitialTest, file.interstitialProduction);
        }

        static string Pick(bool test, string testId, string productionId)
        {
            if (test)
                return testId ?? "";
            return productionId ?? "";
        }
    }
}
