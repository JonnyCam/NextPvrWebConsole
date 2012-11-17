﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace NextPvrWebConsole.Models
{
    [DataContract]
    [PetaPoco.PrimaryKey("Oid")]
	public class Channel
    {
        [DataMember]
        public int Oid { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int Number { get; set; }
        [DataMember]
        public bool Enabled { get; set; }
        [DataMember]
        [PetaPoco.Ignore]
        public bool HasIcon { get; set; }

        [PetaPoco.Ignore]
        [DataMember]
        public List<EpgListing> Listings { get; set; }

        public Channel()
        {
        }

        public static List<Channel> LoadForTimePeriod(int UserOid, string GroupName, DateTime Start, DateTime End)
        {
            int[] channelOids = ChannelGroup.LoadChannelOids(UserOid, GroupName);

            // -12 hours from start to make sure we get data that starts earlier than start, but finishes after start
            var data = NUtility.EPGEvent.GetListingsForTimePeriod(Start.AddHours(-12), End);

            var recordings = NUtility.ScheduledRecording.LoadAll().Where(x => x.EndTime >= Start && x.StartTime <= End && x.EventOID > 0).ToDictionary(x => x.EventOID);

            List<Channel> results = new List<Channel>();
            foreach (var key in data.Keys.Where(x => channelOids.Contains(x.OID)))
            {
                results.Add(new Channel() 
                {
                    Name = key.Name,
                    Number = key.Number,
                    Oid = key.OID,
                    HasIcon = key.Icon != null,

                    Listings = data[key].Where(x => x.EndTime > Start).Select(x => new EpgListing(x) {
                        IsRecording = recordings.ContainsKey(x.OID)
                    }).ToList()
                });
            }
            return results;
        }


        internal static Channel[] LoadAll(int UserOid, bool IncludeDisabled = false)
        {
            var db = DbHelper.GetDatabase();
            List<Channel> results = null;
            if (UserOid == Globals.SHARED_USER_OID)
            {
                results = db.Fetch<Channel>(@"select * from channel order by number");
            }
            else
            {
                results = db.Fetch<Channel>(@"
select c.oid, c.name, uc.*
from userchannel uc
inner join channel c on uc.channeloid = c.oid and c.enabled = 1 and uc.useroid = @0
order by uc.number", UserOid);
            }
            if (IncludeDisabled)
                return results.ToArray();
            return results.Where(x => x.Enabled).ToArray();
        }

        internal static Channel[] LoadChannelsForGroup(int UserOid, string GroupName)
        {
            var db = DbHelper.GetDatabase();
            return db.Fetch<Channel>(@"
select c.oid, c.name, uc.*
from channelgroup cg
inner join channelgroupchannel cgc on cg.oid = cgc.channelgroupoid
inner join channel c on cgc.channeloid = c.oid
inner join userchannel uc on c.oid = uc.channeloid
where c.enabled = 1 and uc.enabled = 1 and uc.useroid = @0 and cg.name = @1", UserOid, GroupName).ToArray();
        }

        internal static Channel Load(int ChannelOid, int UserOid)
        {
            var db = DbHelper.GetDatabase();
            return db.FirstOrDefault<Channel>("select c.oid, c.name, uc.* from channel c inner join userchannel uc on c.oid = uc.channeloid where c.oid = @0 and uc.useroid = @1 and c.enabled = 1", ChannelOid, UserOid);
        }

        internal static Channel Load(int ChannelOid)
        {
            var db = DbHelper.GetDatabase();
            return db.FirstOrDefault<Channel>("select * from channel c where oid = @0", ChannelOid);

        }

        internal static void Update(int UserOid, Channel[] Channels)
        {
            var db = DbHelper.GetDatabase();
            try
            {
                db.BeginTransaction();
                int[] allowedChanneldOids = db.Fetch<int>("select oid from channel where [enabled] = 1").ToArray();
                foreach (var channel in Channels.Where(x => allowedChanneldOids.Contains(x.Oid)))
                    db.Execute("update userchannel set number = @0, [enabled] = @1 where channeloid = @2 and useroid = @3", channel.Number, channel.Enabled, channel.Oid, UserOid);
                db.CompleteTransaction();
            }
            catch (Exception ex)
            {
                db.AbortTransaction();
                throw ex;
            }
        }

        internal static bool SaveForUser(int UserOid, List<Channel> Channels)
        {
            var db = DbHelper.GetDatabase();
            db.BeginTransaction();
            try
            {
                if (UserOid == Globals.SHARED_USER_OID)
                {
                    // delete any missing channels
                    int[] knownOids = db.Fetch<int>("select oid from [channel]").ToArray();
                    db.Execute("delete from [userchannel] where channeloid not in ({0})".FormatStr(String.Join(",", Channels.Where(x => x.Oid > 0).Select(x => x.Oid.ToString()).ToArray())));
                    db.Execute("delete from [channel] where oid not in ({0})".FormatStr(String.Join(",", Channels.Where(x => x.Oid > 0).Select(x => x.Oid.ToString()).ToArray())));
                    foreach (var channel in Channels)
                    {
                        if (String.IsNullOrWhiteSpace(channel.Name))
                            throw new Exception("Channel name required.");
                        if (channel.Number < 0 || channel.Number > 1000)
                            throw new Exception("Channel number must be in the range 0 to 999.");
                        if (knownOids.Contains(channel.Oid))
                            db.Update(channel);
                        else
                            db.Insert("channel", "oid", false, channel);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                db.CompleteTransaction();
                return true;
            }
            catch (Exception ex)
            {
                db.AbortTransaction();
                throw ex;
            }
        }

        internal static System.Drawing.Image LoadIcon(int Oid)
        {
            var channel = NUtility.Channel.LoadByOID(Oid);
            if (channel == null)
                return null;
            return channel.Icon.ToIconSize();
        }
    }
}