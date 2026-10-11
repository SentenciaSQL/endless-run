using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LuminaRush
{
    public sealed class GameUi : MonoBehaviour
    {
        public event Action Play;
        public event Action Pause;
        public event Action Resume;
        public event Action Retry;
        public event Action Menu;
        public event Action Revive;
        public event Action DoubleCoins;
        public event Action BonusAd;
        public event Action OpenShop;
        public event Action OpenMissions;
        public event Action OpenSettings;
        public event Action<int> BuyCharacter;
        public event Action<int> SelectCharacter;
        public event Action<int> BuySkin;
        public event Action<int> SelectSkin;
        public event Action<int> BuyBoost;
        public event Action<int> UseBoost;
        public event Action<int> ClaimMission;
        public event Action<int> BonusMission;
        public event Action<int> ClaimAchievement;
        public event Action<float> MusicChanged;
        public event Action<float> SfxChanged;
        public event Action<bool> VibrationChanged;
        public event Action Privacy;
        public event Action OpenPolicy;
        public event Action DeleteData;

        RectTransform safe;
        Font font;
        Sprite panelSprite;
        Sprite buttonSprite;
        GameObject boot;
        GameObject menu;
        GameObject hud;
        GameObject pause;
        GameObject gameOver;
        GameObject shop;
        GameObject missions;
        GameObject settings;
        Text bootText;
        Text menuCoins;
        Text menuBest;
        Text bonusLabel;
        Button bonusButton;
        Text hudScore;
        Text hudCoins;
        Text hudDistance;
        Text tutorial;
        Text shieldCount;
        Text magnetCount;
        Text boostCount;
        Text overTitle;
        Text overBody;
        Button reviveButton;
        Text reviveLabel;
        Button doubleButton;
        Text doubleLabel;
        RectTransform shopContent;
        RectTransform missionContent;
        Slider musicSlider;
        Slider sfxSlider;
        Toggle vibrationToggle;
        Button privacyButton;
        Button policyButton;
        Text toastText;
        CanvasGroup toastGroup;
        float toastTimer;
        bool suppressSlider;
        int shopTab;
        int hudScoreValue = int.MinValue;
        int hudCoinValue = int.MinValue;
        int hudDistanceValue = int.MinValue;

        public bool ShopOpen { get { return shop != null && shop.activeSelf; } }
        public bool MissionsOpen { get { return missions != null && missions.activeSelf; } }
        public bool SettingsOpen { get { return settings != null && settings.activeSelf; } }

        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            panelSprite = RoundedSprite(64, 18);
            buttonSprite = RoundedSprite(48, 14);

            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.55f;
            canvasObject.AddComponent<GraphicRaycaster>();

            safe = Rect("Safe", canvasObject.transform);
            Stretch(safe);

            boot = Screen("Boot");
            bootText = Label(boot.transform, "Preparando Lumina Rush", 40, TextAnchor.MiddleCenter);
            Stretch(bootText.rectTransform, 80f, 80f, 80f, 80f);

            menu = Screen("Menu");
            BuildMenu();
            hud = Screen("Hud");
            BuildHud();
            pause = Screen("Pause");
            BuildPause();
            gameOver = Screen("GameOver");
            BuildGameOver();
            shop = Screen("Shop");
            BuildShopShell();
            missions = Screen("Missions");
            BuildMissionShell();
            settings = Screen("Settings");
            BuildSettings();
            BuildToast(canvasObject.transform);
            HideAll();
        }

        public void Tick()
        {
            ApplySafeArea();
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.35f);
                if (toastTimer <= 0f)
                    toastText.transform.parent.gameObject.SetActive(false);
            }
        }

        public void ShowBoot(string message)
        {
            HideAll();
            boot.SetActive(true);
            bootText.text = message;
        }

        public void ShowMenu(Profile profile, bool bonusReady, int bonusLeft, int bonusReward)
        {
            HideAll();
            menu.SetActive(true);
            menuCoins.text = profile.coins + " monedas";
            menuBest.text = "Récord  " + profile.bestScore + "   ·   " + Mathf.FloorToInt(profile.bestDistance) + " m";
            bonusButton.interactable = bonusReady && bonusLeft > 0;
            bonusLabel.text = bonusLeft > 0
                ? "Anuncio opcional  +" + bonusReward + "   (" + bonusLeft + " hoy)"
                : "Bonus diario de anuncios agotado";
        }

        public void ShowHud()
        {
            HideAll();
            hud.SetActive(true);
            hudScoreValue = int.MinValue;
            hudCoinValue = int.MinValue;
            hudDistanceValue = int.MinValue;
        }

        public void UpdateHud(int score, int coins, int distance, int shields, int magnets, int boosts, string tutorialText)
        {
            if (score != hudScoreValue)
            {
                hudScoreValue = score;
                hudScore.text = score.ToString();
            }
            if (coins != hudCoinValue)
            {
                hudCoinValue = coins;
                hudCoins.text = coins.ToString();
            }
            if (distance != hudDistanceValue)
            {
                hudDistanceValue = distance;
                hudDistance.text = distance + " m";
            }
            shieldCount.text = shields.ToString();
            magnetCount.text = magnets.ToString();
            boostCount.text = boosts.ToString();
            tutorial.gameObject.SetActive(!string.IsNullOrEmpty(tutorialText));
            tutorial.text = tutorialText ?? "";
        }

        public void ShowPause()
        {
            pause.SetActive(true);
        }

        public void HidePause()
        {
            pause.SetActive(false);
        }

        public void ShowGameOver(int score, float distance, int coins, bool best, bool reviveReady, bool reviveUsed, bool doubleReady, bool doubled)
        {
            HideAll();
            gameOver.SetActive(true);
            overTitle.text = best ? "Nuevo récord" : "Fin de la carrera";
            overBody.text = Mathf.FloorToInt(distance) + " m\n" + score + " puntos\n+" + coins + " monedas";
            reviveButton.gameObject.SetActive(!reviveUsed);
            reviveButton.interactable = reviveReady;
            reviveLabel.text = reviveReady ? "Revivir con anuncio" : "Revivir no disponible";
            bool showDouble = coins > 0 && !doubled;
            doubleButton.gameObject.SetActive(showDouble);
            doubleButton.interactable = doubleReady;
            doubleLabel.text = doubleReady ? "Duplicar monedas" : "Duplicar no disponible";
        }

        public void ShowShop(Profile profile)
        {
            HideAll();
            shop.SetActive(true);
            RebuildShop(profile);
        }

        public void ShowMissions(Profile profile, bool adsReady)
        {
            HideAll();
            missions.SetActive(true);
            RebuildMissions(profile, adsReady);
        }

        public void ShowSettings(Profile profile, bool privacyRequired, bool hasPolicy)
        {
            HideAll();
            settings.SetActive(true);
            suppressSlider = true;
            musicSlider.value = profile.music;
            sfxSlider.value = profile.sfx;
            vibrationToggle.isOn = profile.vibration != 0;
            suppressSlider = false;
            privacyButton.gameObject.SetActive(privacyRequired);
            policyButton.gameObject.SetActive(hasPolicy);
        }

        public void Toast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            toastText.text = message;
            toastText.transform.parent.gameObject.SetActive(true);
            toastGroup.alpha = 1f;
            toastTimer = 2.6f;
        }

        void BuildMenu()
        {
            Panel(menu.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            Title(menu.transform, "LUMINA RUSH", 0.78f);
            Text sub = Label(menu.transform, "Corre por las islas de prisma", 32, TextAnchor.MiddleCenter);
            Place(sub.rectTransform, 0.08f, 0.70f, 0.92f, 0.76f);
            sub.color = new Color(0.25f, 0.3f, 0.36f, 1f);
            menuCoins = Label(menu.transform, "0 monedas", 36, TextAnchor.MiddleCenter);
            Place(menuCoins.rectTransform, 0.12f, 0.63f, 0.88f, 0.69f);
            menuCoins.color = new Color(0.72f, 0.48f, 0.08f, 1f);
            menuBest = Label(menu.transform, "", 28, TextAnchor.MiddleCenter);
            Place(menuBest.rectTransform, 0.1f, 0.58f, 0.9f, 0.63f);
            Button play = ButtonOf(menu.transform, "Jugar", new Color(1f, 0.35f, 0.21f, 1f), () => Raise(Play));
            Place(play.GetComponent<RectTransform>(), 0.14f, 0.45f, 0.86f, 0.55f);
            Button shopButton = ButtonOf(menu.transform, "Tienda", Ink(), () => Raise(OpenShop));
            Place(shopButton.GetComponent<RectTransform>(), 0.14f, 0.35f, 0.49f, 0.43f);
            Button missionButton = ButtonOf(menu.transform, "Misiones", Ink(), () => Raise(OpenMissions));
            Place(missionButton.GetComponent<RectTransform>(), 0.51f, 0.35f, 0.86f, 0.43f);
            Button settingsButton = ButtonOf(menu.transform, "Ajustes", new Color(0.2f, 0.45f, 0.48f, 1f), () => Raise(OpenSettings));
            Place(settingsButton.GetComponent<RectTransform>(), 0.14f, 0.25f, 0.86f, 0.33f);
            bonusButton = ButtonOf(menu.transform, "Anuncio opcional", new Color(0.95f, 0.77f, 0.26f, 1f), () => Raise(BonusAd));
            Place(bonusButton.GetComponent<RectTransform>(), 0.14f, 0.12f, 0.86f, 0.22f);
            bonusLabel = bonusButton.GetComponentInChildren<Text>();
            bonusLabel.color = Ink();
        }

        void BuildHud()
        {
            hudDistance = Label(hud.transform, "0 m", 42, TextAnchor.MiddleLeft);
            Place(hudDistance.rectTransform, 0.05f, 0.91f, 0.4f, 0.97f);
            hudDistance.color = Color.white;
            Outline(hudDistance);
            hudScore = Label(hud.transform, "0", 54, TextAnchor.MiddleCenter);
            Place(hudScore.rectTransform, 0.35f, 0.90f, 0.65f, 0.98f);
            hudScore.color = Color.white;
            Outline(hudScore);
            hudCoins = Label(hud.transform, "0", 42, TextAnchor.MiddleRight);
            Place(hudCoins.rectTransform, 0.55f, 0.91f, 0.82f, 0.97f);
            hudCoins.color = new Color(1f, 0.86f, 0.35f, 1f);
            Outline(hudCoins);
            Button pauseButton = ButtonOf(hud.transform, "II", Ink(), () => Raise(Pause));
            Place(pauseButton.GetComponent<RectTransform>(), 0.84f, 0.915f, 0.96f, 0.975f);
            tutorial = Label(hud.transform, "", 32, TextAnchor.MiddleCenter);
            Place(tutorial.rectTransform, 0.08f, 0.78f, 0.92f, 0.88f);
            tutorial.color = Color.white;
            Outline(tutorial);
            shieldCount = PowerButton(hud.transform, "Escudo", 0.06f, () => Raise(UseBoost, 0));
            magnetCount = PowerButton(hud.transform, "Imán", 0.37f, () => Raise(UseBoost, 1));
            boostCount = PowerButton(hud.transform, "Doble", 0.68f, () => Raise(UseBoost, 2));
        }

        Text PowerButton(Transform parent, string caption, float x, UnityAction action)
        {
            Button button = ButtonOf(parent, caption, new Color(0.1f, 0.12f, 0.16f, 0.82f), action);
            Place(button.GetComponent<RectTransform>(), x, 0.04f, x + 0.26f, 0.13f);
            Text count = Label(button.transform, "0", 28, TextAnchor.UpperRight);
            Place(count.rectTransform, 0.55f, 0.45f, 0.95f, 0.95f);
            count.color = new Color(1f, 0.86f, 0.35f, 1f);
            return count;
        }

        void BuildPause()
        {
            Image dim = pause.AddComponent<Image>();
            dim.color = new Color(0.05f, 0.07f, 0.1f, 0.55f);
            Panel(pause.transform, new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.68f));
            Text title = Label(pause.transform, "Pausa", 60, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0.18f, 0.54f, 0.82f, 0.64f);
            Button resume = ButtonOf(pause.transform, "Continuar", new Color(1f, 0.35f, 0.21f, 1f), () => Raise(Resume));
            Place(resume.GetComponent<RectTransform>(), 0.2f, 0.43f, 0.8f, 0.52f);
            Button toMenu = ButtonOf(pause.transform, "Menú", Ink(), () => Raise(Menu));
            Place(toMenu.GetComponent<RectTransform>(), 0.2f, 0.34f, 0.8f, 0.41f);
        }

        void BuildGameOver()
        {
            Panel(gameOver.transform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.84f));
            overTitle = Label(gameOver.transform, "Fin de la carrera", 52, TextAnchor.MiddleCenter);
            Place(overTitle.rectTransform, 0.12f, 0.70f, 0.88f, 0.80f);
            overBody = Label(gameOver.transform, "", 36, TextAnchor.MiddleCenter);
            Place(overBody.rectTransform, 0.12f, 0.52f, 0.88f, 0.70f);
            reviveButton = ButtonOf(gameOver.transform, "Revivir con anuncio", new Color(0.12f, 0.65f, 0.63f, 1f), () => Raise(Revive));
            Place(reviveButton.GetComponent<RectTransform>(), 0.16f, 0.42f, 0.84f, 0.50f);
            reviveLabel = reviveButton.GetComponentInChildren<Text>();
            doubleButton = ButtonOf(gameOver.transform, "Duplicar monedas", new Color(0.95f, 0.77f, 0.26f, 1f), () => Raise(DoubleCoins));
            Place(doubleButton.GetComponent<RectTransform>(), 0.16f, 0.33f, 0.84f, 0.41f);
            doubleLabel = doubleButton.GetComponentInChildren<Text>();
            doubleLabel.color = Ink();
            Button retry = ButtonOf(gameOver.transform, "Reintentar", new Color(1f, 0.35f, 0.21f, 1f), () => Raise(Retry));
            Place(retry.GetComponent<RectTransform>(), 0.16f, 0.23f, 0.84f, 0.31f);
            Button toMenu = ButtonOf(gameOver.transform, "Menú", Ink(), () => Raise(Menu));
            Place(toMenu.GetComponent<RectTransform>(), 0.16f, 0.14f, 0.84f, 0.21f);
        }

        void BuildShopShell()
        {
            Fill(shop, new Color(0.96f, 0.94f, 0.9f, 1f));
            Text title = Label(shop.transform, "Tienda", 56, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0.08f, 0.90f, 0.7f, 0.97f);
            Button close = ButtonOf(shop.transform, "Cerrar", Ink(), () => Raise(Menu));
            Place(close.GetComponent<RectTransform>(), 0.72f, 0.915f, 0.96f, 0.965f);
            Button characters = ButtonOf(shop.transform, "Corredores", new Color(1f, 0.35f, 0.21f, 1f), () => { shopTab = 0; Raise(OpenShop); });
            Place(characters.GetComponent<RectTransform>(), 0.04f, 0.84f, 0.34f, 0.895f);
            Button skins = ButtonOf(shop.transform, "Aspectos", new Color(0.12f, 0.65f, 0.63f, 1f), () => { shopTab = 1; Raise(OpenShop); });
            Place(skins.GetComponent<RectTransform>(), 0.35f, 0.84f, 0.65f, 0.895f);
            Button boosts = ButtonOf(shop.transform, "Potenciadores", Ink(), () => { shopTab = 2; Raise(OpenShop); });
            Place(boosts.GetComponent<RectTransform>(), 0.66f, 0.84f, 0.96f, 0.895f);
            shopContent = Scroll(shop.transform, 0.03f, 0.03f, 0.97f, 0.82f);
        }

        void BuildMissionShell()
        {
            Fill(missions, new Color(0.96f, 0.94f, 0.9f, 1f));
            Text title = Label(missions.transform, "Misiones", 56, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0.08f, 0.90f, 0.7f, 0.97f);
            Button close = ButtonOf(missions.transform, "Cerrar", Ink(), () => Raise(Menu));
            Place(close.GetComponent<RectTransform>(), 0.72f, 0.915f, 0.96f, 0.965f);
            missionContent = Scroll(missions.transform, 0.03f, 0.03f, 0.97f, 0.88f);
        }

        void BuildSettings()
        {
            Fill(settings, new Color(0.96f, 0.94f, 0.9f, 1f));
            Text title = Label(settings.transform, "Ajustes", 56, TextAnchor.MiddleCenter);
            Place(title.rectTransform, 0.08f, 0.88f, 0.7f, 0.96f);
            Button close = ButtonOf(settings.transform, "Cerrar", Ink(), () => Raise(Menu));
            Place(close.GetComponent<RectTransform>(), 0.72f, 0.90f, 0.96f, 0.95f);
            Text music = Label(settings.transform, "Música", 32, TextAnchor.MiddleLeft);
            Place(music.rectTransform, 0.08f, 0.78f, 0.4f, 0.84f);
            musicSlider = SliderOf(settings.transform, 0.08f, 0.72f, 0.92f, 0.78f, value =>
            {
                if (!suppressSlider && MusicChanged != null)
                    MusicChanged(value);
            });
            Text sfx = Label(settings.transform, "Efectos", 32, TextAnchor.MiddleLeft);
            Place(sfx.rectTransform, 0.08f, 0.64f, 0.4f, 0.70f);
            sfxSlider = SliderOf(settings.transform, 0.08f, 0.58f, 0.92f, 0.64f, value =>
            {
                if (!suppressSlider && SfxChanged != null)
                    SfxChanged(value);
            });
            vibrationToggle = ToggleOf(settings.transform, "Vibración", 0.08f, 0.48f, 0.92f, 0.56f, value =>
            {
                if (!suppressSlider && VibrationChanged != null)
                    VibrationChanged(value);
            });
            privacyButton = ButtonOf(settings.transform, "Opciones de privacidad", new Color(0.12f, 0.65f, 0.63f, 1f), () => Raise(Privacy));
            Place(privacyButton.GetComponent<RectTransform>(), 0.1f, 0.36f, 0.9f, 0.44f);
            policyButton = ButtonOf(settings.transform, "Política de privacidad", Ink(), () => Raise(OpenPolicy));
            Place(policyButton.GetComponent<RectTransform>(), 0.1f, 0.26f, 0.9f, 0.34f);
            Button erase = ButtonOf(settings.transform, "Borrar datos locales", new Color(0.55f, 0.22f, 0.22f, 1f), () => Raise(DeleteData));
            Place(erase.GetComponent<RectTransform>(), 0.1f, 0.14f, 0.9f, 0.22f);
            Text note = Label(settings.transform, "Jugar, la tienda y el guardado funcionan sin anuncios. La publicidad es opcional.", 26, TextAnchor.MiddleCenter);
            Place(note.rectTransform, 0.08f, 0.04f, 0.92f, 0.12f);
            note.color = new Color(0.25f, 0.3f, 0.36f, 1f);
        }

        void RebuildShop(Profile profile)
        {
            Clear(shopContent);
            if (shopTab == 0)
            {
                for (int i = 0; i < Catalog.Characters.Length; i++)
                    CharacterRow(profile, i);
            }
            else if (shopTab == 1)
            {
                for (int i = 0; i < Catalog.Skins.Length; i++)
                    SkinRow(profile, i);
            }
            else
            {
                for (int i = 0; i < Catalog.Boosts.Length; i++)
                    BoostRow(profile, i);
            }
        }

        void CharacterRow(Profile profile, int index)
        {
            RunnerShape item = Catalog.Characters[index];
            bool owned = profile.ownedCharacters[index] != 0;
            bool selected = profile.characterIndex == index;
            string action = selected ? "En uso" : owned ? "Elegir" : item.Price + " monedas";
            Row(shopContent, item.Name, item.Blurb, action, owned || profile.coins >= item.Price, () =>
            {
                if (owned)
                    Raise(SelectCharacter, index);
                else
                    Raise(BuyCharacter, index);
            });
        }

        void SkinRow(Profile profile, int index)
        {
            SkinTint item = Catalog.Skins[index];
            bool owned = profile.ownedSkins[index] != 0;
            bool selected = profile.skinIndex == index;
            string action = selected ? "En uso" : owned ? "Elegir" : item.Price + " monedas";
            Row(shopContent, item.Name, "Paleta de color para tu corredor.", action, owned || profile.coins >= item.Price, () =>
            {
                if (owned)
                    Raise(SelectSkin, index);
                else
                    Raise(BuySkin, index);
            });
        }

        void BoostRow(Profile profile, int index)
        {
            BoostOffer item = Catalog.Boosts[index];
            int count = index == 0 ? profile.shieldCharges : index == 1 ? profile.magnetCharges : profile.boostCharges;
            Row(shopContent, item.Name + "  ·  " + count, item.Blurb, item.Price + " monedas", profile.coins >= item.Price, () => Raise(BuyBoost, index));
        }

        void RebuildMissions(Profile profile, bool adsReady)
        {
            Clear(missionContent);
            Header(missionContent, "Hoy");
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                bool done = profile.missionProgress[i] >= profile.missionTarget[i];
                bool claimed = profile.missionClaimed[i] != 0;
                string action = claimed ? "Reclamada" : done ? "Reclamar" : profile.missionProgress[i] + "/" + profile.missionTarget[i];
                Row(missionContent, Progression.MissionTitle(profile.missionKind[i]), Progression.MissionDetail(profile, i), action, done && !claimed, () => Raise(ClaimMission, captured));
                if (claimed && profile.missionBonusClaimed[i] == 0)
                {
                    Row(missionContent, "Bonus opcional", "Mira un anuncio para repetir la recompensa de esta misión.", adsReady ? "Ver anuncio" : "Anuncio no disponible", adsReady, () => Raise(BonusMission, captured));
                }
            }
            Header(missionContent, "Logros");
            for (int i = 0; i < Catalog.Achievements.Length; i++)
            {
                int captured = i;
                AchievementDef item = Catalog.Achievements[i];
                bool unlocked = profile.achievementUnlocked[i] != 0;
                bool claimed = profile.achievementClaimed[i] != 0;
                string action = claimed ? "Reclamado" : unlocked ? "+" + item.Reward : "Bloqueado";
                Row(missionContent, item.Name, item.Blurb, action, unlocked && !claimed, () => Raise(ClaimAchievement, captured));
            }
        }

        void Header(RectTransform parent, string text)
        {
            var row = NewRow(parent, 64f);
            Text label = Label(row, text, 34, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 8f, 4f, 8f, 4f);
        }

        void Row(RectTransform parent, string title, string body, string action, bool enabled, UnityAction onClick)
        {
            var row = NewRow(parent, 150f);
            Image image = row.gameObject.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Text titleText = Label(row, title, 32, TextAnchor.UpperLeft);
            Place(titleText.rectTransform, 0.04f, 0.52f, 0.62f, 0.92f);
            Text bodyText = Label(row, body, 24, TextAnchor.UpperLeft);
            Place(bodyText.rectTransform, 0.04f, 0.08f, 0.62f, 0.52f);
            bodyText.color = new Color(0.3f, 0.34f, 0.4f, 1f);
            Button button = ButtonOf(row, action, enabled ? new Color(1f, 0.35f, 0.21f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f), onClick);
            button.interactable = enabled;
            Place(button.GetComponent<RectTransform>(), 0.64f, 0.22f, 0.96f, 0.78f);
        }

        RectTransform NewRow(RectTransform parent, float height)
        {
            RectTransform row = Rect("Row", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            return row;
        }

        RectTransform Scroll(Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform root = Rect("Scroll", parent);
            Place(root, xMin, yMin, xMax, yMax);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 24);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        void BuildToast(Transform parent)
        {
            RectTransform panel = Rect("Toast", parent);
            Place(panel, 0.1f, 0.42f, 0.9f, 0.52f);
            Image image = panel.gameObject.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.1f, 0.12f, 0.16f, 0.92f);
            toastGroup = panel.gameObject.AddComponent<CanvasGroup>();
            toastText = Label(panel, "", 30, TextAnchor.MiddleCenter);
            toastText.color = Color.white;
            Stretch(toastText.rectTransform, 16f, 8f, 16f, 8f);
            panel.gameObject.SetActive(false);
        }

        void HideAll()
        {
            boot.SetActive(false);
            menu.SetActive(false);
            hud.SetActive(false);
            pause.SetActive(false);
            gameOver.SetActive(false);
            shop.SetActive(false);
            missions.SetActive(false);
            settings.SetActive(false);
        }

        void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0f || h <= 0f)
                return;
            safe.anchorMin = new Vector2(area.x / w, area.y / h);
            safe.anchorMax = new Vector2((area.x + area.width) / w, (area.y + area.height) / h);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
        }

        GameObject Screen(string name)
        {
            RectTransform rect = Rect(name, safe);
            Stretch(rect);
            return rect.gameObject;
        }

        void Panel(Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect("Card", parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.98f, 0.96f, 0.93f, 0.96f);
        }

        void Fill(GameObject target, Color color)
        {
            Image image = target.GetComponent<Image>();
            if (image == null)
                image = target.AddComponent<Image>();
            image.color = color;
        }

        void Title(Transform parent, string text, float y)
        {
            Text label = Label(parent, text, 72, TextAnchor.MiddleCenter);
            Place(label.rectTransform, 0.06f, y, 0.94f, y + 0.08f);
            label.color = new Color(0.12f, 0.16f, 0.22f, 1f);
        }

        Text Label(Transform parent, string text, int size, TextAnchor anchor)
        {
            RectTransform rect = Rect("Text", parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = new Color(0.12f, 0.16f, 0.22f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        Button ButtonOf(Transform parent, string text, Color color, UnityAction action)
        {
            RectTransform rect = Rect("Button", parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Text label = Label(rect, text, 28, TextAnchor.MiddleCenter);
            label.color = Color.white;
            Stretch(label.rectTransform, 10f, 6f, 10f, 6f);
            return button;
        }

        Slider SliderOf(Transform parent, float xMin, float yMin, float xMax, float yMax, UnityAction<float> changed)
        {
            RectTransform rect = Rect("Slider", parent);
            Place(rect, xMin, yMin, xMax, yMax);
            Slider slider = rect.gameObject.AddComponent<Slider>();
            RectTransform fillArea = Rect("Fill", rect);
            Stretch(fillArea, 0f, 18f, 0f, 18f);
            Image fill = fillArea.gameObject.AddComponent<Image>();
            fill.color = new Color(1f, 0.35f, 0.21f, 1f);
            RectTransform handle = Rect("Handle", rect);
            handle.sizeDelta = new Vector2(36f, 36f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Ink();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(changed);
            return slider;
        }

        Toggle ToggleOf(Transform parent, string caption, float xMin, float yMin, float xMax, float yMax, UnityAction<bool> changed)
        {
            RectTransform rect = Rect("Toggle", parent);
            Place(rect, xMin, yMin, xMax, yMax);
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            Image background = rect.gameObject.AddComponent<Image>();
            background.sprite = buttonSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.9f, 0.9f, 0.88f, 1f);
            RectTransform mark = Rect("Mark", rect);
            Place(mark, 0.02f, 0.2f, 0.16f, 0.8f);
            Image markImage = mark.gameObject.AddComponent<Image>();
            markImage.color = new Color(1f, 0.35f, 0.21f, 1f);
            Text label = Label(rect, caption, 30, TextAnchor.MiddleLeft);
            Place(label.rectTransform, 0.2f, 0.1f, 0.95f, 0.9f);
            toggle.targetGraphic = background;
            toggle.graphic = markImage;
            toggle.onValueChanged.AddListener(changed);
            return toggle;
        }

        static void Clear(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        static void Raise(Action action)
        {
            if (action != null)
                action();
        }

        static void Raise(Action<int> action, int value)
        {
            if (action != null)
                action(value);
        }

        static void Raise(Action<float> action, float value)
        {
            if (action != null)
                action(value);
        }

        static void Raise(Action<bool> action, bool value)
        {
            if (action != null)
                action(value);
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Outline(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        static Color Ink()
        {
            return new Color(0.12f, 0.16f, 0.22f, 1f);
        }

        static Sprite RoundedSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= size - radius) dx = x - (size - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= size - radius) dy = y - (size - radius - 1);
                    float alpha = dx * dx + dy * dy <= radius * radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
