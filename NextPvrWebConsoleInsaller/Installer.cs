﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.AccessControl;


namespace NextPvrWebConsole
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }
    
        private Guid AppId = new Guid("3C12BF4E-DF3A-4D50-8391-3EB051409901");

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

             //unregister the service
            string regapp = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), @"UltiDev\Web Server\UWS.RegApp.exe");
            if (!File.Exists(regapp))
                regapp = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), @"UltiDev\Web Server\UWS.RegApp.exe");
            if (!File.Exists(regapp))
                throw new Exception("Failed to located UltiDev web server.");

            System.Diagnostics.Process.Start(regapp, String.Format("/u /AppID=\"{0}\"", AppId.ToString()));

        }

        public override void Install(IDictionary stateSaver)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("Started");
            //try
            //{
                base.Install(stateSaver);

                string installDir = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.Parent.FullName;
                //string installDir = Context.Parameters["DP_TargetDir"];
                log.AppendLine("installDir: " + installDir);
                string appDataDir = Path.Combine(installDir, "App_Data");
                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);
                GrantDirectoryAccess(appDataDir);

                string npvrDir = @"C:\Users\Public\NPVR";
                if(!Directory.Exists(npvrDir))
                    npvrDir = @"C:\Documents and Settings\All Users\Application Data\NPVR";
                //NUtility.SettingsHelper.GetInstance().GetDataDirectory()
                GrantDirectoryAccess(npvrDir);

                string regapp = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), @"UltiDev\Web Server\UWS.RegApp.exe");
                if (!File.Exists(regapp))
                    regapp = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), @"UltiDev\Web Server\UWS.RegApp.exe");
                log.AppendLine("regapp: " + regapp);
                if (!File.Exists(regapp))
                    throw new Exception("Failed to located UltiDev web server.");
                log.AppendLine("about to execute");
                System.Diagnostics.Process.Start(regapp,
                    String.Format("/r /AppID=\"{0}\" /aspnet:4 /force32 /url=http://*:8877/ /AppName=\"NextPVR Web Console\" /path:\"{1}\" /vdir:/ /sc1icon:\"{2}\"", AppId.ToString(), installDir, Path.Combine(installDir, "favicon.ico")));
                log.AppendLine("executed");
            //}
            //catch (Exception ex)
            //{
                //log.AppendLine("Error: " + ex.Message);
            //}
            //System.IO.File.WriteAllText(log.ToString(), @"C:\nextpvr.log");
        }

        private static void GrantDirectoryAccess(string Path, string User = "NETWORK SERVICE")
        {

            DirectorySecurity dirSecurity = Directory.GetAccessControl(Path);
            dirSecurity.AddAccessRule(new FileSystemAccessRule(User,
                                                                FileSystemRights.FullControl,
                                                                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                                PropagationFlags.None,
                                                                AccessControlType.Allow));
            Directory.SetAccessControl(Path, dirSecurity);
        }
    }
}
