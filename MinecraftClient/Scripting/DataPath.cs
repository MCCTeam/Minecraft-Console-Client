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
        /// 需要手动传入脚本文件名（不含.cs扩展名）
        /// </summary>
        /// <param name="scriptName">脚本文件名（不含扩展名）</param>
        public static void Init(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
                throw new ArgumentNullException(nameof(scriptName), "脚本名称不能为空");

            // 计算路径
            relativePath = scriptName;
            absolutePath = Path.Combine(AppContext.BaseDirectory, scriptName);

            // 创建目录
            if (!Directory.Exists(absolutePath))
                Directory.CreateDirectory(absolutePath);
        }

        /// <summary>
        /// 获取路径
        /// </summary>
        public static string Get(PathType pathType = PathType.Relative)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(absolutePath))
                throw new InvalidOperationException("请先调用DataPath.Init()初始化");

            return pathType switch
            {
                PathType.Relative => relativePath,
                PathType.Absolute => absolutePath,
                _ => throw new ArgumentOutOfRangeException(nameof(pathType))
            };
        }
    }
}