namespace WinformTTC.Client;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Forms.ClientForm());
    }
}
