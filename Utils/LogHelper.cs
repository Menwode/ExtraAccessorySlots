using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace ExtraAccessorySlots.Utils
{
    public class LogHelper
    {
        private static readonly Lazy<LogHelper> _instance =
            new Lazy<LogHelper>(() => new LogHelper(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static LogHelper Instance => _instance.Value;

        // 检测是否在 BepInEx 环境下运行
        public static bool IsBepInExEnvironment { get; } = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "BepInEx.Core");

        public void LogTest(string message, ConsoleColor color = ConsoleColor.Cyan)
        {

            if (IsBepInExEnvironment)
            {
                // BepInEx 环境下使用彩色日志
                Debug.LogWarning(message);
            }
            else
            {
                Console.ResetColor(); // 重置颜色
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor(); // 重置颜色
            }
        }

        private LogHelper()
        {
            if (IsBepInExEnvironment)
            {
                LogTest("检测到 BepInEx 环境");
            }
            else
            {
                LogTest("未检测到 BepInEx 环境");
            }
        }

        public void Log(object message)
        {
            Debug.Log(message);
        }

        public void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public void LogError(object message)
        {
            Debug.LogError(message);
        }
    }
}
