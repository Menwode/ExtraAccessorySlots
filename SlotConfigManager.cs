using System.Collections.Generic;
using SodaCraft.Localizations;
using UnityEngine;

namespace ExtraAccessorySlots
{
    // 负责管理“新增插槽数量”的配置（0~10）
    public static class SlotConfigManager
    {
        public const string ModName = "ExtraAccessorySlots";
        private const string KeySlotCount = "extraSlotCount";

        private static int _extraSlotCount = 1; // 默认 1 个
        private static bool _registered = false; // 防止重复添加 UI / 事件
        private static int _lastSlotCount = 1;   // 上次生效的数量

        public static int ExtraSlotCount
        {
            get
            {
                if (_extraSlotCount < 0) return 0;
                if (_extraSlotCount > 10) return 10;
                return _extraSlotCount;
            }
        }

        public static void Initialize()
        {
            if (_registered) return; // 已经注册过，直接返回

            if (!ModConfigAPI.IsAvailable())
            {
                // ModConfig 未加载，保留默认值
                return;
            }

            // 语言判定（中文/英文）
            var isChinese = LocalizationManager.CurrentLanguage == SystemLanguage.Chinese
                             || LocalizationManager.CurrentLanguage == SystemLanguage.ChineseSimplified
                             || LocalizationManager.CurrentLanguage == SystemLanguage.ChineseTraditional;

            // 使用下拉列表提供 0~10 的选项
            var options = new SortedDictionary<string, object>();
            for (int i = 0; i <= 10; i++)
            {
                options.Add(i.ToString(), i);
            }

            // 添加 UI（确保只添加一次）
            ModConfigAPI.SafeAddDropdownList(
                ModName,
                KeySlotCount,
                isChinese ? "新增插槽数量" : "Additional WeaponChip Slots",
                options,
                typeof(int),
                _extraSlotCount
            );

            // 监听配置变动（只添加一次）
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnOptionsChanged);
            _registered = true;

            // 初次加载当前配置值
            LoadFromModConfig();
            _lastSlotCount = _extraSlotCount;
        }

        public static void Release()
        {
            if (!_registered) return;
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnOptionsChanged);
            _registered = false;
        }

        private static void OnOptionsChanged(string changedKey)
        {
            // 只处理本 Mod 的键
            if (!string.IsNullOrEmpty(changedKey) && changedKey.StartsWith(ModName + "_"))
            {
                int oldCount = _extraSlotCount;
                LoadFromModConfig();
                int newCount = _extraSlotCount;

                // 当减少slot时，触发全局处理：卸下多余插槽的物品并转仓库
                if (newCount != oldCount)
                {
                    WeaponSlotAdder.ApplyDecreaseGlobally(oldCount, newCount);
                }

                _lastSlotCount = newCount;
            }
        }

        public static void LoadFromModConfig()
        {
            _extraSlotCount = ModConfigAPI.SafeLoad<int>(ModName, KeySlotCount, _extraSlotCount);
            if (_extraSlotCount < 0) _extraSlotCount = 0;
            if (_extraSlotCount > 10) _extraSlotCount = 10;
        }
    }
}