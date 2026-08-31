using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;

/// <summary>
/// 定義雲端與電信邊界防火牆主動聯動提供者之通用介面。
/// </summary>
public interface ICloudPerimeterProvider
{
    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    CloudPerimeterType ProviderType { get; }

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 非同步將指定 IP 位址加入雲端邊界阻絕清單。
    /// </summary>
    /// <param name="ipAddress">欲阻絕的來源 IP 位址。</param>
    /// <param name="reason">阻絕原因說明。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>傳回是否成功推播至雲端邊界。</returns>
    Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// 非同步將指定 IP 位址自雲端邊界阻絕清單移除。
    /// </summary>
    /// <param name="ipAddress">欲解除阻絕的來源 IP 位址。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>傳回是否成功自雲端邊界解除。</returns>
    Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// 非同步測試與雲端邊界端點之連通性與授權憑證。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>傳回連通性測試結果與說明訊息。</returns>
    Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default);
}
