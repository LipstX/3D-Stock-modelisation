using System;
using System.Windows;
using Microsoft.Win32;

namespace DoBlocks
{
    /// <summary>
    /// Logique d'interaction pour UserControl1.xaml
    /// </summary>
    public partial class Gui
    {
        public Gui()
        {
            InitializeComponent();
            _comm = new Commands();
        }
        private string _path;
        private int _height;
        private int _distance;
        private int _deep;
        private int _width;
        private int _levels;
        private int _racks;
        private Commands _comm;

        //
        // Ouvre une boite de dialogue permettant de sélectionner le fichier xml à parser.
        //
        private void BrowseFiles(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = new OpenFileDialog { Filter = "Fichiers XML|*.xml;*.txt", Title = "Choisissez un fichier à parser" };
                if (files.ShowDialog() == true)
                {
                    _path = files.FileName;
                    xmltxt.Text = _path;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Syntax problem in your XML file here : \n" + exc);
            }
        }

        //
        // Ouvre une boite de dialogue permettant de sélectionner le fichier excel à parser.
        //
        private void BrowseExcel(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = new OpenFileDialog { Filter = "Fichiers excel|*.xlsx;*.xls", Title = "Choisissez un fichier à parser" };
                if (files.ShowDialog() == true)
                {
                    _path = files.FileName;
                    exceltxt.Text = _path;
                    xmltxt.Text = @"C:\Box.xml";
                    // Appel de la fonction permettant la récupération des informations sur les blocks
                    _comm.GetInfoExcel(_path);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Syntax problem in your Excel file here : \n" + exc);
            }
        }

        //
        // Appel lors de l'appui sur le bouton launch
        //
        private void ApplyFile(object sender, RoutedEventArgs e)
        {
            _height = Convert.ToInt32(HeightR.Text);
            _distance = Convert.ToInt32(DistanceR.Text);
            _levels = Convert.ToInt32(LevelR.Text);
            _racks = Convert.ToInt32(RacksR.Text);
            _deep = Convert.ToInt32(DeepR.Text);
            _width = Convert.ToInt32(WidthR.Text);
            _path = xmltxt.Text;
            _comm.GetInfoRacks(_width, _distance, _height, _levels, _racks, _deep);
            _comm.DeSerializeFile(_path);
        }

        private void FindByPosition(object sender, RoutedEventArgs e)
        {
            _comm.LookForPos(Convert.ToInt32(RackS.Text), Convert.ToInt32(LevelS.Text), Convert.ToInt32(PositionS.Text));
        }

        private void FindBByName(object sender, RoutedEventArgs e)
        {
            _comm.LookForName(NameS.Text);
        }
    }
}
