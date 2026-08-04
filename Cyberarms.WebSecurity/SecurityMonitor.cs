using System;
using System.Web;
using System.Reflection;



namespace Cyberarms.WebSecurity;

public class SecurityMonitor : IHttpModule
{
    const string VAR_NAME_FAILED_LOGIN_RDWEB = "bFailedLogon";
    const string VAR_NAME_FAILED_LOGIN_DEFAULT = "bCyberarmsLoginFailed";
    const string EVENT_LOG_MESSAGE = "Cyberarms Web Security Monitor has recognized an unsuccessful login from computer {0} [IP = '{1}'] \nUser agent: {2}\nRequested url: {3}";

    /// <summary>
    /// Executes the dispose operation.
    /// </summary>

    public void Dispose()
    {

    }


    /// <summary>
    /// Executes the init operation.
    /// </summary>
    /// <param name="context">The context value.</param>

    public void Init(HttpApplication context) => context.PostRequestHandlerExecute += context_PostRequestHandlerExecute;

    /// <summary>
    /// Handles the post request handler execute event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void context_PostRequestHandlerExecute(object? sender, EventArgs e)
    {
        try
        {
            bool bFailedLoginDetected = false;
            if (sender != null)
            {
                HttpContext? context = ((HttpApplication)sender).Context;
                if (context != null)
                {
                    IHttpHandler? handler = context.Handler;
                    if (handler is null)
                        return;
                    foreach (FieldInfo fi in handler.GetType().GetFields())
                    {
                        if (fi.Name == VAR_NAME_FAILED_LOGIN_DEFAULT || fi.Name == VAR_NAME_FAILED_LOGIN_RDWEB)
                        {
                            if (fi.GetValue(handler) is object value && bool.TryParse(value.ToString(), out bool bFailed))
                            {
                                if (bFailed) bFailedLoginDetected = true;
                            }
                        }
                    }
                    if (bFailedLoginDetected)
                    {
                        // write login failed to application event log
                        System.Diagnostics.EventLog.WriteEntry("Application",
                            string.Format(EVENT_LOG_MESSAGE, context.Request.UserHostName, context.Request.UserHostAddress, context.Request.UserAgent, context.Request.Url),
                            System.Diagnostics.EventLogEntryType.FailureAudit, 4625);
                    }
                }
            }
        }
        catch
        {
            // avoid errors caused by this module
        }
    }



}
