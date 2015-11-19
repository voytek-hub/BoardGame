﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BoardGame.API;
using BoardGame.Domain.Enums;
using BoardGame.Domain.Factories;
using BoardGame.Domain.Interfaces;
using BoardGame.Infrastructure.DependecyResolution;
using Ninject;
using Ninject.Modules;
using Ninject.Parameters;

namespace BoardGame.Client.Connect4.WinForms
{
    public partial class Form1 : Form
    {
        private GameAPI api = new GameAPI();
        IKernel kernel = new StandardKernel();

        public Form1()
        {
            InitializeComponent();
            
            kernel.Load(new List<INinjectModule> { new GameModule() });

            this.tableLayoutPanel1.RowStyles.Clear();
            this.tableLayoutPanel1.ColumnStyles.Clear();
            for (int i = 1; i <= this.tableLayoutPanel1.RowCount; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 1));
            }
            for (int i = 1; i <= this.tableLayoutPanel1.ColumnCount; i++)
            {
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            }
        }

        int GetColumnIndex(TableLayoutPanel tlp, Point point)
        {
            if (point.X > tlp.Width || point.Y > tlp.Height)
                return -1;

            int w = tlp.Width;
            int h = tlp.Height;
            int[] widths = tlp.GetColumnWidths();

            int i;
            for (i = widths.Length - 1; i >= 0 && point.X < w; i--)
            {
                w -= widths[i];
            }

            int col = i + 1;
            return col;
        }

        private void tableLayoutPanel1_Click(object sender, EventArgs e)
        {
            int clickedColumn = GetColumnIndex(
                tableLayoutPanel1,
                tableLayoutPanel1.PointToClient(Cursor.Position));

            IMove result = api.MakeMove(0, clickedColumn);

            DoMove(result);
            //if (result != null && result.FollowingBotResult != null)
            //{
            //    DoMove(result.FollowingBotResult);
            //}
        }

        private void DoMove(IMove result)
        {
            if (result != null && result.Row >= 0)
            {
                Panel panel1 = new Panel();
                panel1.BackColor = (result.PlayerId == 1) ? Color.Red : Color.Black;
                panel1.Name = "panel" + result.Column + result.Row;
                panel1.Size = new Size(91, 91);
                panel1.TabIndex = 0;
                tableLayoutPanel1.Controls.Add(panel1, result.Column, result.Row);

                if (result.IsConnected)
                {
                    Color connectColor = (result.PlayerId == 1) ? Color.Purple : Color.Brown;

                    MessageBox.Show("Success! Player " + result.PlayerId.ToString() + " won!");
                }
            }

            if (result != null && result.IsTie)
            {
                MessageBox.Show("Game Over!");
                return;
            }
        }

        private void bSinglePlayer_Click(object sender, EventArgs e)
        {
            NewGame();
            IBoard board = kernel.Get<IBoard>( new ConstructorArgument("width", 7), new ConstructorArgument("height", 6));
            api.CreateGame(kernel.Get<IGameFactory>(), GameType.SinglePlayer, board);
        }

        private void NewGame()
        {
            tableLayoutPanel1.Controls.Clear();
            lPlayer1Score.Visible = true;
            lPlayer2Score.Visible = true;
        }

        private void bTwoPlayers_Click(object sender, EventArgs e)
        {
            NewGame();
            IBoard board = kernel.Get<IBoard>(new ConstructorArgument("width", 7), new ConstructorArgument("height", 6));
            api.CreateGame(kernel.Get<IGameFactory>(), GameType.TwoPlayers, board);
        }

    }
}
