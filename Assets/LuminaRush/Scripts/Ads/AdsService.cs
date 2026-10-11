using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

namespace LuminaRush
{
    public sealed class AdsService
    {
        public event Action<bool> FullScreenChanged;
        public event Action ConsentCompleted;

        readonly Dictionary<RewardedPlacement, RewardedSlot> rewarded = new Dictionary<RewardedPlacement, RewardedSlot>();
        InterstitialSlot interstitial;
        AdConfigFile config;
        AnalyticsService analytics;
        bool bootFinished;
        bool consentStarted;
        bool mobileAdsInitialized;
        bool canRequestAds;
        bool fullScreen;
        float rewardedShownAt = -999f;
        float consentTimeout = 12f;
        Action bootCallback;

        public bool CanRequestAds => canRequestAds;
        public bool IsFullScreenOpen => fullScreen;
        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void Start(AdConfigFile adConfig, AnalyticsService analyticsService, Action onBootFinished)
        {
            config = adConfig;
            analytics = analyticsService;
            bootCallback = onBootFinished;
            foreach (RewardedPlacement placement in Enum.GetValues(typeof(RewardedPlacement)))
                rewarded[placement] = new RewardedSlot(placement, this);
            interstitial = new InterstitialSlot(this);
            BeginConsent();
        }

        public void Tick()
        {
            if (!bootFinished)
            {
                consentTimeout -= Time.unscaledDeltaTime;
                if (consentTimeout <= 0f)
                    FinishBoot();
            }
            foreach (KeyValuePair<RewardedPlacement, RewardedSlot> pair in rewarded)
                pair.Value.Tick();
            if (interstitial != null)
                interstitial.Tick();
        }

        public bool IsReady(RewardedPlacement placement)
        {
            RewardedSlot slot;
            return canRequestAds && rewarded.TryGetValue(placement, out slot) && slot.IsReady;
        }

        public void ShowRewarded(RewardedPlacement placement, Action onReward, Action<string> onUnavailable)
        {
            RewardedSlot slot;
            if (!canRequestAds || !rewarded.TryGetValue(placement, out slot))
            {
                Track("rewarded_unavailable", placement.ToString(), "ads_not_allowed");
                if (onUnavailable != null)
                    onUnavailable("Los anuncios no están disponibles. Puedes seguir jugando sin ellos.");
                return;
            }
            slot.Show(onReward, onUnavailable);
        }

        public void TryShowInterstitial(Profile profile, Action onContinue)
        {
            if (onContinue == null)
                return;
            if (!ShouldShowInterstitial(profile))
            {
                onContinue();
                return;
            }
            interstitial.Show(
                () =>
                {
                    profile.runsSinceInterstitial = 0;
                    profile.lastInterstitialUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    profile.interstitialsShown++;
                    onContinue();
                },
                onContinue);
        }

        public void ShowPrivacyOptions(Action<string> onDone)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (onDone != null)
                    onDone(error == null ? null : error.Message);
            });
        }

        public bool ShouldShowInterstitial(Profile profile)
        {
            if (!canRequestAds || interstitial == null || !interstitial.IsReady || profile == null)
                return false;
            if (profile.runsSinceInterstitial < config.interstitialMinRuns)
                return false;
            if (Time.unscaledTime - rewardedShownAt < config.interstitialAfterRewardedSeconds)
                return false;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - profile.lastInterstitialUnix < config.interstitialMinSeconds)
                return false;
            return true;
        }

        void BeginConsent()
        {
            if (consentStarted)
                return;
            consentStarted = true;
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
                ConsentDebugSettings = new ConsentDebugSettings()
            };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyDebugGeography(request);
#endif
            analytics.Track("consent_update_requested");
            try
            {
                ConsentInformation.Update(request, error =>
            {
                if (error != null)
                {
                    analytics.Track("consent_update_failed", "message", error.Message);
                    Debug.LogWarning("UMP: " + error.Message);
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        analytics.Track("consent_form_failed", "message", formError.Message);
                        Debug.LogWarning("Formulario de consentimiento: " + formError.Message);
                    }
                    canRequestAds = ConsentInformation.CanRequestAds();
                    analytics.Track("consent_result", "can_request_ads", canRequestAds ? "1" : "0");
                    if (canRequestAds)
                        InitializeMobileAds();
                    if (bootFinished && canRequestAds && ConsentCompleted != null)
                        ConsentCompleted();
                    FinishBoot();
                });
            });
            }
            catch (Exception error)
            {
                analytics.Track("consent_update_failed", "message", error.Message);
                Debug.LogWarning("No se pudo iniciar el consentimiento de anuncios. El juego continúa. " + error.Message);
                FinishBoot();
            }
        }

        void InitializeMobileAds()
        {
            if (mobileAdsInitialized)
                return;
            mobileAdsInitialized = true;
            var requestConfiguration = new RequestConfiguration
            {
                MaxAdContentRating = MaxAdContentRating.PG,
                TestDeviceIds = new List<string>()
            };
            if (config.useTestAds)
                requestConfiguration.TestDeviceIds.Add(AdRequest.TestDeviceSimulator);
            if (!string.IsNullOrEmpty(config.testDeviceHashedIds))
            {
                string[] ids = config.testDeviceHashedIds.Split(',');
                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i].Trim();
                    if (id.Length > 0)
                        requestConfiguration.TestDeviceIds.Add(id);
                }
            }
            MobileAds.SetRequestConfiguration(requestConfiguration);
            MobileAds.Initialize(status =>
            {
                if (status == null)
                {
                    analytics.Track("mobile_ads_init_failed");
                    Debug.LogError("Google Mobile Ads no se inicializó.");
                    return;
                }
                analytics.Track("mobile_ads_initialized");
                foreach (KeyValuePair<RewardedPlacement, RewardedSlot> pair in rewarded)
                    pair.Value.Load();
                interstitial.Load();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                if (config.useTestAds)
                    Debug.LogWarning("Lumina Rush está usando IDs de prueba de AdMob en una build de publicación. Pon useTestAds en false y rellena los IDs de producción antes de subirla a Google Play.");
#endif
            });
        }

        void FinishBoot()
        {
            if (bootFinished)
                return;
            bootFinished = true;
            Action callback = bootCallback;
            bootCallback = null;
            if (callback != null)
                callback();
        }

        void SetFullScreen(bool open)
        {
            if (fullScreen == open)
                return;
            fullScreen = open;
            if (FullScreenChanged != null)
                FullScreenChanged(open);
        }

        void MarkRewardedShown()
        {
            rewardedShownAt = Time.unscaledTime;
        }

        void Track(string name, string key, string value)
        {
            if (analytics != null)
                analytics.Track(name, key, value);
        }

        void Track(string name)
        {
            if (analytics != null)
                analytics.Track(name);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void ApplyDebugGeography(ConsentRequestParameters request)
        {
            if (string.IsNullOrEmpty(config.umpDebugGeography) || config.umpDebugGeography == "Disabled")
                return;
            DebugGeography geography;
            try
            {
                geography = (DebugGeography)Enum.Parse(typeof(DebugGeography), config.umpDebugGeography, true);
            }
            catch (ArgumentException)
            {
                return;
            }
            request.ConsentDebugSettings.DebugGeography = geography;
            if (!string.IsNullOrEmpty(config.testDeviceHashedIds))
            {
                string[] ids = config.testDeviceHashedIds.Split(',');
                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i].Trim();
                    if (id.Length > 0)
                        request.ConsentDebugSettings.TestDeviceHashedIds.Add(id);
                }
            }
        }
#endif

        sealed class RewardedSlot
        {
            readonly RewardedPlacement placement;
            readonly AdsService owner;
            RewardedAd ad;
            bool loading;
            bool earnedThisShow;
            float nextLoadAt;
            int attempts;

            public bool IsReady => ad != null && ad.CanShowAd();

            public RewardedSlot(RewardedPlacement placement, AdsService owner)
            {
                this.placement = placement;
                this.owner = owner;
            }

            public void Tick()
            {
                if (!owner.canRequestAds || loading || IsReady)
                    return;
                if (Time.unscaledTime < nextLoadAt)
                    return;
                Load();
            }

            public void Load()
            {
                if (!owner.canRequestAds || loading)
                    return;
                string unit = AdConfig.Unit(owner.config, placement);
                if (string.IsNullOrEmpty(unit))
                {
                    nextLoadAt = Time.unscaledTime + 30f;
                    owner.Track("rewarded_unavailable", placement.ToString(), "missing_unit_id");
                    return;
                }
                loading = true;
                if (ad != null)
                {
                    ad.Destroy();
                    ad = null;
                }
                owner.Track("rewarded_load_requested", placement.ToString(), "1");
                RewardedAd.Load(unit, new AdRequest(), (RewardedAd loaded, LoadAdError error) =>
                {
                    loading = false;
                    if (error != null || loaded == null)
                    {
                        attempts++;
                        nextLoadAt = Time.unscaledTime + Mathf.Min(60f, 8f * attempts);
                        string message = error != null ? error.ToString() : "null_ad";
                        owner.analytics.Track("rewarded_load_failed", "placement", placement.ToString(), "message", message);
                        Debug.LogWarning("Anuncio recompensado no cargado (" + placement + "): " + message);
                        return;
                    }
                    attempts = 0;
                    ad = loaded;
                    Register(loaded);
                    owner.Track("rewarded_loaded", placement.ToString(), "1");
                });
            }

            public void Show(Action onReward, Action<string> onUnavailable)
            {
                if (!IsReady)
                {
                    owner.Track("rewarded_unavailable", placement.ToString(), "not_ready");
                    Load();
                    if (onUnavailable != null)
                        onUnavailable("Anuncio no disponible ahora. La partida sigue sin necesidad de verlo.");
                    return;
                }
                earnedThisShow = false;
                RewardedAd showing = ad;
                owner.MarkRewardedShown();
                owner.SetFullScreen(true);
                owner.Track("rewarded_show", placement.ToString(), "1");
                showing.Show(reward =>
                {
                    if (earnedThisShow)
                        return;
                    earnedThisShow = true;
                    owner.analytics.Track(
                        "rewarded_completed",
                        "placement", placement.ToString(),
                        "reward_type", reward != null ? reward.Type : "",
                        "reward_amount", reward != null ? reward.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0");
                    if (onReward != null)
                        onReward();
                    owner.analytics.Track("reward_granted", "placement", placement.ToString());
                });
            }

            void Register(RewardedAd loaded)
            {
                loaded.OnAdFullScreenContentOpened += () => owner.SetFullScreen(true);
                loaded.OnAdFullScreenContentClosed += () =>
                {
                    owner.SetFullScreen(false);
                    ReleaseAndReload();
                };
                loaded.OnAdFullScreenContentFailed += error =>
                {
                    owner.SetFullScreen(false);
                    owner.analytics.Track("rewarded_failed", "placement", placement.ToString(), "message", error != null ? error.ToString() : "");
                    ReleaseAndReload();
                };
                loaded.OnAdPaid += value => ReportPaid("rewarded_" + placement, value);
            }

            void ReleaseAndReload()
            {
                if (ad != null)
                {
                    ad.Destroy();
                    ad = null;
                }
                nextLoadAt = Time.unscaledTime + 1f;
            }

            void ReportPaid(string placementName, AdValue value)
            {
                if (value == null || owner.analytics == null)
                    return;
                owner.analytics.TrackAdValue(placementName, value.Value, value.CurrencyCode, value.Precision.ToString());
            }
        }

        sealed class InterstitialSlot
        {
            readonly AdsService owner;
            InterstitialAd ad;
            bool loading;
            bool waiting;
            bool opened;
            bool continued;
            float nextLoadAt;
            float watchdog;
            int attempts;
            Action onShown;
            Action onSkipped;

            public bool IsReady => ad != null && ad.CanShowAd();

            public InterstitialSlot(AdsService owner)
            {
                this.owner = owner;
            }

            public void Tick()
            {
                if (waiting && !opened)
                {
                    watchdog -= Time.unscaledDeltaTime;
                    if (watchdog <= 0f)
                        Continue(onSkipped);
                }
                if (!owner.canRequestAds || loading || IsReady || waiting)
                    return;
                if (Time.unscaledTime < nextLoadAt)
                    return;
                Load();
            }

            public void Load()
            {
                if (!owner.canRequestAds || loading)
                    return;
                string unit = AdConfig.InterstitialUnit(owner.config);
                if (string.IsNullOrEmpty(unit))
                {
                    nextLoadAt = Time.unscaledTime + 30f;
                    return;
                }
                loading = true;
                if (ad != null)
                {
                    ad.Destroy();
                    ad = null;
                }
                owner.Track("interstitial_load_requested");
                InterstitialAd.Load(unit, new AdRequest(), (InterstitialAd loaded, LoadAdError error) =>
                {
                    loading = false;
                    if (error != null || loaded == null)
                    {
                        attempts++;
                        nextLoadAt = Time.unscaledTime + Mathf.Min(60f, 8f * attempts);
                        owner.analytics.Track("interstitial_load_failed", "message", error != null ? error.ToString() : "null_ad");
                        return;
                    }
                    attempts = 0;
                    ad = loaded;
                    loaded.OnAdFullScreenContentOpened += () =>
                    {
                        opened = true;
                        owner.SetFullScreen(true);
                    };
                    loaded.OnAdFullScreenContentClosed += () =>
                    {
                        owner.SetFullScreen(false);
                        Continue(onShown);
                    };
                    loaded.OnAdFullScreenContentFailed += fail =>
                    {
                        owner.SetFullScreen(false);
                        owner.analytics.Track("interstitial_failed", "message", fail != null ? fail.ToString() : "");
                        Continue(onSkipped);
                    };
                    loaded.OnAdPaid += value =>
                    {
                        if (value != null)
                            owner.analytics.TrackAdValue("interstitial", value.Value, value.CurrencyCode, value.Precision.ToString());
                    };
                    owner.Track("interstitial_loaded");
                });
            }

            public void Show(Action whenShown, Action whenSkipped)
            {
                if (!IsReady)
                {
                    if (whenSkipped != null)
                        whenSkipped();
                    return;
                }
                onShown = whenShown;
                onSkipped = whenSkipped;
                continued = false;
                opened = false;
                waiting = true;
                watchdog = 8f;
                owner.SetFullScreen(true);
                owner.Track("interstitial_show");
                ad.Show();
            }

            void Continue(Action callback)
            {
                if (continued)
                    return;
                continued = true;
                waiting = false;
                opened = false;
                owner.SetFullScreen(false);
                if (ad != null)
                {
                    ad.Destroy();
                    ad = null;
                }
                nextLoadAt = Time.unscaledTime + 1f;
                if (callback != null)
                    callback();
            }
        }
    }
}
