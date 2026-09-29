using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BallDropParty.Gameplay;

namespace BusBallJam.UI
{
    public class LosePanel : Panel
    {
        public class Data
        {
            public int level;
            public System.Action retry;
        }

        [Header("UI Elements")]
        [SerializeField] private Text textLevel;
        [SerializeField] private TextMeshProUGUI textLevelTMP;
        [SerializeField] private Button btnRetry;

        private Data _data;

        public override void OnSetup()
        {
            base.OnSetup();
            EnsureUI();

            if (btnRetry != null)
            {
                btnRetry.onClick.RemoveAllListeners();
                btnRetry.onClick.AddListener(OnRetryClicked);
            }
        }

        public void Setup(Data data)
        {
            _data = data;
            EnsureUI();
            int lvl = data != null ? data.level : (LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelId : 1);
            string msg = $"LEVEL {lvl} FAILED!";
            if (textLevelTMP != null)
            {
                textLevelTMP.text = msg;
            }
            if (textLevel != null)
            {
                textLevel.text = msg;
            }
        }

        public void OnRetryClicked()
        {
            Close();
            if (_data != null && _data.retry != null)
            {
                _data.retry.Invoke();
            }
            else if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartLevel();
            }
        }

        private void EnsureUI()
        {
            if (contentRoot == null)
            {
                contentRoot = transform.Find("Content") ?? transform;
            }

            if (textLevelTMP == null)
            {
                textLevelTMP = GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (textLevel == null && textLevelTMP == null)
            {
                textLevel = GetComponentInChildren<Text>(true);
            }
            if (btnRetry == null)
            {
                btnRetry = GetComponentInChildren<Button>(true);
            }

            if (textLevel == null && textLevelTMP == null && btnRetry == null)
            {
                BuildDefaultLoseCard();
            }
        }

        private void BuildDefaultLoseCard()
        {
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 750);
            rt.anchoredPosition = Vector2.zero;

            var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.2f, 0.96f);

            Font font = GetDefaultFont();

            // Title
            if (transform.Find("Title") == null)
            {
                var titleGo = new GameObject("Title");
                titleGo.transform.SetParent(transform, false);
                var t = titleGo.AddComponent<Text>();
                t.text = "LEVEL FAILED";
                t.font = font;
                t.fontSize = 54;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.9f, 0.25f, 0.25f);
                var trt = titleGo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.8f);
                trt.anchorMax = new Vector2(0.5f, 0.8f);
                trt.sizeDelta = new Vector2(600, 100);
            }

            // Subtitle textLevel
            if (textLevel == null)
            {
                var subGo = transform.Find("Subtitle")?.gameObject ?? new GameObject("Subtitle");
                subGo.transform.SetParent(transform, false);
                textLevel = subGo.GetComponent<Text>() ?? subGo.AddComponent<Text>();
                textLevel.text = "LEVEL 1 FAILED!";
                textLevel.font = font;
                textLevel.fontSize = 36;
                textLevel.alignment = TextAnchor.MiddleCenter;
                textLevel.color = Color.white;
                var srt = subGo.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.55f);
                srt.anchorMax = new Vector2(0.5f, 0.55f);
                srt.sizeDelta = new Vector2(600, 80);
            }

            // Button
            if (btnRetry == null)
            {
                var btnGo = transform.Find("BtnRetry")?.gameObject ?? new GameObject("BtnRetry");
                btnGo.transform.SetParent(transform, false);
                var btnImg = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.95f, 0.4f, 0.2f);
                btnRetry = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0.25f);
                brt.anchorMax = new Vector2(0.5f, 0.25f);
                brt.sizeDelta = new Vector2(480, 110);

                var btGo = btnGo.transform.Find("Text")?.gameObject ?? new GameObject("Text");
                btGo.transform.SetParent(btnGo.transform, false);
                var bt = btGo.GetComponent<Text>() ?? btGo.AddComponent<Text>();
                bt.text = "RESTART";
                bt.font = font;
                bt.fontSize = 40;
                bt.fontStyle = FontStyle.Bold;
                bt.alignment = TextAnchor.MiddleCenter;
                bt.color = Color.white;
                var btrt = btGo.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;
            }
        }

        private Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return font;
        }
    }
}
