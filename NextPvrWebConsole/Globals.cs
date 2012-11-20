﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NextPvrWebConsole
{
    public class Globals
    {
        public const int DB_VERSION = 114;
        public const int SHARED_USER_OID = 1;
        public const string SHARED_USER_USERNAME = "Shared";

        public const NextPvrWebConsole.Models.UserRole USER_ROLE_ALL = Models.UserRole.Dashboard | Models.UserRole.Guide | Models.UserRole.Recordings | Models.UserRole.UserSettings | Models.UserRole.Configuration | Models.UserRole.System;

        public static Version NextPvrWebConsoleVersion = new Version("0.0.0.0");
        public static Version NextPvrVersion = new Version("0.0.0.0");
        public static string WebConsolePhysicalPath = null;
        public static string WebConsoleLoggingDirectory = null;
    }
}