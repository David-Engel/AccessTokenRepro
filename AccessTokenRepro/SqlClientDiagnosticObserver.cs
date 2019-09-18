using Microsoft.Extensions.DiagnosticAdapter;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace AccessTokenRepro
{
    public sealed class SqlClientDiagnosticObserver : IObserver<DiagnosticListener>
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
        {
            if (diagnosticListener.Name == "SqlClientDiagnosticListener")
            {
                var subscription = diagnosticListener.SubscribeWithAdapter(this);
                _subscriptions.Add(subscription);
            }
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        { }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
            _subscriptions.ForEach(x => x.Dispose());
            _subscriptions.Clear();
        }

        private readonly AsyncLocal<Stopwatch> _stopwatch = new AsyncLocal<Stopwatch>();

        [DiagnosticName("System.Data.SqlClient.WriteConnectionOpenBefore")]
        public void OnConnectionBeforeOpen(SqlConnection connection)
        {
            _stopwatch.Value = Stopwatch.StartNew();
            Console.WriteLine($"Connection Before Open: {connection?.ConnectionString}");
            Console.WriteLine($"AccessToken: {connection?.AccessToken}");
            Console.WriteLine();
        }

        [DiagnosticName("System.Data.SqlClient.WriteConnectionOpenAfter")]
        public void OnConnectionAfterOpen(SqlConnection connection)
        {
            var stopwatch = _stopwatch.Value;
            stopwatch.Stop();

            Console.WriteLine($"Connection After Open: {connection?.ConnectionString}");
            Console.WriteLine($"AccessToken: {connection?.AccessToken}");
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
            Console.WriteLine();
        }

        [DiagnosticName("System.Data.SqlClient.WriteConnectionOpenError")]
        public void OnConnectionErrorOpen(SqlConnection connection, Exception exception)
        {
            var stopwatch = _stopwatch.Value;
            stopwatch.Stop();

            Console.WriteLine($"Connection Open Error: {connection?.ConnectionString}");
            Console.WriteLine($"AccessToken: {connection?.AccessToken}");
            Console.WriteLine($"Error Message: {exception?.Message}");
            Console.WriteLine();
        }
    }
}
