﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using NextPvrWebConsole;

namespace NextPvrWebConsole.Controllers.Api
{
    [Authorize]
    public class RecordingsController : ApiController
    {
        // GET api/recordings
        public IEnumerable<Models.RecordingGroup> Get()
        {
            return Models.RecordingGroup.Get(this.GetUser().Oid, IncludeAll: true);
        }

        [HttpGet]
        public IEnumerable<Models.RecordingGroup> Available()
        {
            return Models.RecordingGroup.Get(this.GetUser().Oid, IncludeAvailable: true);
        }

        [HttpGet]
        public IEnumerable<Models.Recording> Pending()
        {
            return Models.RecordingGroup.Get(this.GetUser().Oid, IncludePending: true).SelectMany(x => x.Recordings).OrderBy(x => x.StartTime);
        }

        [HttpGet]
        public IEnumerable<Models.RecurringRecording> Recurring()
        {
            return Models.RecurringRecording.LoadAll(this.GetUser().Oid).OrderBy(x => x.Name);
        }

        public IEnumerable<Models.Recording> GetUpcoming()
        {
            return Models.Recording.GetUpcoming(this.GetUser().Oid);
        }

        // GET api/recordings/5
        public NUtility.ScheduledRecording Get(int id)
        {
            return NUtility.ScheduledRecording.LoadAll().Where(x => x.EventOID == id).FirstOrDefault();
        }

        [HttpPost]
        public bool UpdateRecurring(Models.RecurringRecording RecurringRecording)
        {
            var user = this.GetUser();
            return RecurringRecording.Save(user.Oid);
        }
        
        // DELETE api/recordings/5
        public bool Delete(int Oid)
        {
            var recording = NUtility.ScheduledRecording.LoadByOID(Oid);
            if(recording == null)
                throw new Exception("Failed to locate recording");
            
            var instance = NShared.RecordingServiceProxy.GetInstance();
            instance.DeleteRecording(recording);
            Hubs.NextPvrEventHub.Clients_ShowInfoMessage("Deleted recording: " + recording.Name, "Recording Deleted");
            return true;
        }
    }
}
