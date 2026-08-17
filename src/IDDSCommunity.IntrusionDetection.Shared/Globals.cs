namespace IDDSCommunity.IntrusionDetection.Shared;

public class Globals
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string APPLICATION_NAME = "IDDS Community";
    /// <summary>
    /// Windows service key used by the community distribution.
    /// </summary>
    public const string WINDOWS_SERVICE_NAME = "IDDSCommunityProtection";
    /// <summary>
    /// User-facing Windows service name.
    /// </summary>
    public const string WINDOWS_SERVICE_DISPLAY_NAME = "IDDS Community Protection Service";
    /// <summary>
    /// Plugin directory name
    /// </summary>
    public const string PLUGIN_DIRECTORY_NAME = "Plugins";
    /// <summary>
    /// Windows firewall group name of blocking rules
    /// </summary>
    public const string IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME = "IDDS Community";
    /// <summary>
    /// Windows firewall rule name for all blocked clients
    /// </summary>
    public const string IDDSCOMMUNITY_WINDOWS_IDS_RULE_NAME = "Blocked by IDDS Community";
    /// <summary>
    /// Windows event log source name forIntrusion Detectionlogs
    /// </summary>
    public const string IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE = "IDDS Community";
    /// <summary>
    /// Windows event log name forIntrusion Detectionlogs.
    /// </summary>
    public const string IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME = "IDDS Community";
    /// <summary>
    /// 由安裝程式建立之本機群組名稱；其成員可在非提升權限狀態下讀取受 DPAPI 保護的資料庫金鑰檔案。
    /// </summary>
    public const string IDDSCOMMUNITY_OPERATORS_GROUP_NAME = "IDDSCommunityOperators";


    // Event Log Information
    /// <summary>
    /// Windows event log category for configuration errors and information
    /// </summary>
    public const short IDDSCOMMUNITY_LOG_CATEGORY_CONFIGURATION = 1000;

    public const short IDDSCOMMUNITY_LOG_CATEGORY_SECURITY = 1001;

    public const short IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN = 1200;

    public const short IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME = 1400;


    public const int IDDSCOMMUNITY_EVENT_ID_INFORMATION = 1000;
    /// <summary>
    /// Windows event log
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_CREATED = 4001;

    public const int IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_ALTERED = 4002;
    /// <summary>
    /// A file cannot be saved to disk
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_PERSISTANCE_ERROR = 5000;
    /// <summary>
    /// Windows event log id for configuration file not found
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR = 9000;
    /// <summary>
    /// Event log id when calling delegate with invalid parameters
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL = 9001;
    /// <summary>
    /// Windows event log id for plugin error
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_PLUGIN_ERROR = 9002;
    /// <summary>
    /// Windows event log id for plugin error during initialisation
    /// </summary>
    public const int IDDSCOMMUNITY_EVENT_ID_PLUGIN_LOAD_ERROR = 9003;



}
