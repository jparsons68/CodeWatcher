﻿using System;
using System.Windows.Forms;

namespace CodeWatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjU5NzIyQDMxMzgyZTMxMmUzMFJwM0s3QkJhSldnaFNiRDh6UzJoTityeWsrajJoMkFPejhjaUYrZElUMFE9");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
