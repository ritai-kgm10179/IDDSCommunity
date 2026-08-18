using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義擴充元件引發例外狀況之來源階段列舉。
/// </summary>
public enum PluginExceptionSource
{
        /// <summary>
    /// 定義 Init 列舉值。
    /// </summary>
Init = 0,
        /// <summary>
    /// 定義 Load 列舉值。
    /// </summary>
Load = 100,
        /// <summary>
    /// 定義 Configuration 列舉值。
    /// </summary>
Configuration = 200,
        /// <summary>
    /// 定義 ServiceAction 列舉值。
    /// </summary>
ServiceAction = 300,
        /// <summary>
    /// 定義 ExecuteAction 列舉值。
    /// </summary>
ExecuteAction = 400,
        /// <summary>
    /// 定義 Unload 列舉值。
    /// </summary>
Unload = 500
}

/// <summary>
/// 代表擴充元件執行異常時之事件引數物件。
/// </summary>
public class PluginExceptionArguments
{
        /// <summary>
    /// 取得或設定 AssemblyName。
    /// </summary>
public string AssemblyName { get; set; } = string.Empty;
        /// <summary>
    /// 取得或設定 ModuleName。
    /// </summary>
public string? ModuleName { get; set; }
        /// <summary>
    /// 取得或設定 Exception。
    /// </summary>
public Exception Exception { get; set; } = new InvalidOperationException();
        /// <summary>
    /// 取得或設定 Source。
    /// </summary>
public PluginExceptionSource Source { get; set; }
}
