using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ElevenLabsMusicGenerator
{
    internal static class Program
    {
        public static string AppName { get { return L.T("ElevenLabs Muziek en Spraak", "ElevenLabs Music and Speech"); } }
        public static string DisplayName { get { return L.T("ElevenLabs Muziek, Geluidseffecten en Spraak", "ElevenLabs Music, Sound FX and Speech"); } }
        public const string Version = AppVersion.Short;
        private const string MutexName = "JaapWortel.ElevenLabsMuziekEnSpraak";

        [STAThread]
        private static void Main(string[] args)
        {
            PlaybackDependency.Initialize();
            L.Laad();
            Run(args);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void Run(string[] args)
        {
            if (Controle.IsControle(args))
            {
                Application.EnableVisualStyles();
                Environment.ExitCode = Controle.Voer(args);
                return;
            }
            if (ProgramUpdater.IsApplyUpdateCommand(args))
            {
                ProgramUpdater.ApplyUpdateFromCommandLine(args);
                return;
            }

            bool ownsMutex;
            using (var mutex = new Mutex(true, MutexName, out ownsMutex))
            {
                if (!ownsMutex && args.Any(a => a.Equals("--herstart", StringComparison.OrdinalIgnoreCase)))
                {
                    try { ownsMutex = mutex.WaitOne(15000); }
                    catch (AbandonedMutexException) { ownsMutex = true; }
                }
                if (!ownsMutex)
                {
                    MessageBox.Show(L.T("ElevenLabs Muziek en Spraak is al open.", "ElevenLabs Music and Speech is already running."), AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                AppPaths.EnsureUserFolders();
                AppLog.Initialize();
                ProgramUpdater.ScheduleCleanupFromCommandLine(args);
                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs eventArgs)
                {
                    AppLog.WriteException("Unhandled interface exception", eventArgs.Exception);
                    MessageBox.Show(L.T("Er ging onverwacht iets mis.", "An unexpected error occurred.") + Environment.NewLine + Environment.NewLine + eventArgs.Exception.Message + Environment.NewLine + Environment.NewLine + L.T("De details staan in het logboek.", "Details were written to the log."), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                var initialFile = InitialDocument(args);
                try
                {
                    var hoofd = new MainForm(initialFile);
                    Application.Run(hoofd);
                    if (hoofd.Herstarten)
                    {
                        mutex.ReleaseMutex();
                        ownsMutex = false;
                        System.Diagnostics.Process.Start(Application.ExecutablePath, "--herstart");
                    }
                }
                catch (Exception ex)
                {
                    AppLog.WriteException("Fatal application exception", ex);
                    MessageBox.Show(L.T("Het programma kan niet verder.", "The application could not continue.") + Environment.NewLine + Environment.NewLine + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string InitialDocument(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--cleanup-update", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) i++;
                    continue;
                }
                if (!args[i].StartsWith("--", StringComparison.Ordinal)) return args[i];
            }
            return null;
        }
    }
}
