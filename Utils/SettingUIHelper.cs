using Duckov.Options.UI;
using Duckov.Utilities;
using HarmonyLib;
using SodaCraft.Localizations;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#nullable disable
namespace ExtraAccessorySlots.Utils
{
    public class SettingUIHelper : MonoBehaviour
    {
        public const string DefaultSettingTabButtonName = "ModSettingButton";

        public const string DefaultSettingPanelName =  "ModSettingPanel";

        public const string DefaultSettingDropdownName = "SettingDropdown";

        private GameObject _tabButtonTemplate;

        private GameObject _settingPanelTemplate;

        private GameObject _dropdownTemplate;

        public static SettingUIHelper Instance { get; private set; }

        private bool _needInit = true;

        public void Awake()
        {
            Instance = this;
        }

        public void OnDestroy()
        {
            _tabButtonTemplate = null;
            _settingPanelTemplate = null;
            _dropdownTemplate = null;

            Instance = null;
        }

        public bool InitSettingUITemplate(List<OptionsPanel_TabButton> ___tabButtons)
        {
            if (!_needInit) return true;
            if (___tabButtons == null || ___tabButtons.Count == 0)
            {
                LogHelper.Instance.LogError($"{nameof(SettingUIHelper)}::{nameof(InitSettingUITemplate)} 中的参数 {nameof(___tabButtons)} 未包含有效内容");
                return false;
            }
            OptionsPanel_TabButton commonTabButton = ___tabButtons.Find(tabButton => tabButton.gameObject.name == "Common");
            if (commonTabButton == null)
            {
                LogHelper.Instance.LogError("无法获取到名为 Common 的 OptionsPanel_TabButton 实例");
                return false;
            }
            do
            {
                // 复制常规设置选项卡按钮作为选项卡按钮模板
                _tabButtonTemplate = Instantiate(commonTabButton.gameObject, transform, false);
                _tabButtonTemplate.SetActive(false);
                _tabButtonTemplate.name = DefaultSettingTabButtonName;
                var tabFieldAccess = AccessTools.FieldRefAccess<OptionsPanel_TabButton, GameObject>("tab");
                tabFieldAccess(_tabButtonTemplate.GetComponent<OptionsPanel_TabButton>()) = null;
                LogHelper.Instance.LogTest("成功初始化设置按钮模板");
                // 复制常规设置选项卡的面板作为面板模板
                var commonTab = tabFieldAccess(commonTabButton);
                if (commonTab == null) break;
                _settingPanelTemplate = Instantiate(commonTab, transform, false);
                _settingPanelTemplate.SetActive(false);
                _settingPanelTemplate.name = DefaultSettingPanelName;
                _settingPanelTemplate.transform.DestroyAllChildren();
                LogHelper.Instance.LogTest("成功初始化设置面板模板");
                // 复制常规设置中的分辨率设置作为下拉框模板
                GameObject commonDropdown = commonTab.transform.GetChild(1)?.gameObject;
                if (commonDropdown == null) break;
                _dropdownTemplate = Instantiate(commonDropdown, transform, false);
                _dropdownTemplate.SetActive(false);
                _dropdownTemplate.name = DefaultSettingDropdownName;
                if (_dropdownTemplate.TryGetComponent<ResolutionOptions>(out var resolutionOptions))
                {
                    Destroy(resolutionOptions);
                }
                if (_dropdownTemplate.TryGetComponent<OptionsUIEntry_Dropdown>(out var dropdownComponent))
                {
                    AccessTools.FieldRefAccess<OptionsUIEntry_Dropdown, OptionsProviderBase>(dropdownComponent, "provider") = null;
                    TMP_Dropdown tmp_Dropdown = AccessTools.FieldRefAccess<OptionsUIEntry_Dropdown, TMP_Dropdown>(dropdownComponent, "dropdown");
                    tmp_Dropdown.ClearOptions();
                }
                LogHelper.Instance.LogTest("成功初始化下拉框模板");

                _needInit = false;
                return true;
            } while (false);
            LogHelper.Instance.LogError("初始化设置 UI 模板失败");
            if (_tabButtonTemplate)
            {
                Destroy(_tabButtonTemplate);
                _tabButtonTemplate = null;
            }
            if (_settingPanelTemplate)
            {
                Destroy(_settingPanelTemplate);
                _settingPanelTemplate = null;
            }
            if (_dropdownTemplate)
            {
                Destroy(_dropdownTemplate);
                _dropdownTemplate = null;
            }

            return false;
        }

        public GameObject CreateSettingPanel(OptionsPanel optionsPanel, string tabButtonName, string tabButtonLabelKey, string panelName, out OptionsPanel_TabButton tabButton)
        {
            if (!_tabButtonTemplate)
            {
                LogHelper.Instance.LogError($"在 {nameof(SettingUIHelper)}::{nameof(CreateSettingPanel)} 中无法获取到设置选项卡按钮模板");
                tabButton = null;
                return null;
            }
            if (!_settingPanelTemplate)
            {
                LogHelper.Instance.LogError($"在 {nameof(SettingUIHelper)}::{nameof(CreateSettingPanel)} 中无法获取到设置面板模板");
                tabButton = null;
                return null;
            }
            var tabs = optionsPanel.transform.Find("Tabs")?.gameObject;
            if (!tabs)
            {
                LogHelper.Instance.LogError($"在 {nameof(SettingUIHelper)}::{nameof(CreateSettingPanel)} 中无法获取到设置界面的选项卡容器");
                tabButton = null;
                return null;
            }
            var panels = optionsPanel.transform.Find("ScrollView/Viewport/Content")?.gameObject;
            if (!panels)
            {
                LogHelper.Instance.LogError($"在 {nameof(SettingUIHelper)}::{nameof(CreateSettingPanel)} 中无法获取到设置界面的面板容器");
                tabButton = null;
                return null;
            }
            GameObject tabButtonObj = Instantiate(_tabButtonTemplate, tabs.transform, false);
            tabButtonObj.SetActive(true);
            tabButtonObj.name = tabButtonName;
            // 设置选项卡按钮的标签文本
            GameObject labelGameObject = tabButtonObj.transform.Find("Label")?.gameObject;
            if (labelGameObject != null && labelGameObject.TryGetComponent<TextLocalizor>(out var textLocalizor))
            {
                textLocalizor.Key = tabButtonLabelKey;
            }
            LogHelper.Instance.LogTest("成功创建设置选项卡按钮实例");

            GameObject settingPanelObj = Instantiate(_settingPanelTemplate, panels.transform, false);
            settingPanelObj.SetActive(false);
            settingPanelObj.name = panelName;
            LogHelper.Instance.LogTest("成功创建设置面板实例");

            // 关联选项卡按钮和面板
            tabButton = tabButtonObj.GetComponent<OptionsPanel_TabButton>();
            AccessTools.FieldRefAccess<OptionsPanel_TabButton, GameObject>(tabButton, "tab") = settingPanelObj;

            return settingPanelObj;
        }


        public OptionsUIEntry_Dropdown CreateDropdown<TProvider>(GameObject panel) where TProvider : OptionsProviderBase
        {
            if (!_dropdownTemplate)
            {
                LogHelper.Instance.LogError($"在 {nameof(SettingUIHelper)}::{nameof(CreateDropdown)} 中无法获取到下拉框模板");
                return null;
            }

            var dropdownObj = Instantiate(_dropdownTemplate, panel.transform, false);
            var optionsProvider = dropdownObj.AddComponent<TProvider>();
            var dropdownComponent = dropdownObj.GetComponent<OptionsUIEntry_Dropdown>();
            AccessTools.FieldRefAccess<OptionsUIEntry_Dropdown, OptionsProviderBase>(dropdownComponent, "provider") = optionsProvider;
            dropdownObj.SetActive(true);
            dropdownObj.name = optionsProvider.Key;

            LogHelper.Instance.LogTest("成功创建下拉框实例");
            return dropdownObj.GetComponent<OptionsUIEntry_Dropdown>();
        }
    }
}
