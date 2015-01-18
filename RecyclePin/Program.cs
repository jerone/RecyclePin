using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RecyclePin
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
		Debug.WriteLine(args);
			args.Process(
				() => Console.WriteLine("Usage is switch0:value switch:value switch2"),
				new CommandLine.Switch("switch0", val => Debug.WriteLine("switch 0 with value {0}", String.Join(" ", val))),
				new CommandLine.Switch("switch1", val => Debug.WriteLine("switch 1 with value {0}", string.Join(" ", val)), "s1"),
				new CommandLine.Switch("switch2", val => Debug.WriteLine("switch 2 with value {0}", string.Join(" ", val)))
			);




			bool isOwned = false;
			System.Threading.Mutex appStartMutex = new System.Threading.Mutex(
			   true,
			   Application.ProductName,
			   out isOwned
			);

			if (!isOwned)
			{
				MessageBox.Show(String.Format("{0} is already running!", Application.ProductName), "Already active", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new RecyclePinForm());
			}
		}
	}
}
