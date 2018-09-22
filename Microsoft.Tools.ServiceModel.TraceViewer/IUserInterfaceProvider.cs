using System;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IUserInterfaceProvider
	{
		DialogResult ShowMessageBox(string message, string title, MessageBoxIcon icon, MessageBoxButtons btn);

		object InvokeOnUIThread(Delegate d, params object[] props);

		object InvokeOnUIThread(Delegate d);

		DialogResult ShowDialog(Form dlg, Form parentForm);

		DialogResult ShowDialog(FileDialog dlg, Form parentForm);
	}
}
