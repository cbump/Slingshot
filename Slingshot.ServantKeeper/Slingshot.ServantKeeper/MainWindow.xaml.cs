﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using Slingshot.Core;
using Slingshot.Core.Utilities;

using Slingshot.ServantKeeper.Utilities;

namespace Slingshot.ServantKeeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Threading.DispatcherTimer _apiUpdateTimer = new System.Windows.Threading.DispatcherTimer();

        private readonly BackgroundWorker exportWorker = new BackgroundWorker();

        public List<string> ExportEmailTypes { get; set; }
        public List<ComboBoxItem> EmailTypesComboBoxItems { get; set; } = new List<ComboBoxItem>();

        public List<string> ExportCampus { get; set; }
        public List<ComboBoxItem> ExportCampusComboBoxItems { get; set; } = new List<ComboBoxItem>();

        public MainWindow()
        {
            InitializeComponent();

            _apiUpdateTimer.Tick += _apiUpdateTimer_Tick;
            _apiUpdateTimer.Interval = new TimeSpan( 0, 2, 30 );

            exportWorker.DoWork += ExportWorker_DoWork;
            exportWorker.RunWorkerCompleted += ExportWorker_RunWorkerCompleted;
            exportWorker.ProgressChanged += ExportWorker_ProgressChanged;
            exportWorker.WorkerReportsProgress = true;
        }

        #region Background Worker Events
        private void ExportWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            txtExportMessage.Text = e.UserState.ToString();
            pbProgress.Value = e.ProgressPercentage;
        }

        private void ExportWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            txtExportMessage.Text = "Export Complete";
            pbProgress.Value = 100;
        }

        private void ExportWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            exportWorker.ReportProgress( 0, "" );

            var exportSettings = ( ExportSettings ) e.Argument;

            // clear filesystem directories
            ServantKeeperApi.InitializeExport();

            // export individuals and phone numbers
            if ( exportSettings.ExportIndividuals )
            {
                exportWorker.ReportProgress( 1, "Exporting Individuals..." );
                ServantKeeperApi.ExportIndividuals( exportSettings.ModifiedSince, exportSettings.ExportEmailType, exportSettings.ExportCampus );

                if ( ServantKeeperApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    txtMessages.Text = $"Error exporting individuals: {ServantKeeperApi.ErrorMessage}";
                }

                /*exportWorker.ReportProgress( 1, "Exporting Phones..." );
                ServantKeeperApi.ExportPhoneNumbers( exportSettings.ModifiedSince );

                if ( ServantKeeperApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    txtMessages.Text = $"Error exporting phones: {ServantKeeperApi.ErrorMessage}";
                }*/
            }

            // export contributions
            if ( exportSettings.ExportContributions )
            {
                exportWorker.ReportProgress( 30, "Exporting Funds..." );

                ServantKeeperApi.ExportFunds();
                if ( ServantKeeperApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 31, $"Error exporting funds: {ServantKeeperApi.ErrorMessage}" );
                }

                exportWorker.ReportProgress( 32, "Exporting Contributions..." );

                ServantKeeperApi.ExportContributions( exportSettings.ModifiedSince );
                if ( ServantKeeperApi.ErrorMessage.IsNotNullOrWhitespace() )
                {
                    exportWorker.ReportProgress( 33, $"Error exporting contributions: {ServantKeeperApi.ErrorMessage}" );
                }
            }


            // finalize the package
            ImportPackage.FinalizePackage( "servantkeeper-export.slingshot" );

            // schedule the API status to update (the status takes a few mins to update)
            _apiUpdateTimer.Start();
        }

        #endregion

        /// <summary>
        /// Handles the Tick event of the _apiUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void _apiUpdateTimer_Tick( object sender, EventArgs e )
        {
            // update the api stats
            _apiUpdateTimer.Stop();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            // populate email types combobox
            EmailTypesComboBoxItems.Add( new ComboBoxItem { Text = "Primary Email" } );

            // populate campus fields combobox

            ExportCampusComboBoxItems.Add( new ComboBoxItem { Text = "Main Campus" } );


            txtImportCutOff.Text = DateTime.Now.ToShortDateString();
        }

        /// <summary>
        /// Handles the Click event of the btnDownloadPackage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnDownloadPackage_Click( object sender, RoutedEventArgs e )
        {
            // launch our background export
            var exportSettings = new ExportSettings
            {
                ModifiedSince = ( DateTime ) txtImportCutOff.Text.AsDateTime(),
                ExportContributions = cbContributions.IsChecked.Value,
                ExportIndividuals = cbIndividuals.IsChecked.Value
            };

            exportWorker.RunWorkerAsync( exportSettings );
        }

        #region Windows Events

        private void cbIndividuals_Checked( object sender, RoutedEventArgs e )
        {
            if ( cbIndividuals.IsChecked.Value )
            {
                gridMain.RowDefinitions[5].Height = new GridLength( 1, GridUnitType.Auto );
            }
            else
            {
                gridMain.RowDefinitions[5].Height = new GridLength( 0 );
            }
        }

        #endregion
    }

    public class ExportSettings
    {
        public DateTime ModifiedSince { get; set; } = DateTime.Now;

        public bool ExportIndividuals { get; set; } = true;

        public bool ExportContributions { get; set; } = true;

        public bool ExportGroups { get; set; } = true;

        public string ExportEmailType { get; set; }

        public string ExportCampus { get; set; }
    }

    public class ComboBoxItem
    {
        public string Text { get; set; }
    }
}
