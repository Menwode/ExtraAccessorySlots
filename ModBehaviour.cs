using System;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System;
using ExtraAccessorySlots.Utils;
using System.Reflection;
using System.Collections.Generic;
using ItemStatsSystem.Data;
namespace ExtraAccessorySlots;

public class ModBehaviour : Duckov.Modding.ModBehaviour
{
    public const string HarmonyId = nameof(ExtraAccessorySlots);

    public static ModBehaviour Instance { get; private set; }

    public HarmonyHelper HarmonyHelperObj { get; private set; }

    public LinkedList<GameObject> ChildGameObject { get; } = new LinkedList<GameObject>();

    protected override void OnAfterSetup()
    {
        base.OnAfterSetup();


        // 加载 Harmony 补丁
        var assembly = Assembly.GetExecutingAssembly();
        HarmonyHelper.CheckHarmonyVersion();
        HarmonyHelper.CheckHarmonyPatchClasses(assembly);
        if (HarmonyHelperObj == null) HarmonyHelperObj = new HarmonyHelper(HarmonyId);

        ChildGameObject.AddLast(new GameObject(nameof(TextHelper), typeof(TextHelper)));
        ChildGameObject.AddLast(new GameObject(nameof(SettingUIHelper), typeof(SettingUIHelper)));
        HarmonyHelperObj.PatchAllUngrouped(assembly);

        // 应用武器slot添加补丁组
        HarmonyHelperObj.PatchGroup(assembly, nameof(WeaponSlotAdder));
        // 初始化本地化
        LocalizationHelper.Init();

        // 初始化配置（新增插槽数量 0~10）
        SlotConfigManager.Initialize();

        foreach (var gameObject in ChildGameObject)
        {
            gameObject.SetActive(false);
            gameObject.transform.SetParent(transform);
        }

        Instance = this;

        LogHelper.Instance.LogTest($"{nameof(ExtraAccessorySlots)} 已加载");
    }

    void OnDestroy()
    {
        SlotConfigManager.Release();
    }


    protected override void OnBeforeDeactivate()
    {
        base.OnBeforeDeactivate();


        // 卸载 Harmony 补丁
        HarmonyHelperObj.HarmonyInstance.UnpatchAll(HarmonyId);

        foreach (var gameObject in ChildGameObject)
        {
            if (gameObject != null) Destroy(gameObject);
        }
        ChildGameObject.Clear();
        // 移除注册的动态物品类型
        // 释放本地化事件
        LocalizationHelper.Release();
        SlotConfigManager.Release();
        Instance = null;

        LogHelper.Instance.LogTest($"{nameof(ExtraAccessorySlots)} 已卸载");
    }



}
