using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Reactive.Threading.Tasks;
using System.Dynamic;
using System.Collections;
using Newtonsoft.Json;
using PsJson;

namespace PowerShellModule
{
    [Cmdlet(VerbsCommunications.Receive, "Messages")]
    public class ReceiveMessages : PSCmdlet
    {
        MessagePump pump = new MessagePump();

        Connection connection;
        private CancellationToken _token;
        private CancellationTokenSource _cancellationTokenSource;

        [Parameter(Mandatory = true, HelpMessage = "URI of SignalR")]
        public string Uri { get; set; }

        protected override void BeginProcessing()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _token = _cancellationTokenSource.Token;
        }

        protected override void StopProcessing()
        {
            _cancellationTokenSource.Cancel();
            connection.Stop();
            connection.Dispose();
        }

        protected override void ProcessRecord()
        {
            using (var connection = new Connection(Uri))
            {
                connection.Error += exception =>
                {
                    var warningMessage = $"Connection error: {exception.Message}";
                    pump.Enqueue(() => WriteWarning(warningMessage));
                };

                connection.StateChanged += change =>
                {
                    var statusChangeMessage = $"Connection state change: From {change.OldState} To {change.NewState}";
                    pump.Enqueue(() => WriteVerbose(statusChangeMessage));
                };

                connection.Closed += () =>
                {
                    pump.Enqueue(() => WriteVerbose("Connection closed."));
                };

                connection.EnsureReconnecting();

                var connectableObservable = connection.AsObservable().Replay();
                
                connection.Start().Wait();

                using (connectableObservable.Connect())
                {
                    var task = connectableObservable
                        .Select(notificationString =>
                        {
                            // Could WriteObject as string, PSObject, other types instead here
                            // PSObject might be more in line with PowerShell ethos
                            pump.Enqueue(() => WriteObject(notificationString));

                            return 1;
                        })
                        .ToTask(_cancellationTokenSource.Token);

                    pump.LoopUntil(task);
                }                
            }
        }
    }

}
