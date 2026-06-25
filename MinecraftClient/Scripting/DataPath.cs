using System;
using System.IO;

namespace MinecraftClient.Scripting
{
    public static class DataPath
    {
        private static string? relativePath;
        private static string? absolutePath;

        public enum PathType
        {
            Relative,
            Absolute
        }

        /// <summary>
        /// 初始化数据目录（修正版）
        /// Initialize the data directory (Revised version)
        /// 需要手动传入脚本文件名（不含.cs扩展名）
        /// Requires manually passing the script file name (without the .cs extension)
        /// </summary>
        /// <param name="scriptName">脚本文件名（不含扩展名）Script file name (without extension)</param>
        public static void Init(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
                throw new ArgumentNullException(nameof(scriptName), "Script name cannot be null or empty(脚本名称不能为空)");

            // 计算路径 Calculate the path
            relativePath = scriptName;
            absolutePath = Path.Combine(AppContext.BaseDirectory, scriptName);

            // 创建目录 Create the directory
            if (!Directory.Exists(absolutePath))
                Directory.CreateDirectory(absolutePath);
        }

        /// <summary>
        /// 获取路径
        /// Get the path
        /// </summary>
        public static string Get(PathType pathType = PathType.Relative)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(absolutePath))
                throw new InvalidOperationException("Please call DataPath.Init() first to initialize(请先调用DataPath.Init()初始化)");

            return pathType switch
            {
                PathType.Relative => relativePath,
                PathType.Absolute => absolutePath,
                _ => throw new ArgumentOutOfRangeException(nameof(pathType))
            };
        }
    }
}
