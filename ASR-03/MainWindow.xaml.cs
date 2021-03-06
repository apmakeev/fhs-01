﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Data;
using MySql.Data.MySqlClient;
using Xceed.Wpf;
using System.ComponentModel;

namespace ASR_03
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MySqlConnection conn = new MySqlConnection("server=127.0.0.1;port=3306;userid=admin;password=admin;database=analiticsb");
        // MySqlConnection conn = new MySqlConnection("server=198.18.235.238;port=9306;userid=admin;password=admin;database=analiticsb;Connect Timeout=300");
        // MySqlConnection conn = new MySqlConnection("server=10.28.11.112;port=3306;userid=admin;password=admin;database=analiticsb;Connect Timeout=300");

        public MainWindow()
        {
            InitializeComponent();
            UpdateASR();
        }

        private void UpdateASR()
        {
            DateTime dtFrom = (DateTime)dtPickerFrom.Value;
            DateTime dtTo = (DateTime)dtPickerTo.Value;

            string sql_01 = 
                "SELECT IFNULL(c.mediaGateLocation, 'Итого') AS 'Region', "
                + "COUNT(*) AS 'Total', "
                + "SUM(IF(c.result = 0, 1, 0)) AS 'Success', "
                + "SUM(IF(c.result = 0 AND c.CallCodeResult = 4, 1, 0)) AS 'Connect', "
                + "ROUND((SUM(IF(c.CallCodeResult = 4, 1, 0)) / COUNT(*) * 100), 2) AS 'ASR' "
                + "FROM cdr c INNER JOIN voip v ON c.voip_index = v.voip_index "
                + "WHERE timeStart BETWEEN STR_TO_DATE('"
                + dtFrom.ToString("yyyy-MM-dd  HH:mm:ss")
                + "', '%Y-%m-%d %H:%i:%s') AND STR_TO_DATE('"
                + dtTo.ToString("yyyy-MM-dd  HH:mm:ss")
                + "', '%Y-%m-%d %H:%i:%s') "
                + "GROUP BY c.mediaGateLocation WITH ROLLUP";

            string sql_02 =
                "SELECT "
                + "IFNULL(cdr.Filial,'Все филиалы') AS 'Branch', "
                + "IFNULL(cdr.code, 'Все шлюзы') AS 'Router', "
                + "cdr.GW_type, "
                + "SUM(IF(cdr.TypeOfCall = 'OUT', 1, 0)) AS 'Total', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.result = 0, 1, 0)) AS 'Success', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.result = 0 AND cdr.CompareCLI = 2, 1, 0)) AS 'NonMatching', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.CompareCLI = 1, 1, 0)) AS 'Matching', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.CompareCLI = 4, 1, 0)) AS 'Distortion', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.result = 0 AND cdr.CallCodeResult = 4, 1, 0)) AS 'Connected', "
                + "SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.result = 0 AND cdr.descr_name = 'False Answer Supervision', 1, 0)) AS 'FAS', "
                + "SUM(IF(cdr.TypeOfCall='OUT',cdr.duration,0)) AS 'Duration', "
                + "ROUND((SUM(IF(cdr.TypeOfCall = 'OUT' AND cdr.result = 0, 1, 0)) / SUM(IF(cdr.TypeOfCall = 'OUT', 1, 0)) * 100), 2) AS 'ASR', "
                + "SUM(IF(cdr.TypeOfCall = 'IN', 1, 0)) AS 'InTotal' "                
                + "FROM ( SELECT "
                + "IF(cm.Location_src = lm.hash, 'OUT', 'IN') 'TypeOfCall', "
                + "lm.code, lm.Carrier, lm.Macroregion, lm.Filial, lm.City, lm.GW_type, lm.GW_str, "
                + "cm.srcNumber, cm.dstNumber, cm.originator, cm.timeStart, "
                + "cm.ConnectTimeLeg1, cm.timeOffset, cm.result, cm.synq, "
                + "cm.duration, cm.incomingTime, cm.ConnectTime_Leg2, cm.timeOffset_leg2, "
                + "cm.duration_Leg2, cm.CallCodeResult, cm.SoundDetect, cm.IsAuthorizedA1, "
                + "cm.source_IMEI, cm.network_leg2, cm.LastDateSynch, cm.DateTimeImport, "
                + "cm.SynchAB, cm.CompareCLI, cm.delta_time_synchro, cm.IpAddr_out, "
                + "cm.Port_out, cm.IpAddr_in, cm.Port_in, cm.originator_original, "
                + "cm.src_port, cm.dst_port, cd.descr_name "
                + "FROM cdr_mg cm "
                + "INNER JOIN location_mg lm "
                + "ON(cm.Location_src = lm.hash OR cm.Location_dst = lm.hash) "
                + "INNER JOIN calldescrption cd "
                + "ON cm.descr_index = cd.descr_index "
                + "WHERE cm.timeStart BETWEEN STR_TO_DATE('"    + dtFrom.ToString("yyyy-MM-dd  HH:mm:ss")
                + "', '%Y-%m-%d %H:%i:%s') AND STR_TO_DATE('"   + dtTo.ToString("yyyy-MM-dd  HH:mm:ss")+ "', '%Y-%m-%d %H:%i:%s') order by lm.Filial"
                // + ") cdr GROUP BY cdr.Filial, cdr.code";
                + ") cdr GROUP BY cdr.code";

            // System.Windows.MessageBox.Show(sql_02);

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(sql_02, conn);
                MySqlDataAdapter adp = new MySqlDataAdapter(cmd);
                // DataSet ds = new DataSet();
                // adp.Fill(ds, "LoadDataBinding");
                // dataGridASR.DataContext = ds;

                DataTable dt = new DataTable();
                adp.Fill(dt);
                dataGridASR.DataContext = dt;

                ICollectionView view = CollectionViewSource.GetDefaultView(dataGridASR.DataContext);
                view.SortDescriptions.Add(new SortDescription("Branch", ListSortDirection.Ascending));
                dataGridASR.ItemsSource = view;

                // ICollectionView view = CollectionViewSource.GetDefaultView(dt);
                // ICollectionView view = CollectionViewSource.GetDefaultView(dataGridASR.DataContext);
                // view.GroupDescriptions.Add(new PropertyGroupDescription("Branch"));
                // dataGridASR.DataContext = view;
                // dataGridASR.ItemsSource = view;
            }
            catch (MySqlException ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            finally
            {
                conn.Close();
                Mouse.OverrideCursor = null;
            }
        }

        private void dtPickerFrom_Initialized(object sender, EventArgs e)
        {
            // DateTime dtFrom = DateTime.Today;
            // dtPickerFrom.DefaultValue = dtFrom.AddDays(-1);
            dtPickerFrom.DefaultValue = new DateTime(2016,03,24,0,0,0);
            
            
        }

        private void dtPickerTo_Initialized(object sender, EventArgs e)
        {
            // DateTime dtTo = DateTime.Today;
            // dtPickerTo.DefaultValue = dtTo.AddSeconds(-1);
            dtPickerTo.DefaultValue = new DateTime(2016, 03, 24, 23, 59, 59);
            
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            UpdateASR();
        }

        private void GroupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGridASR.DataContext);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("Branch"));
            dataGridASR.AlternatingRowBackground = null;
        }

        private void GroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGridASR.DataContext);
            view.GroupDescriptions.Clear();
            dataGridASR.AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(100,250,250,250));
        }

        private void FilterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ((DataTable)dataGridASR.DataContext).DefaultView.RowFilter = "GW_type='mobile'";
        }

        private void FilterCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ((DataTable)dataGridASR.DataContext).DefaultView.RowFilter = "";
        }
    }
}
