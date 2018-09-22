using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class RichTextXmlRenderer : RichTextBox
	{
		internal IList<XmlNodeRecord> attributeValueRecords = new List<XmlNodeRecord>();

		internal IList<int> initialHighlightPositions = new List<int>();

		internal IList<XmlNodeRecord> textRecords = new List<XmlNodeRecord>();

		private const int WM_PAINT = 15;

		private RichTextXmlFormatter formatter = new RichTextXmlFormatter();

		private bool isHighlightBeforeLoading;

		private bool isWorking;

		private bool isSystemColorChanged;

		private FindCriteria savedFindCriteria;

		private string savedRtf;

		public RichTextXmlRenderer()
		{
			InitializeComponent();
		}

		internal void ExecuteHighlight()
		{
			foreach (int initialHighlightPosition in initialHighlightPositions)
			{
				Select(initialHighlightPosition, savedFindCriteria.FindingText.Length);
				base.SelectionColor = SystemColors.HighlightText;
				base.SelectionBackColor = SystemColors.Highlight;
			}
			initialHighlightPositions.Clear();
		}

		internal void PrepareHighlight(FindCriteria findCriteria)
		{
			savedFindCriteria = findCriteria;
			if (savedRtf != null)
			{
				isHighlightBeforeLoading = false;
				if (savedFindCriteria != null)
				{
					initialHighlightPositions.Clear();
					if ((savedFindCriteria.Options & FindingOptions.MatchWholeWord) != 0)
					{
						FindWholeWordMatchedResult();
					}
					else if ((savedFindCriteria.Options & FindingOptions.MatchCase) != 0)
					{
						FindMatchedResult(StringComparison.CurrentCulture);
					}
					else
					{
						FindMatchedResult(StringComparison.CurrentCultureIgnoreCase);
					}
					isWorking = true;
					base.Rtf = savedRtf;
					ExecuteHighlight();
					isWorking = false;
				}
			}
			else
			{
				isHighlightBeforeLoading = true;
			}
		}

		internal void ClearXmlText()
		{
			base.Rtf = null;
			savedFindCriteria = null;
			savedRtf = null;
			attributeValueRecords.Clear();
			initialHighlightPositions.Clear();
			textRecords.Clear();
		}

		internal void SetXmlText(string xml)
		{
			if (!string.IsNullOrEmpty(xml))
			{
				textRecords.Clear();
				attributeValueRecords.Clear();
				base.Rtf = formatter.Xml2Rtf(xml, attributeValueRecords, textRecords);
				savedRtf = (string)base.Rtf.Clone();
				if (isHighlightBeforeLoading)
				{
					PrepareHighlight(savedFindCriteria);
				}
			}
			else
			{
				base.Rtf = null;
				savedRtf = null;
			}
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
		}

		protected override void OnFontChanged(EventArgs e)
		{
		}

		protected override void OnForeColorChanged(EventArgs e)
		{
		}

		protected override void OnSelectionChanged(EventArgs e)
		{
		}

		protected override void WndProc(ref Message m)
		{
			int msg = m.Msg;
			if (msg == 15)
			{
				if (isSystemColorChanged)
				{
					isSystemColorChanged = false;
					if (savedRtf != null)
					{
						base.Rtf = savedRtf;
					}
				}
				if (isWorking)
				{
					m.Result = IntPtr.Zero;
				}
				else
				{
					base.WndProc(ref m);
				}
			}
			else
			{
				base.WndProc(ref m);
			}
		}

		protected void RichTextXmlRenderer_MouseClick(object sender, MouseEventArgs e)
		{
			if (savedFindCriteria != null && e.Button == MouseButtons.Left)
			{
				base.Rtf = savedRtf;
				savedFindCriteria = null;
				initialHighlightPositions.Clear();
			}
		}

		private void FindMatchedResult(StringComparison stringComparison)
		{
			int startIndex = 0;
			int length = savedFindCriteria.FindingText.Length;
			int num = 0;
			if (savedFindCriteria.Target == FindingTarget.RawData || savedFindCriteria.Target == FindingTarget.LoggedMessage)
			{
				while ((num = Text.IndexOf(savedFindCriteria.FindingText, startIndex, stringComparison)) >= 0)
				{
					initialHighlightPositions.Add(num);
					startIndex = num + length;
				}
			}
			else
			{
				IList<XmlNodeRecord> searchingList = GetSearchingList();
				if (searchingList != null)
				{
					foreach (XmlNodeRecord item in searchingList)
					{
						startIndex = 0;
						while ((num = item.Value.IndexOf(savedFindCriteria.FindingText, startIndex, stringComparison)) >= 0)
						{
							initialHighlightPositions.Add(num + item.Pos);
							startIndex = num + length;
						}
					}
				}
			}
		}

		private IList<XmlNodeRecord> GetSearchingList()
		{
			if (savedFindCriteria.Target == FindingTarget.XmlTagValue)
			{
				return textRecords;
			}
			if (savedFindCriteria.Target == FindingTarget.XmlTagAttribute)
			{
				return attributeValueRecords;
			}
			return null;
		}

		private void FindWholeWordMatchedResult()
		{
			if (savedFindCriteria.Target == FindingTarget.RawData || savedFindCriteria.Target == FindingTarget.LoggedMessage)
			{
				Match match = savedFindCriteria.WholeWordRegex.Match(Text, 0, TextLength);
				while (match.Success)
				{
					initialHighlightPositions.Add(match.Index);
					match.NextMatch();
				}
			}
			else
			{
				IList<XmlNodeRecord> searchingList = GetSearchingList();
				if (searchingList != null)
				{
					foreach (XmlNodeRecord item in searchingList)
					{
						Match match2 = savedFindCriteria.WholeWordRegex.Match(item.Value, item.Value.Length);
						while (match2.Success)
						{
							initialHighlightPositions.Add(item.Pos + match2.Index);
						}
					}
				}
			}
		}

		private void InitializeComponent()
		{
			SuspendLayout();
			base.WordWrap = false;
			base.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
			Dock = System.Windows.Forms.DockStyle.Fill;
			base.MouseClick += new System.Windows.Forms.MouseEventHandler(RichTextXmlRenderer_MouseClick);
			base.ReadOnly = true;
			base.BorderStyle = System.Windows.Forms.BorderStyle.None;
			BackColor = System.Drawing.SystemColors.Window;
			base.DetectUrls = false;
			DoubleBuffered = true;
			ResumeLayout(performLayout: false);
		}

		protected override void OnSystemColorsChanged(EventArgs e)
		{
			isSystemColorChanged = true;
			base.OnSystemColorsChanged(e);
		}
	}
}
