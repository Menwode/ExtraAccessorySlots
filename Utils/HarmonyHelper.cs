using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable disable
namespace ExtraAccessorySlots.Utils
{
    public class HarmonyHelper
    {
        public Harmony HarmonyInstance { get; }

        public Dictionary<Assembly, Dictionary<string, HashSet<Type>>> PatchesByAssembly { get; }

        public HarmonyHelper(string harmonyId)
        {
            HarmonyInstance = new Harmony(harmonyId);
            PatchesByAssembly = new Dictionary<Assembly, Dictionary<string, HashSet<Type>>>(1);
        }

        public static void CheckHarmonyVersion()
        {
            try
            {
                var versionDic = Harmony.VersionInfo(out var version);
                LogHelper.Instance.LogTest($"Harmony 版本: {version}");

                var harmonyAssembly = typeof(Harmony).Assembly;
                var assemblyVersion = harmonyAssembly.GetName().Version;
                LogHelper.Instance.LogTest($"Harmony 程序集版本: {assemblyVersion}");
            }
            catch (Exception ex)
            {
                LogHelper.Instance.LogError($"检查 Harmony 版本失败: {ex}");
            }
        }

        public static IEnumerable<Type> GetHarmonyPatchClasses(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0);
        }

        public static void CheckHarmonyPatchClasses(Assembly assembly)
        {
            var types = GetHarmonyPatchClasses(assembly);
            LogHelper.Instance.LogTest($"程序集信息：{assembly}");
            LogHelper.Instance.LogTest($"检测到的 Harmony 补丁类数量: {types.Count()}");
            foreach (var type in types)
            {
                LogHelper.Instance.LogTest($"-> Harmony 补丁类: {type.FullName}");
            }
        }

        private HashSet<Type> GetPatchesByAssemblyAndGroup(Assembly assembly, string groupName)
        {
            Dictionary<string, HashSet<Type>> patchesByGroup = null;
            HashSet<Type> patches = null;
            if (PatchesByAssembly.TryGetValue(assembly, out patchesByGroup))
            {
                if (!patchesByGroup.TryGetValue(groupName, out patches))
                {
                    patches = new HashSet<Type>();
                    patchesByGroup.Add(groupName, patches);
                }
            }
            else
            {
                patches = new HashSet<Type>();
                patchesByGroup = new Dictionary<string, HashSet<Type>>()
                {
                    [groupName] = patches
                };
                PatchesByAssembly.Add(assembly, patchesByGroup);
            }
            return patches;
        }

        public void PatchAllUngrouped(Assembly assembly)
        {
            var types = GetHarmonyPatchClasses(assembly).Where(type =>
            {
                var categoryAttr = type.GetCustomAttribute<PatchGroup>();
                return categoryAttr == null;
            });
            var patches = GetPatchesByAssemblyAndGroup(assembly, string.Empty);

            foreach (var type in types)
            {
                try
                {
                    if (patches.Contains(type)) continue;
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                    patches.Add(type);
                    LogHelper.Instance.LogTest($"已加载补丁: {type.Name} (无组别)");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.LogError($"加载补丁失败: {type.Name}, 错误: {ex.Message}");
                }
            }
        }

        public void UnpatchAllUngrouped(Assembly assembly)
        {
            var patches = GetPatchesByAssemblyAndGroup(assembly, string.Empty);

            foreach (var type in patches.ToArray())
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(type).Unpatch();
                    patches.Remove(type);
                    LogHelper.Instance.LogTest($"已卸载补丁: {type.Name} (无组别)");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.LogError($"卸载补丁失败: {type.Name}, 错误: {ex.Message}");
                }
            }

            if (patches.Count == 0)
            {
                if (PatchesByAssembly.TryGetValue(assembly, out var patchesByGroup))
                {
                    patchesByGroup.Remove(string.Empty);
                }
            }
        }

        public void PatchGroup(Assembly assembly, string group)
        {
            var types = GetHarmonyPatchClasses(assembly).Where(type =>
            {
                var categoryAttr = type.GetCustomAttribute<PatchGroup>();
                return categoryAttr != null && categoryAttr.GroupName == group;
            });
            var patches = GetPatchesByAssemblyAndGroup(assembly, group);
            foreach (var type in types)
            {
                try
                {
                    if (patches.Contains(type)) continue;
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                    patches.Add(type);
                    LogHelper.Instance.LogTest($"已加载补丁: {type.Name} (组别: {group})");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.LogError($"加载补丁失败: {type.Name}, 错误: {ex.Message}");
                }
            }
        }

        public void UnpatchGroup(Assembly assembly, string group)
        {
            var patches = GetPatchesByAssemblyAndGroup(assembly, group);

            foreach (var type in patches.ToArray())
            {
                try
                {
                    HarmonyInstance.CreateClassProcessor(type).Unpatch();
                    patches.Remove(type);
                    LogHelper.Instance.LogTest($"已卸载补丁: {type.Name} (组别: {group})");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.LogError($"卸载补丁失败: {type.Name}, 错误: {ex.Message}");
                }
            }

            if (patches.Count == 0)
            {
                if (PatchesByAssembly.TryGetValue(assembly, out var patchesByGroup))
                {
                    patchesByGroup.Remove(group);
                }
            }
        }
    }
}
