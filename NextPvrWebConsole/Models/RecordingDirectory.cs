﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace NextPvrWebConsole.Models
{
    [PetaPoco.PrimaryKey("Oid")]
    public class RecordingDirectory
    {
        public string Name { get; set; }
        public int Oid { get; set; }
        public int UserOid { get; set; }
        public string Path { get; set; }
        public bool IsDefault { get; set; }
        [PetaPoco.ResultColumn]
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets teh RecordingDirectoryId used by NextPVR to identify the directory, this should be "{Username} - {Name}"
        /// </summary>
        [PetaPoco.Ignore]
        public string RecordingDirectoryId
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Username))
                    return null;
                return GetRecordingDirectoryId(Username, this.Name);
            }
        }

        public RecordingDirectory()
        {
            this.Path = "";
        }

        public static List<RecordingDirectory> LoadForUser(int UserOid, bool IncludeShared = false)
        {
            var db = DbHelper.GetDatabase();
            var config = new Configuration();
            if (config.EnableUserSupport)
            {
                string select = "select rd.*, username from recordingdirectory rd inner join [user] u on rd.useroid = u.oid where useroid = {0} {1}".FormatStr(UserOid, IncludeShared ? " or useroid = {0}".FormatStr(Globals.SHARED_USER_OID) : "");
                return SetUserPaths(db.Fetch<RecordingDirectory>(select));
            }
            else if (!IncludeShared)
            {
                return new List<RecordingDirectory>(); // nothing to return then
            }
            else
            {
                return db.Fetch<RecordingDirectory>("select rd.*, username from recordingdirectory rd inner join user u on rd.useroid = u.oid where useroid = @0", Globals.SHARED_USER_OID);
            }
        }

        public static List<RecordingDirectory> LoadAll()
        { 
            var db = DbHelper.GetDatabase();
            return SetUserPaths(db.Fetch<RecordingDirectory>("select rd.*, username, username || '\' || rd.name as shortname from recordingdirectory rd inner join user u on rd.useroid = u.oid"));
        }

        private static List<RecordingDirectory> SetUserPaths(List<RecordingDirectory> Directories)
        {
            var config = new Configuration();
            foreach (var dir in Directories)
            {
                if (dir.UserOid != Globals.SHARED_USER_OID)
                    dir.Path = System.IO.Path.Combine(config.DefaultRecordingDirectoryRoot, dir.Username, dir.Name);
            }
            return Directories;
        }

        public static RecordingDirectory LoadByName(int UserOid, string Name)
        {
            var db = DbHelper.GetDatabase();
            return db.FirstOrDefault<RecordingDirectory>("select * from recordingdirectory where useroid = @0 and name = @1", UserOid, Name);
        }

        public static RecordingDirectory Create(int UserOid, string Name)
        {
            var db = DbHelper.GetDatabase();
            // check if exists
            if (db.ExecuteScalar<int>("select count(*) from recordingdirectory where useroid = @0 and lower(name) = @1", UserOid, Name.Trim().ToLower()) > 0)
                throw new ArgumentException("A Recording Directory with the name '{0}' already exists.".FormatStr(Name));

            string username = User.GetUsername(UserOid);
            RecordingDirectory directory = new RecordingDirectory() { UserOid = UserOid, Name = Name.Trim(), Username = username };
            db.Insert(directory);
            Configuration.Write();
            return directory;
        }

        public static string GetRecordingDirectoryId(string Username, string RecordingDirectoryName)
        {
            return "{0} - {1}".FormatStr(Username, RecordingDirectoryName);
        }


        public void Save()
        {
            var db = DbHelper.GetDatabase();
            if (this.Oid == 0)
            {
                db.Insert("recordingdirectory", "oid", true, this);
            }
            else
            {
                db.Update("recordingdirectory", "oid", this, this.Oid, new string[] { "Name" });
            }
            Configuration.Write();
        }

        /// <summary>
        /// Saves the recording directories for a user, will delete any that aren't listed
        /// </summary>
        /// <param name="UserOid">the OID of the User</param>
        /// <param name="RecordingDirectories">the list of recording directories to save</param>
        internal static bool SaveForUser(int UserOid, List<RecordingDirectory> RecordingDirectories)
        {
            if (RecordingDirectories.DuplicatesBy(x => x.Name.ToLower().Trim()).Count() > 0)
                throw new ArgumentException("Recording Directory names must be unique.");
            string username = User.GetUsername(UserOid);
            var db = DbHelper.GetDatabase();
            db.BeginTransaction();
            try
            {
                // delete the old
                string rdOids = String.Join(",", (from rd in RecordingDirectories where rd.Oid > 0 select rd.Oid.ToString()).ToArray());
                db.Execute("delete from recordingdirectory where useroid = {0} {1}".FormatStr(UserOid, String.IsNullOrWhiteSpace(rdOids) ? "" : "and oid not in ({0})".FormatStr(rdOids)));

                // save the rest
                foreach (var rd in RecordingDirectories)
                {
                    rd.UserOid = UserOid;
                    rd.Username = username;
                    if (rd.Oid < 1) // new one
                        db.Insert("recordingdirectory", "oid", true, rd);
                    else // update an old one
                        db.Update(rd);
                }

                db.CompleteTransaction();
                Configuration.Write();
                return true;
            }
            catch (Exception ex)
            {
                db.AbortTransaction();
                return false;
            }
        }

        public static bool IsValidRecordingDirectoryName(string Name)
        {
            Regex rgx = new Regex(@"^([^""*/:?|<>\\.\x00-\x20]([^""*/:?|<>\\\x00-\x1F]*[^""*/:?|<>\\.\x00-\x20])?)$");
            return rgx.IsMatch(Name);
        }

        internal static void Delete(int UserOid, int Oid)
        {
            var db = DbHelper.GetDatabase();
            db.Execute("delete from recordingdirectory where useroid = @0 and oid = @1", UserOid, Oid);
            Configuration.Write();
        }

        internal static RecordingDirectory Load(int Oid)
        {
            var db = DbHelper.GetDatabase();
            return db.FirstOrDefault<RecordingDirectory>("select * from recordingdirectory where oid = @0", Oid);
        }
    }
}