using System;
using System.Windows.Forms;

namespace StampDesigner;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new StampDesignerForm());
    }
}
