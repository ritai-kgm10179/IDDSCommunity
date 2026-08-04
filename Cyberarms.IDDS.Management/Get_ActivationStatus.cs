using System;

namespace Cyberarms.IDDS.Management;

[System.Management.Automation.Cmdlet(System.Management.Automation.VerbsCommon.Get, "ActivationStatus")]
public class Get_ActivationStatus : System.Management.Automation.PSCmdlet
{
    [System.Management.Automation.Parameter(Position = 0, Mandatory = false)]
    public string Options = string.Empty;

    /// <summary>
    /// Executes the process record operation.
    /// </summary>

    protected override void ProcessRecord()
    {
        if (string.IsNullOrEmpty(Options))
        {
            WriteObject(System.Reflection.Assembly.GetExecutingAssembly().Location);

        }
        else
        {
            switch (Options)
            {
                case "-v":
                    break;
            }
        }
    }

}
