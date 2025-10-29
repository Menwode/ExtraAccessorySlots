using HarmonyLib;
using ExtraAccessorySlots.Utils;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using System.Collections.Generic;
using UnityEngine;
using Duckov.Utilities;
using System;
using System.Linq;
using System.Reflection;

#nullable disable
namespace ExtraAccessorySlots
{
    [HarmonyPatch]
    [PatchGroup(nameof(WeaponSlotAdder))] // 添加到补丁组
    public class WeaponSlotAdder
    {
        // 补丁Item.Initialize方法，在物品初始化时检查并添加/移除slot
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Item), "Initialize")]
        public static void AfterItemInitialize(Item __instance)
        {

            // 检查物品是否有武器tag
            if (__instance.Tags != null && HasWeaponTag(__instance.Tags))
            {

                // 确保物品有SlotCollection组件
                if (__instance.Slots == null)
                {
                    // 如果没有SlotCollection，创建一个
                    __instance.CreateSlotsComponent();
                }

                // 目标数量（0~10）
                int targetCount = SlotConfigManager.ExtraSlotCount;

                if (__instance.Slots != null)
                {
                    EnforceSlotCountWithTransfer(__instance, targetCount);
                }
            }
        }
        // 检查是否有武器相关的tag
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Slot), "CheckAbleToPlug")]
        public static bool BeforeSlotCheckAbleToPlug(Slot __instance, Item otherItem, ref bool __result)
        {
            if (!string.IsNullOrEmpty(__instance.Key) && __instance.Key.StartsWith("WeaponChip"))
            {
                __result = HasRequireTag(__instance.requireTags,__instance.excludeTags,otherItem.Tags);

                return false;
            }
            return true;
        }
        private static bool HasRequireTag(List<Tag> requireTags,List<Tag> excludeTags, TagCollection tags)
        {
            if (excludeTags != null && excludeTags.Count > 0)
            {
                foreach (var tag in excludeTags)
                {
                    if (tag != null && tags.Contains(tag))
                    {
                        return false;
                    }
                }
            }
            foreach (var tag in tags)
            {
                if (tag != null && requireTags.Contains(tag))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool HasWeaponTag(TagCollection tags)
        {
            // 这里需要根据游戏中实际的武器tag名称来判断
            // 例如，可能的tag名称包括"Weapon"、"Gun"、"Melee"等
            foreach (var tag in tags)
            {
                if (tag != null && (tag.name == "Weapon" || tag.name == "Gun" || tag.name == "Melee"))
                {
                    return true;
                }
            }
            return false;
        }

        // 判断是否是武器配件插槽（支持主名+序号）
        private static bool IsWeaponChipSlot(Slot s)
        {
            return s != null && !string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("WeaponChip");
        }

        // 计算下一个插槽序号（查找已有最大序号并+1）
        private static int GetNextWeaponChipIndex(SlotCollection slots)
        {
            int max = 0;
            foreach (var s in slots)
            {
                if (s == null || string.IsNullOrEmpty(s.Key)) continue;
                if (!s.Key.StartsWith("WeaponChip")) continue;
                int baseLen = "WeaponChip".Length;
                if (s.Key.Length > baseLen && s.Key[baseLen] == '_')
                {
                    var suffix = s.Key.Substring(baseLen + 1);
                    if (int.TryParse(suffix, out var n))
                    {
                        if (n > max) max = n;
                    }
                }
            }
            return max + 1;
        }

        // 全局应用减少：扫描场景中的武器，移除多余插槽并转仓库
        public static void ApplyDecreaseGlobally(int oldCount, int newCount)
        {
            try
            {
                var items = Resources.FindObjectsOfTypeAll<Item>();
                foreach (var it in items)
                {
                    
                    if (it != null && it.Tags != null && HasWeaponTag(it.Tags) && it.Slots != null)
                    {
                        EnforceSlotCountWithTransfer(it, newCount);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"全局应用插槽减少时异常: {ex}");
            }
        }

        // 统一执行添加/移除逻辑；在移除时尝试卸下物品并转仓库
        private static void EnforceSlotCountWithTransfer(Item item, int targetCount)
        {
            try
            {
                // 当前已有的 WeaponChip 插槽列表
                var weaponChipSlots = new List<Slot>();
                foreach (var s in item.Slots)
                {
                    if (IsWeaponChipSlot(s)) weaponChipSlots.Add(s);
                }
                int existingCount = weaponChipSlots.Count;

                // 需要添加的数量
                int toAdd = targetCount - existingCount;
                while (toAdd > 0)
                {
                    var idx = GetNextWeaponChipIndex(item.Slots);
                    Slot newSlot = new Slot($"WeaponChip_{idx}")
                    {
                        SlotIcon = null,
                    };

                    Tag tag1 = TagUtilities.TagFromString("Accessory");
                    Tag tag2 = TagUtilities.TagFromString("Scope");
                    Tag tag3 = TagUtilities.TagFromString("Gem");
                    newSlot.requireTags.Add(tag1);
                    newSlot.requireTags.Add(tag3);
                    newSlot.excludeTags.Add(tag2);

                    item.Slots.Add(newSlot);
                    newSlot.Initialize(item.Slots);
                    toAdd--;
                }

                // 需要移除的数量（从后向前，先空后实，且将物品发送至仓库）
                int toRemove = existingCount - targetCount;
                if (toRemove > 0)
                {
                    // 倒序优先移除空插槽
                    for (int i = weaponChipSlots.Count - 1; i >= 0 && toRemove > 0; i--)
                    {
                        var slot = weaponChipSlots[i];
                        if (slot == null) continue;
                        if (slot.Content == null||!IsPlayerItem(slot.Content))
                        {
                            SafeRemoveSlot(item, slot);
                            toRemove--;
                        }else{
                            var unpluggedItem = slot.Unplug();
                            if (unpluggedItem != null)
                            {
                                ItemUtilities.SendToPlayerStorage(unpluggedItem);
                            }
                            SafeRemoveSlot(item, slot);
                            toRemove--;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"调整武器插槽数量时异常: {ex}");
            }
        }
        private static bool IsPlayerItem(Item item)
        {
            if (item == null) return false;
            if (item.IsInPlayerCharacter() || item.IsInPlayerStorage()) return true;
            return false;
        }

        private static void SafeRemoveSlot(Item item, Slot slot)
        {
            try
            {
                if (item?.Slots == null || slot == null) return;
                // 仅当该插槽当前还在集合中时才删除
                if (!item.Slots.Contains(slot)) return;
                item.Slots.Remove(slot);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"移除插槽时异常: {ex}");
            }
        }

        private static Item TryGetPluggedItem(Slot slot)
        {
            try
            {
                if (slot == null) return null;
                // 常见属性/字段名尝试
                var type = slot.GetType();
                var prop = type.GetProperty("PluggedItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && typeof(Item).IsAssignableFrom(prop.PropertyType))
                {
                    return prop.GetValue(slot) as Item;
                }
                var field = type.GetField("pluggedItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && typeof(Item).IsAssignableFrom(field.FieldType))
                {
                    return field.GetValue(slot) as Item;
                }
                // 其他候选
                prop = type.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && typeof(Item).IsAssignableFrom(prop.PropertyType))
                {
                    return prop.GetValue(slot) as Item;
                }
                field = type.GetField("item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && typeof(Item).IsAssignableFrom(field.FieldType))
                {
                    return field.GetValue(slot) as Item;
                }
            }
            catch {}
            return null;
        }

        private static void TryUnplugItem(Slot slot, Item plugged)
        {
            try
            {
                if (slot == null || plugged == null) return;
                var type = slot.GetType();
                // 优先调用显式卸下方法
                var m = type.GetMethod("Unplug", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Item) }, null);
                if (m != null)
                {
                    m.Invoke(slot, new object[] { plugged });
                    return;
                }
                m = type.GetMethod("UnplugItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null)
                {
                    m.Invoke(slot, null);
                    return;
                }
                m = type.GetMethod("Detach", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null)
                {
                    m.Invoke(slot, null);
                    return;
                }
                m = type.GetMethod("RemoveItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null)
                {
                    m.Invoke(slot, null);
                    return;
                }

                // 没有找到方法时，尝试把字段置空（兜底）
                var field = type.GetField("pluggedItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(slot, null);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"卸下插槽物品时异常: {ex}");
            }
        }

        private static void TrySendToWarehouse(Item item)
        {
            try
            {
                if (item == null) return;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                // 查找可能的仓库/背包/库存类型
                var candidateTypes = assemblies.SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                    })
                    .Where(t => t != null && t.Name != null && (
                        t.Name.Contains("Inventory") || t.Name.Contains("Storage") || t.Name.Contains("Stash") || t.Name.Contains("Backpack") || t.Name.Contains("Container")
                    ));

                foreach (var t in candidateTypes)
                {
                    // 找到可添加物品的方法
                    var addMethods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                        .Where(m => (m.Name.Contains("Add") || m.Name.Contains("AddItem") || m.Name.Contains("TryAdd"))
                                    && m.GetParameters().Length >= 1
                                    && typeof(Item).IsAssignableFrom(m.GetParameters()[0].ParameterType));

                    foreach (var m in addMethods)
                    {
                        object instance = null;
                        // 单例/静态实例
                        var instProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (instProp != null) instance = instProp.GetValue(null);
                        var instField = t.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (instance == null && instField != null) instance = instField.GetValue(null);

                        // 尝试在场景中寻找该类型的对象
                        if (instance == null && typeof(UnityEngine.Object).IsAssignableFrom(t))
                        {
                            var found = UnityEngine.Object.FindObjectOfType(t);
                            if (found != null) instance = found;
                        }

                        // 静态方法或有实例时调用
                        if (m.IsStatic || instance != null)
                        {
                            var args = new object[m.GetParameters().Length];
                            args[0] = item;
                            m.Invoke(m.IsStatic ? null : instance, args);
                            LogHelper.Instance.LogTest($"已尝试将物品 {item.DisplayName} 转入仓库/背包: {t.FullName}.{m.Name}");
                            return;
                        }
                    }
                }

                LogHelper.Instance.LogWarning($"未找到仓库/背包API，无法自动转移物品: {item.DisplayName}");
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"尝试转移物品到仓库时异常: {ex}");
            }
        }

        // 在初始化时注册本地化键的模板
        public static void RegisterLocalizationKeys()
        {
            try
            {
                var group = "Tags";
                var key = "Tag_WeaponChip";
                var value = "武器配件"; // 显示名
                var descKey = "Tag_WeaponChip_Desc";
                var descValue = "允许安装武器配件和宝石"; // 描述文本

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var candidates = assemblies.SelectMany(a => a.GetTypes())
                    .Where(t => t.Namespace != null &&
                                (t.Namespace.Contains("SodaCraft") || t.Namespace.Contains("MiniLocalizor")) &&
                                (t.Name.Contains("Localization") || t.Name.Contains("Localizor")));

                bool registered = false;
                foreach (var t in candidates)
                {
                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach (var m in methods)
                    {
                        var ps = m.GetParameters();
                        if (ps.Length == 3 && ps.All(p => p.ParameterType == typeof(string)) &&
                            (m.Name.Contains("Set") || m.Name.Contains("Add") || m.Name.Contains("Register")))
                        {
                            try
                            {
                                m.Invoke(null, new object[] { group, key, value });
                                m.Invoke(null, new object[] { group, descKey, descValue });
                                registered = true;
                                LogHelper.Instance.LogTest("已注册 Tag_WeaponChip 的本地化键");
                                break;
                            }
                            catch { }
                        }
                    }
                    if (registered) break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogWarning($"注册本地化键时异常: {ex}");
            }
        }
    }
}