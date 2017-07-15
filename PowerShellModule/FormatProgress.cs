using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PowerShellModule
{
    [Cmdlet(VerbsCommon.Format, "Progress")]
    public class FormatProgress : PSCmdlet
    {
        [Parameter(
           Position = 0,
           ValueFromPipeline = true,
           ValueFromPipelineByPropertyName = true
        )]
        [ValidateNotNull]
        public object Notification { get; set; }

        private List<Guid> _activityIds = new List<Guid>();

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            ProgressRecord2 p2 = GetProgressRecord2FromNotification(Notification);
                
            int index = ActivityIndex(p2.ActivityId);
            int parentIndex = ActivityIndex(p2.ParentActivityId);
                
            ProgressRecordType recordType;
            Enum.TryParse<ProgressRecordType>(p2.RecordType, out recordType);

            var progress = new ProgressRecord(index, p2.Activity, p2.StatusDescription)
            {
                PercentComplete = p2.PercentComplete,
                ParentActivityId = parentIndex,
                RecordType = recordType,
                SecondsRemaining = p2.SecondsRemaining,
            };

            WriteProgress(progress);
        }

        private ProgressRecord2 GetProgressRecord2FromNotification(object notification)
        {
            // Objects which come via WriteObject are wrapped in PSObject so we need to unwrap
            var innerNotification = (string)((PSObject)notification).BaseObject;
            return JsonConvert.DeserializeObject<ProgressRecord2>(innerNotification);
        }

        private int ActivityIndex(Guid activityId)
        {
            if (activityId == Guid.Empty) return -1;

            var index = _activityIds.IndexOf(activityId);
            if (index == -1)
            {
                _activityIds.Add(activityId);
                index = _activityIds.IndexOf(activityId);
            }

            return index;
        }
    }


    public class ProgressRecord2
    {
        public Guid ActivityId { get; set; }

        public Guid ParentActivityId { get; set; }

        public string RecordType { get; set; }

        public string Activity { get; set; }

        public int SecondsRemaining { get; set; }

        public int PercentComplete { get; set; }

        public string StatusDescription { get; set; }
    }
}
