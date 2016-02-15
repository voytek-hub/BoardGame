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

namespace BoardGame.Client.Connect4.WPF.Views.UserControls
{
    /// <summary>
    /// Interaction logic for BoardUserControl.xaml
    /// </summary>
    public partial class BoardUserControl : UserControl
    {
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("BorderStrokeThickness", typeof(int), typeof(BoardUserControl));

        public int BorderStrokeThickness
        {
            get { return (int)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public BoardUserControl()
        {
            InitializeComponent();
        }

        public event EventHandler BoardClick;
        protected void BoardGrid_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            double x = e.GetPosition(this).X - BorderStrokeThickness;
            double actualCellWidth = (((Grid)sender).ActualWidth - 2 * BorderStrokeThickness) / 7;
            var columnClicked = (int)(x / actualCellWidth);
            if (columnClicked > 6) columnClicked = 6;
            if (columnClicked < 0) columnClicked = 0;

            this.BoardClick?.Invoke(this, e);
        }
    }
}