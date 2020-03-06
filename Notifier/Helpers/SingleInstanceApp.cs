using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    public abstract class SingleInstanceApp<T> : Application
    {
        public SingleInstanceApp()
        {

        }

        public SingleInstanceApp(T pars)
        {
            HandleNextInstance(pars);
        }

        internal abstract void HandleNextInstance(T parameters);

        private async void InitiateNamedPipeServer()
        {
            // A perf test could be useful here.
            var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            await pipeServer.WaitForConnectionAsync();

#pragma warning disable CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel

            Task.Run(() =>
            {
                string msg;
                using (var sr = new StreamReader(pipeServer))
                {
                    msg = sr.ReadLine();
                }
                // Warning: could fail if any of the arguments contains the string we are splitting on (program path can and will...).
                HandleNextInstance(JsonSerializer.Deserialize<T>(msg));
            });
            Task.Run(InitiateNamedPipeServer);

#pragma warning restore CS4014 // Dans la mesure où cet appel n'est pas attendu, l'exécution de la méthode actuelle continue avant la fin de l'appel

        }

        private static readonly string _singleInstancePipeName;
        private static readonly string _pipeName;

        static SingleInstanceApp()
        {
            var assemblyName = Assembly.GetExecutingAssembly().FullName;
            _singleInstancePipeName = $"{assemblyName}_Pipe_SingleInstance";
            _pipeName = $"{assemblyName}_Pipe";
        }

        internal static void RunOrJoin<TA>(T parameters) where TA : SingleInstanceApp<T>
        {
            // Tries to create a named pipe (if it exists, it means the notifier is already running since we allow only one instance)
            // Should probably use either a mutex or a semaphore here (to be improved... later).
            try
            {
                using var pipeServer = new NamedPipeServerStream(_singleInstancePipeName, PipeDirection.In, 1);
                var app = ((SingleInstanceApp<T>)Activator.CreateInstance(typeof(TA), parameters));
                app.InitiateNamedPipeServer();
                app.Run();
            }
            catch (IOException)
            {
                // Already running: sending message to the server.
                using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                pipeClient.Connect();
                pipeClient.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parameters)));
            }
        }
    }
}