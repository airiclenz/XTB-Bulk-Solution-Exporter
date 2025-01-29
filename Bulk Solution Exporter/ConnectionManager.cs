using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Com.AiricLenz.XTB.Components;
using McTools.Xrm.Connection;


// ============================================================================
// ============================================================================
// ============================================================================
namespace Com.AiricLenz.XTB.Plugin
{

	// ============================================================================
	// ============================================================================
	// ============================================================================
	public partial class ConnectionManager : Form
	{

		private BulkSolutionExporter_PluginControl _parentControl;



		// ============================================================================
		public ConnectionManager(
			BulkSolutionExporter_PluginControl parentControl)
		{
			InitializeComponent();

			_parentControl = parentControl;
			button_removeConnection.Enabled = listBoxConnections.SelectedItem != null;

			UpdateConnections();

			SetSolumns();
		}


		// ============================================================================
		public void UpdateConnections()
		{
			listBoxConnections.Items.Clear();
			int i = 0;

			foreach (var connection in _parentControl.TargetConnections)
			{
				var listItem = new SortableCheckItem(connection, i++);
				listBoxConnections.Items.Add(listItem);
			}

			listBoxConnections.Refresh();
		}



		// ============================================================================
		private void SetSolumns()
		{
			var nameColumn = new ColumnDefinition
			{
				Width = "100%",
				Header = "Connection Name",
				PropertyName = "ConnectionName",
				IsSortable = true,
				IsSortingColumn = true,
				SortOrder = SortOrder.Ascending
			};

			listBoxConnections.Columns.Clear();
			listBoxConnections.Columns.Add(nameColumn);
		}


		// ============================================================================
		private void button_ok_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}


		// ============================================================================
		private void listBoxConnections_SelectedIndexChanged(object sender, EventArgs e)
		{
			button_removeConnection.Enabled = listBoxConnections.SelectedItem != null;
		}


		// ============================================================================
		private void button_addConnection_Click(object sender, EventArgs e)
		{
			_parentControl.AddConnection();
		}

		// ============================================================================
		private void button_removeConnection_Click(object sender, EventArgs e)
		{
			if (listBoxConnections.SelectedIndex == -1)
			{
				return;
			}

			_parentControl.RemoveConnection(
				listBoxConnections.SelectedItem as ConnectionDetail);
		}


	}
}
