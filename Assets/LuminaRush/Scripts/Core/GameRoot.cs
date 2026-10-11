using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuminaRush
{
    public enum Phase
    {
        Boot,
        Menu,
        Playing,
        Paused,
        GameOver
    }

    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        Profile profile;
        AdConfigFile adConfig;
        AnalyticsService analytics;
        AdsService ads;
        AudioDirector audioDirector;
        GameUi ui;
        WorldStreamer world;
        PlayerController player;
        CharacterVisual podium;
        CameraRig cameraRig;
        Effects effects;
        SwipeReader swipes;
        Phase phase;
        bool bootstrapped;
        bool runClosed;
        bool baseBanked;
        bool doubled;
        bool reviveUsed;
        bool reviveQueued;
        bool doubleQueued;
        bool bonusQueued;
        int pendingMissionBonus = -1;
        int runCoins;
        float runStartedAt;
        GameObject menuSet;
        int lastBiome = -1;
        float deleteArmedUntil;
        float tutorialTimer;
        string lastBiomeName = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindRoot() != null)
                return;
            var host = new GameObject("LuminaRush");
            host.AddComponent<GameRoot>();
        }

        static GameRoot FindRoot()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<GameRoot>();
#else
            return FindObjectOfType<GameRoot>();
#endif
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (!bootstrapped)
                Bootstrap();
        }

        void Bootstrap()
        {
            bootstrapped = true;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Physics.autoSyncTransforms = true;
            adConfig = AdConfig.Load();
            profile = ProfileStore.Load();
            Progression.TouchDay(profile);
            analytics = new AnalyticsService(false);
            RecordSession();
            EnsureEventSystem();
            PrepareCamera();
            audioDirector = gameObject.AddComponent<AudioDirector>();
            audioDirector.Build();
            audioDirector.SetMusic(profile.music);
            audioDirector.SetSfx(profile.sfx);
            effects = gameObject.AddComponent<Effects>();
            effects.Build();
            Material material = MeshLibrary.SharedMaterial();
            world = gameObject.AddComponent<WorldStreamer>();
            world.Build(material, transform);
            world.SetVisible(false);
            BuildPlayer(material);
            BuildPodium(material);
            ui = gameObject.AddComponent<GameUi>();
            ui.Build();
            WireUi();
            swipes = new SwipeReader();
            ads = new AdsService();
            ads.FullScreenChanged += audioDirector.SetDuck;
            ads.ConsentCompleted += () => FirebaseAnalyticsSink.TryInit();
            ApplyBiome(Catalog.Biomes[0], true);
            cameraRig.SnapToMenu(Vector3.zero);
            ui.ShowBoot("Preparando Lumina Rush");
            ads.Start(adConfig, analytics, EnterMenu);
            ProfileStore.Save(profile);
        }

        void Update()
        {
            if (!bootstrapped || ui == null)
                return;
            ui.Tick();
            ads.Tick();
            float dt = Time.deltaTime;
            if (phase == Phase.Menu || phase == Phase.Boot)
                podium.Animate(Time.unscaledDeltaTime, 0f, true, false, 0f, 0f, false);
            if (ads.IsFullScreenOpen)
                return;
            ResolveQueuedRewards();
            if (phase == Phase.Playing)
                Simulate(dt);
            else if (phase == Phase.GameOver)
                player.Tick(dt, 0f, default(SwipeFrame));
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBack();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && profile != null)
                ProfileStore.Save(profile);
        }

        void OnApplicationQuit()
        {
            if (profile != null)
                ProfileStore.Save(profile);
        }

        void Simulate(float dt)
        {
            float distance = Mathf.Max(0f, player.transform.position.z);
            int tier = distance < 250f ? 0 : distance < 700f ? 1 : distance < 1300f ? 2 : 3;
            float speed = Mathf.Lerp(11f, 23f, Mathf.Clamp01(distance / 1700f));
            SwipeFrame swipe = swipes.Read();
            player.Tick(dt, speed, swipe);
            if (phase != Phase.Playing)
                return;
            BiomeLook biome = BiomeAt(player.transform.position.z);
            world.Tick(player.transform.position.z, dt, tier, biome, player);
            ApplyBiome(biome, false);
            cameraRig.Follow(player.transform.position, dt);
            int shownDistance = Mathf.FloorToInt(player.transform.position.z);
            string hint = "";
            if (profile.tutorialSeen == 0)
            {
                tutorialTimer += dt;
                hint = "Desliza a los lados, arriba para saltar y abajo para agacharte";
                if (tutorialTimer > 8f)
                {
                    profile.tutorialSeen = 1;
                    ProfileStore.Save(profile);
                }
            }
            ui.UpdateHud(Score(shownDistance), runCoins, shownDistance, profile.shieldCharges, profile.magnetCharges, profile.boostCharges, hint);
        }

        void StartRun()
        {
            runClosed = false;
            baseBanked = false;
            doubled = false;
            reviveUsed = false;
            reviveQueued = false;
            doubleQueued = false;
            runCoins = 0;
            tutorialTimer = 0f;
            lastBiome = -1;
            podium.gameObject.SetActive(false);
            world.SetVisible(true);
            world.ResetRun(Catalog.Biomes[0]);
            player.gameObject.SetActive(true);
            player.ApplyLook(profile.characterIndex, profile.skinIndex);
            player.ResetAt(new Vector3(0f, 0f, 2f));
            cameraRig.SnapToRun(player.transform.position);
            runStartedAt = Time.unscaledTime;
            phase = Phase.Playing;
            if (menuSet != null)
                menuSet.SetActive(false);
            ApplyBiome(Catalog.Biomes[0], true);
            ui.ShowHud();
            audioDirector.PlayStart();
            analytics.Track("run_start", "character", profile.characterIndex.ToString(), "skin", profile.skinIndex.ToString());
        }

        void FinishRun()
        {
            if (runClosed)
                return;
            runClosed = true;
            BankBase();
            float distance = Mathf.Max(0f, player.transform.position.z);
            int score = Score(Mathf.FloorToInt(distance));
            profile.runs++;
            profile.lifetimeDistance += distance;
            profile.runsSinceInterstitial++;
            bool bestDistance = distance > profile.bestDistance;
            bool bestScore = score > profile.bestScore;
            if (bestDistance)
                profile.bestDistance = distance;
            if (bestScore)
                profile.bestScore = score;
            Progression.AddMission(profile, MissionKind.Distance, Mathf.FloorToInt(distance));
            Progression.AddMission(profile, MissionKind.Runs, 1);
            var result = new RunResult
            {
                Distance = distance,
                Coins = runCoins,
                Score = score,
                Revived = reviveUsed
            };
            Progression.EvaluateAchievements(profile, result);
            analytics.Track(
                "run_end",
                "distance", distance.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                "duration", Mathf.Max(0f, Time.unscaledTime - runStartedAt).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                "coins", runCoins.ToString(),
                "score", score.ToString(),
                "revived", reviveUsed ? "1" : "0");
            ProfileStore.Save(profile);
        }

        void BankBase()
        {
            if (baseBanked)
                return;
            baseBanked = true;
            Progression.GrantCoins(profile, runCoins);
            analytics.Track("coins_banked", "amount", runCoins.ToString(), "source", "run");
        }

        void EnterMenu()
        {
            if (ads != null && ads.CanRequestAds)
                FirebaseAnalyticsSink.TryInit();
            phase = Phase.Menu;
            player.gameObject.SetActive(false);
            world.SetVisible(false);
            podium.gameObject.SetActive(true);
            if (menuSet != null)
                menuSet.SetActive(true);
            podium.Apply(profile.characterIndex, profile.skinIndex);
            cameraRig.SnapToMenu(Vector3.zero);
            ApplyBiome(Catalog.Biomes[0], true);
            RefreshMenu();
        }

        void RefreshMenu()
        {
            Progression.TouchDay(profile);
            int left = Mathf.Max(0, adConfig.bonusAdDailyCap - profile.bonusAdsToday);
            ui.ShowMenu(profile, ads.IsReady(RewardedPlacement.Bonus), left, adConfig.bonusAdCoinReward);
        }

        void ResolveQueuedRewards()
        {
            if (reviveQueued)
            {
                reviveQueued = false;
                ApplyRevive();
            }
            if (doubleQueued)
            {
                doubleQueued = false;
                ApplyDouble();
            }
            if (bonusQueued)
            {
                bonusQueued = false;
                ApplyBonus();
            }
            if (pendingMissionBonus >= 0)
            {
                int index = pendingMissionBonus;
                pendingMissionBonus = -1;
                int reward;
                if (Progression.ClaimMissionBonus(profile, index, out reward))
                {
                    profile.rewardedCompleted++;
                    audioDirector.PlayUnlock();
                    analytics.Track("reward_granted", "placement", "mission", "amount", reward.ToString());
                    ui.Toast("Bonus de misión: +" + reward + " monedas");
                    ProfileStore.Save(profile);
                    ui.ShowMissions(profile, ads.IsReady(RewardedPlacement.Mission));
                }
            }
        }

        void ApplyRevive()
        {
            if (reviveUsed || phase != Phase.GameOver)
                return;
            reviveUsed = true;
            runClosed = false;
            profile.revivesUsed++;
            profile.rewardedCompleted++;
            player.Revive();
            world.ClearAhead(player.transform.position.z, 14f);
            phase = Phase.Playing;
            ui.ShowHud();
            audioDirector.PlayStart();
            analytics.Track("revive_applied");
            Progression.EvaluateAchievements(profile, new RunResult());
            ProfileStore.Save(profile);
        }

        void ApplyDouble()
        {
            if (doubled || phase != Phase.GameOver)
                return;
            doubled = true;
            BankBase();
            Progression.GrantCoins(profile, runCoins);
            profile.rewardedCompleted++;
            analytics.Track("reward_granted", "placement", "double_coins", "amount", runCoins.ToString());
            ProfileStore.Save(profile);
            audioDirector.PlayUnlock();
            ui.Toast("Monedas duplicadas");
            ShowGameOver();
        }

        void ApplyBonus()
        {
            if (profile.bonusAdsToday >= adConfig.bonusAdDailyCap)
                return;
            profile.bonusAdsToday++;
            Progression.GrantCoins(profile, adConfig.bonusAdCoinReward);
            profile.rewardedCompleted++;
            analytics.Track("reward_granted", "placement", "bonus", "amount", adConfig.bonusAdCoinReward.ToString());
            ProfileStore.Save(profile);
            audioDirector.PlayUnlock();
            ui.Toast("+" + adConfig.bonusAdCoinReward + " monedas");
            if (phase == Phase.Menu)
                RefreshMenu();
        }

        void ShowGameOver()
        {
            float distance = Mathf.Max(0f, player.transform.position.z);
            int score = Score(Mathf.FloorToInt(distance));
            bool best = score > profile.bestScore || distance > profile.bestDistance;
            ui.ShowGameOver(
                score,
                distance,
                doubled ? runCoins * 2 : runCoins,
                best,
                !reviveUsed && ads.IsReady(RewardedPlacement.Revive),
                reviveUsed,
                !doubled && runCoins > 0 && ads.IsReady(RewardedPlacement.DoubleCoins),
                doubled);
        }

        void WireUi()
        {
            ui.Play += () =>
            {
                audioDirector.PlayUi();
                StartRun();
            };
            ui.Pause += () =>
            {
                if (phase != Phase.Playing)
                    return;
                phase = Phase.Paused;
                audioDirector.PlayUi();
                ui.ShowPause();
            };
            ui.Resume += () =>
            {
                if (phase != Phase.Paused)
                    return;
                phase = Phase.Playing;
                audioDirector.PlayUi();
                ui.HidePause();
            };
            ui.Menu += () =>
            {
                audioDirector.PlayUi();
                if (ui.ShopOpen || ui.MissionsOpen || ui.SettingsOpen)
                {
                    EnterMenu();
                    return;
                }
                if (phase == Phase.Playing || phase == Phase.Paused || phase == Phase.GameOver)
                    FinishRun();
                ads.TryShowInterstitial(profile, EnterMenu);
            };
            ui.Retry += () =>
            {
                audioDirector.PlayUi();
                if (phase == Phase.GameOver)
                    FinishRun();
                ads.TryShowInterstitial(profile, StartRun);
            };
            ui.Revive += () =>
            {
                audioDirector.PlayUi();
                ads.ShowRewarded(RewardedPlacement.Revive, () =>
                {
                    reviveQueued = true;
                    }, message => ui.Toast(message));
            };
            ui.DoubleCoins += () =>
            {
                audioDirector.PlayUi();
                ads.ShowRewarded(RewardedPlacement.DoubleCoins, () => doubleQueued = true, message => ui.Toast(message));
            };
            ui.BonusAd += () =>
            {
                audioDirector.PlayUi();
                if (profile.bonusAdsToday >= adConfig.bonusAdDailyCap)
                {
                    ui.Toast("Ya reclamaste los anuncios opcionales de hoy.");
                    return;
                }
                ads.ShowRewarded(RewardedPlacement.Bonus, () => bonusQueued = true, message => ui.Toast(message));
            };
            ui.OpenShop += () =>
            {
                audioDirector.PlayUi();
                analytics.Track("shop_open");
                ui.ShowShop(profile);
            };
            ui.OpenMissions += () =>
            {
                audioDirector.PlayUi();
                analytics.Track("missions_open");
                Progression.TouchDay(profile);
                ui.ShowMissions(profile, ads.IsReady(RewardedPlacement.Mission));
            };
            ui.OpenSettings += () =>
            {
                audioDirector.PlayUi();
                analytics.Track("settings_open");
                ui.ShowSettings(profile, ads.PrivacyOptionsRequired, !string.IsNullOrEmpty(adConfig.privacyPolicyUrl));
            };
            ui.BuyCharacter += index => Purchase(() => Progression.TryBuyCharacter(profile, index, out string error) ? null : error, "character", index);
            ui.SelectCharacter += index => Select(() => Progression.TrySelectCharacter(profile, index, out string error) ? null : error, "character", index);
            ui.BuySkin += index => Purchase(() => Progression.TryBuySkin(profile, index, out string error) ? null : error, "skin", index);
            ui.SelectSkin += index => Select(() => Progression.TrySelectSkin(profile, index, out string error) ? null : error, "skin", index);
            ui.BuyBoost += index => Purchase(() => Progression.TryBuyBoost(profile, (PowerKind)index, out string error) ? null : error, "boost", index);
            ui.UseBoost += UseBoost;
            ui.ClaimMission += index =>
            {
                int reward;
                if (!Progression.ClaimMission(profile, index, out reward))
                {
                    ui.Toast("Esta misión todavía no se puede reclamar.");
                    return;
                }
                audioDirector.PlayUnlock();
                effects.Burst(podium.transform.position + Vector3.up, Catalog.Hex("F5C542"));
                analytics.Track("mission_claimed", "index", index.ToString(), "amount", reward.ToString());
                ProfileStore.Save(profile);
                ui.Toast("+" + reward + " monedas");
                ui.ShowMissions(profile, ads.IsReady(RewardedPlacement.Mission));
            };
            ui.BonusMission += index =>
            {
                audioDirector.PlayUi();
                ads.ShowRewarded(RewardedPlacement.Mission, () => pendingMissionBonus = index, message => ui.Toast(message));
            };
            ui.ClaimAchievement += index =>
            {
                int reward;
                if (!Progression.ClaimAchievement(profile, index, out reward))
                {
                    ui.Toast("Este logro todavía no está listo.");
                    return;
                }
                audioDirector.PlayUnlock();
                analytics.Track("achievement_claimed", "index", index.ToString(), "amount", reward.ToString());
                ProfileStore.Save(profile);
                ui.Toast("+" + reward + " monedas");
                ui.ShowMissions(profile, ads.IsReady(RewardedPlacement.Mission));
            };
            ui.MusicChanged += value =>
            {
                profile.music = value;
                audioDirector.SetMusic(value);
                ProfileStore.Save(profile);
            };
            ui.SfxChanged += value =>
            {
                profile.sfx = value;
                audioDirector.SetSfx(value);
                audioDirector.PlayUi();
                ProfileStore.Save(profile);
            };
            ui.VibrationChanged += value =>
            {
                profile.vibration = value ? 1 : 0;
                ProfileStore.Save(profile);
            };
            ui.Privacy += () => ads.ShowPrivacyOptions(message =>
            {
                if (!string.IsNullOrEmpty(message))
                    ui.Toast(message);
            });
            ui.OpenPolicy += () =>
            {
                if (!string.IsNullOrEmpty(adConfig.privacyPolicyUrl))
                    Application.OpenURL(adConfig.privacyPolicyUrl);
            };
            ui.DeleteData += DeleteLocalData;
        }

        void Purchase(Func<string> action, string kind, int index)
        {
            string error = action();
            if (!string.IsNullOrEmpty(error))
            {
                ui.Toast(error);
                return;
            }
            audioDirector.PlayUnlock();
            effects.Burst(podium.transform.position + Vector3.up * 1.2f, Catalog.Hex("FF5A36"));
            podium.Apply(profile.characterIndex, profile.skinIndex);
            analytics.Track("shop_purchase", "kind", kind, "index", index.ToString(), "coins_left", profile.coins.ToString());
            Progression.EvaluateAchievements(profile, new RunResult());
            ProfileStore.Save(profile);
            ui.ShowShop(profile);
        }

        void Select(Func<string> action, string kind, int index)
        {
            string error = action();
            if (!string.IsNullOrEmpty(error))
            {
                ui.Toast(error);
                return;
            }
            audioDirector.PlayUi();
            podium.Apply(profile.characterIndex, profile.skinIndex);
            analytics.Track("cosmetic_selected", "kind", kind, "index", index.ToString());
            ProfileStore.Save(profile);
            ui.ShowShop(profile);
        }

        void UseBoost(int index)
        {
            if (phase != Phase.Playing)
                return;
            PowerKind kind = (PowerKind)Mathf.Clamp(index, 0, 2);
            if (!Progression.TryConsumeCharge(profile, kind))
            {
                ui.Toast("No tienes cargas. Puedes comprarlas en la tienda.");
                return;
            }
            if (kind == PowerKind.Shield)
                player.ActivateShield();
            else if (kind == PowerKind.Magnet)
                player.ActivateMagnet(8f);
            else
                player.ActivateBoost(10f);
            audioDirector.PlayUi();
            analytics.Track("powerup_used", "kind", kind.ToString());
            ProfileStore.Save(profile);
        }

        void DeleteLocalData()
        {
            if (Time.unscaledTime > deleteArmedUntil)
            {
                deleteArmedUntil = Time.unscaledTime + 3f;
                ui.Toast("Pulsa otra vez para borrar el progreso de este dispositivo.");
                return;
            }
            ProfileStore.Delete();
            analytics.DeleteLocalLog();
            analytics.Track("local_data_deleted");
            profile = new Profile();
            Progression.TouchDay(profile);
            audioDirector.SetMusic(profile.music);
            audioDirector.SetSfx(profile.sfx);
            podium.Apply(profile.characterIndex, profile.skinIndex);
            ProfileStore.Save(profile);
            ui.Toast("Datos locales borrados.");
            EnterMenu();
        }

        void HandleBack()
        {
            if (phase == Phase.Playing)
            {
                phase = Phase.Paused;
                ui.ShowPause();
            }
            else if (phase == Phase.Paused)
            {
                phase = Phase.Playing;
                ui.HidePause();
            }
            else if (ui.ShopOpen || ui.MissionsOpen || ui.SettingsOpen)
            {
                EnterMenu();
            }
        }

        void PrepareCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 220f;
            camera.allowMSAA = false;
            cameraRig = camera.GetComponent<CameraRig>();
            if (cameraRig == null)
                cameraRig = camera.gameObject.AddComponent<CameraRig>();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 110f;
        }

        void BuildPlayer(Material material)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Runner");
            GameObject go = prefab != null ? Instantiate(prefab, transform) : new GameObject("Runner");
            if (prefab == null)
                go.transform.SetParent(transform, false);
            player = go.GetComponent<PlayerController>();
            if (player == null)
                player = go.AddComponent<PlayerController>();
            player.Build(material);
            player.Hit += OnHit;
            player.CoinCollected += OnCoin;
            player.Jumped += () =>
            {
                profile.jumps++;
                Progression.AddMission(profile, MissionKind.Jumps, 1);
                audioDirector.PlayJump();
            };
            player.Slid += () =>
            {
                profile.slides++;
                Progression.AddMission(profile, MissionKind.Slides, 1);
                audioDirector.PlaySlide();
            };
            player.PowerCollected += kind =>
            {
                audioDirector.PlayUnlock();
                analytics.Track("powerup_pickup", "kind", kind.ToString());
            };
            go.SetActive(false);
        }

        void BuildPodium(Material material)
        {
            menuSet = new GameObject("MenuSet");
            menuSet.transform.SetParent(transform, false);
            var island = MeshLibrary.Create("Podium", MeshLibrary.Cylinder(), material, menuSet.transform);
            island.transform.position = new Vector3(0f, -0.45f, 0f);
            island.transform.localScale = new Vector3(3.2f, 0.45f, 3.2f);
            MeshLibrary.Tint(island.GetComponent<Renderer>(), Catalog.Biomes[0].Ground, new MaterialPropertyBlock());
            var crystal = MeshLibrary.Create("DecorCrystal", MeshLibrary.Crystal(), material, menuSet.transform);
            crystal.transform.position = new Vector3(1.7f, 0.4f, 0.4f);
            crystal.transform.localScale = Vector3.one * 1.3f;
            MeshLibrary.Tint(crystal.GetComponent<Renderer>(), Catalog.Biomes[0].PropA, new MaterialPropertyBlock());
            var tree = MeshLibrary.Create("DecorTree", MeshLibrary.Cone(), material, menuSet.transform);
            tree.transform.position = new Vector3(-1.8f, 0.7f, 0.2f);
            tree.transform.localScale = new Vector3(1.1f, 1.8f, 1.1f);
            MeshLibrary.Tint(tree.GetComponent<Renderer>(), Catalog.Biomes[0].PropB, new MaterialPropertyBlock());
            var host = new GameObject("PodiumRunner");
            host.transform.SetParent(menuSet.transform, false);
            host.transform.position = new Vector3(0f, 0f, 0f);
            podium = host.AddComponent<CharacterVisual>();
            podium.Build(material, host.transform);
            podium.Apply(profile.characterIndex, profile.skinIndex);
        }

        void OnHit()
        {
            if (phase != Phase.Playing)
                return;
            phase = Phase.GameOver;
            audioDirector.PlayHit();
            effects.Burst(player.transform.position + Vector3.up, Catalog.Hex("FF5A36"));
            cameraRig.Punch();
            if (profile.vibration != 0)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
            ShowGameOver();
        }

        void OnCoin(int value)
        {
            runCoins += value;
            Progression.AddMission(profile, MissionKind.Coins, value);
            audioDirector.PlayCoin();
            effects.Burst(player.CollectCenter, Catalog.Hex("F5C542"));
        }

        void ApplyBiome(BiomeLook biome, bool force)
        {
            int index = BiomeIndex(player != null && phase == Phase.Playing ? player.transform.position.z : 0f);
            Camera camera = cameraRig.GetComponent<Camera>();
            camera.backgroundColor = biome.Sky;
            RenderSettings.fogColor = biome.Fog;
            RenderSettings.ambientLight = biome.Sky;
            if (!force && index == lastBiome)
                return;
            if (!force && lastBiome >= 0 && biome.Name != lastBiomeName)
                ui.Toast(biome.Name);
            lastBiome = index;
            lastBiomeName = biome.Name;
        }

        BiomeLook BiomeAt(float distance)
        {
            float span = 450f;
            float cursor = Mathf.Max(0f, distance) / span;
            int index = Mathf.FloorToInt(cursor);
            float local = cursor - index;
            BiomeLook from = Catalog.Biomes[index % Catalog.Biomes.Length];
            BiomeLook to = Catalog.Biomes[(index + 1) % Catalog.Biomes.Length];
            float blend = local > 0.82f ? (local - 0.82f) / 0.18f : 0f;
            return Catalog.Blend(from, to, blend);
        }

        int BiomeIndex(float distance)
        {
            return Mathf.FloorToInt(Mathf.Max(0f, distance) / 450f);
        }

        int Score(int distance)
        {
            return distance + runCoins * 5;
        }

        void RecordSession()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrEmpty(profile.firstDay))
                profile.firstDay = today;
            profile.sessions++;
            DateTime first;
            int day = 0;
            if (DateTime.TryParse(profile.firstDay, out first))
                day = Mathf.Max(0, (int)(DateTime.Parse(today) - first.Date).TotalDays);
            bool returned = profile.lastDay != today && profile.sessions > 1;
            profile.lastDay = today;
            analytics.Track("session_start", "day_index", day.ToString(), "sessions", profile.sessions.ToString());
            if (returned)
                analytics.Track("retention_active", "day_index", day.ToString());
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
