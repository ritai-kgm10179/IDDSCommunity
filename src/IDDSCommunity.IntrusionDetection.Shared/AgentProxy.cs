using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 代表安全性代理程式擴充元件之遠端呼叫代理包裝類別。
/// </summary>
public class AgentProxy : MarshalByRefObject, IAgentPlugin
{
        /// <summary>
    /// 當 AttackDetected 時引發之事件。
    /// </summary>
public event AttackDetectedHandler? AttackDetected;

    private IAgentPlugin? _agent;
    private readonly AgentPluginLoadContext loadContext;
    private bool disposed;
    /// <summary>
    /// 初始化 <see cref="AgentProxy"/> class的新執行個體。
    /// </summary>
    /// <param name="pluginRoot">The trusted plug-in directory.</param>
    /// <param name="assemblyFilename">assembly filename參數。</param>
    /// <param name="typeName">type name參數。</param>
    public AgentProxy(string pluginRoot, string assemblyFilename, string typeName)
    {
        string pluginPath = PluginPathValidator.Validate(pluginRoot, assemblyFilename);
        loadContext = new AgentPluginLoadContext(pluginPath);
        System.Reflection.Assembly assembly = loadContext.LoadFromAssemblyPath(pluginPath);
        Type pluginType = assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unable to resolve the requested agent plugin type."));
        object? instance = Activator.CreateInstance(pluginType);
        _agent = instance as IAgentPlugin
            ?? throw new InvalidOperationException(string.Format(Localization.Strings.Get("Unable to create agent plugin '{0}' from '{1}'."), typeName, assemblyFilename));
        _agent.AttackDetected += agent_AttackDetected;
    }
    /// <summary>
    /// 處理 attack detected 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="data">The event data.</param>
    private void agent_AttackDetected(object sender, INotificationEventArgs data)
    {
        foreach (AttackDetectedHandler handler in AttackDetected?.GetInvocationList() ?? [])
        {
            try
            {
                handler(sender, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(Localization.Strings.Format("AgentProxy AttackDetected subscriber failed: {0}", ex.GetType().Name));
            }
        }
    }
    /// <summary>
    /// Starts requested operation.
    /// </summary>
    public void Start() => GetAgent().Start();
    /// <summary>
    /// Stops requested operation.
    /// </summary>
    public void Stop() => GetAgent().Stop();
    /// <summary>
    /// 執行pause作業。
    /// </summary>
    public void Pause() => GetAgent().Pause();
    /// <summary>
    /// 執行continue作業。
    /// </summary>
    public void Continue() => GetAgent().Continue();
    /// <summary>
    /// Determines whether n pause.
    /// </summary>
    /// <returns>若n pause傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanPause() => GetAgent().CanPause();
    /// <summary>
    /// Determines whether n continue.
    /// </summary>
    /// <returns>若n continue傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanContinue() => GetAgent().CanContinue();

        /// <summary>
    /// 取得或設定 IsPaused。
    /// </summary>
public bool IsPaused
    {
        get => GetAgent().IsPaused;
        set => GetAgent().IsPaused = value;
    }

        /// <summary>
    /// 取得或設定 IsRunning。
    /// </summary>
public bool IsRunning => GetAgent().IsRunning;

        /// <summary>
    /// 取得或設定 Configuration。
    /// </summary>
public IAgentConfiguration Configuration
    {
        get => GetAgent().Configuration;
        set => GetAgent().Configuration = value;
    }
    /// <summary>
    /// 執行dispose作業。
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        if (_agent is not null)
            _agent.AttackDetected -= agent_AttackDetected;
        _agent = null;
        loadContext.Unload();
        disposed = true;
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// 取得 active plug-in instance or rejects access after unload.
    /// </summary>
    /// <returns>作用中的擴充元件實體。</returns>
    private IAgentPlugin GetAgent() => _agent ?? throw new ObjectDisposedException(nameof(AgentProxy));
}
