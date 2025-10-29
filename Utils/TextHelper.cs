using Duckov.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;

#nullable disable
namespace ExtraAccessorySlots.Utils
{
    [HarmonyPatch]
    public class TextHelper : MonoBehaviour
    {
        public static TextHelper Instance { get; private set; }

        private GameObject _textTemplate;

        public void Awake()
        {
            Instance = this;
        }

        public void OnDestroy()
        {
            _textTemplate = null;

            Instance = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HealthBarManager), "Awake")]
        public static void InitTextTemplate()
        {
            if (!Instance)
            {
                LogHelper.Instance.LogError($"在 {nameof(TextHelper)}.{nameof(InitTextTemplate)} 中无法获取到 {nameof(TextHelper)} 实例");
                return;
            }
            if (Instance._textTemplate) return;
            if (!HealthBarManager.Instance)
            {
                LogHelper.Instance.LogError($"在 {nameof(TextHelper)}.{nameof(InitTextTemplate)} 中无法获取到 {nameof(HealthBarManager)} 实例");
                return;
            }

            Instance._textTemplate = Object.Instantiate(HealthBarManager.Instance.healthBarPrefab.transform.Find("Horizontal").gameObject, Instance.transform, false);
            var textTemplate = Instance._textTemplate;
            textTemplate.name = "TextTemplate";
            Object.Destroy(textTemplate.transform.Find("Image").gameObject);
            GameObject text = textTemplate.transform.Find("NameText").gameObject;
            text.name = "Text";
            text.SetActive(true);
            textTemplate.SetActive(false);
        }

        public GameObject GetText(TextConfigure configure)
        {
            if (!_textTemplate)
            {
                LogHelper.Instance.LogError($"在 {nameof(TextHelper)}::{nameof(GetText)} 中无法获取到 Text 模板");
                return null;
            }

            GameObject newText = Object.Instantiate(_textTemplate);
            if (configure.parent != null) newText.transform.SetParent(configure.parent);
            if (configure.localPosition.HasValue) newText.transform.localPosition = configure.localPosition.Value;
            if (configure.localRotation.HasValue) newText.transform.localEulerAngles = configure.localRotation.Value;
            if (configure.localScale.HasValue) newText.transform.localScale = configure.localScale.Value;
            if (!string.IsNullOrWhiteSpace(configure.textTemplateName)) newText.name = configure.textTemplateName;
            GameObject text = newText.transform.Find("Text").gameObject;
            if (!string.IsNullOrWhiteSpace(configure.textName)) text.name = configure.textName;
            text.GetComponent<TextMeshProUGUI>().text = string.IsNullOrWhiteSpace(configure.initText) ? string.Empty : configure.initText;
            newText.SetActive(configure.active);
            return newText;
        }

        public struct TextConfigure
        {
            public bool active;

            public Transform parent;

            public Vector3? localPosition;

            public Vector3? localRotation;

            public Vector3? localScale;

            public string textTemplateName;

            public string textName;

            public string initText;
        }
    }
}
