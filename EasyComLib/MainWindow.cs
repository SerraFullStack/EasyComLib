using System;
using EasyComLib;
using Gtk;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

		ReCom re = new ReCom("secondId");
		re.ConnectTo("firstId", delegate ()
		{
			MessageDialog md = new MessageDialog(this,
			                                     DialogFlags.DestroyWithParent, MessageType.Info,
                ButtonsType.Close, "sucess connect");
            md.Run();
            md.Destroy();
		}, delegate ()
		{
			MessageDialog md = new MessageDialog(this,
                                                 DialogFlags.DestroyWithParent, MessageType.Info,
                ButtonsType.Close, "connect failure");
            md.Run();
            md.Destroy();
		});
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    
}
