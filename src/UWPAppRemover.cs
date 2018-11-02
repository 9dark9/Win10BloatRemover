﻿using System;
using System.Management.Automation;

namespace Win10BloatRemover
{
    /**
     *  UWPAppRemover
     *  Removes the UWP which are passed into the constructor
     *  Once removal is performed, the class instance can not be reused
     */
    class UWPAppRemover
    {
        private readonly string[] appsToRemove;
        private bool removalPerformed = false;

        public UWPAppRemover(string[] appsToRemove)
        {
            this.appsToRemove = appsToRemove;
        }

        public void PerformRemoval()
        {
            if (removalPerformed)
                throw new InvalidOperationException("Apps have been already removed!");

            PowerShell psInstance = PowerShell.Create();
            foreach (string appName in appsToRemove)
            {
                // The following script uninstalls the specified app package for all users when it is found
                // and removes the package from Windows image (so that new users don't find the removed app)
                string appRemovalScript = $"$package = Get-AppxPackage -AllUsers *{appName}*;" +
                                           "if ($package) { Remove-AppxPackage -AllUsers $package;" +
                                           "$provisionedPackage = Get-AppxProvisionedPackage -Online | where {$_.DisplayName -eq \"$package.Name\"};" +
                                           "if ($provisionedPackage) { Remove-AppxProvisionedPackage -Online -PackageName $provisionedPackage.PackageName; } }";

                Console.WriteLine($"Removing {appName} app...");
                psInstance.RunScriptAndPrintOutput(appRemovalScript);   // FIXME: POWERSHELL READDS ERRORS FROM PREVIOUS SCRIPTS EXECUTIONS (unfixable?)

                // Perform post-uninstall operations only if package removal was successful
                if (psInstance.Streams.Error.Count == 0)
                {
                    Console.WriteLine($"Performing post-uninstall operations for app {appName}...");
                    PerformPostUninstallOperations(appName);
                }
                psInstance.Streams.Error.Clear();   // if we don't do it, we would check error count from previous uninstalls
            }
            psInstance.Dispose();
            removalPerformed = true;
        }

        private void PerformPostUninstallOperations(string appName)
        {
            switch (appName)
            {
                case "GetHelp":
                    Operations.RemoveComponentUsingInstallWimTweak("Microsoft-Windows-ContactSupport");
                    break;
                case "WindowsMaps":
                    SystemUtils.ExecuteWindowsCommand("sc delete MapsBroker");
                    SystemUtils.ExecuteWindowsCommand("sc delete lfsvc");
                    SystemUtils.ExecuteWindowsCommand("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /disable");
                    break;
                default:
                    Console.WriteLine("Nothing to do.");
                    break;
            }
        }
    }
}