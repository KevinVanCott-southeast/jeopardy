﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Jeopardy
{
    public partial class frmMain : Form
    {
        private List<Game> allGames;
        private Game selectedGame;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Loading Games...";
            bwLoadGames.RunWorkerAsync(); //start background thread to load games
        }

        //Load the game list in a background thread so it does not freeze the form
        private void bwLoadGames_DoWork(object sender, DoWorkEventArgs e)
        {
            allGames = DB_Select.SelectAllGameInfo(); //now selects only top level game info, not the categories and questions, etc.
        }

        //Show the games in the list box once the background thread has finished loading the games
        private void bwLoadGames_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RefreshListBox();
        }

        //MARK: Selected Index Change Event Handler
        private void lstGamesFromDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableAllControls();
        }

        //MARK Button Event Handlers
        private void btnPlayGame_Click(object sender, EventArgs e) //Play Game Button
        {
            UseWaitCursor = true;
            lblStatus.Text = "Loading Game...";
            DisableAllControls();
            bwLoadGameToPlay.RunWorkerAsync();
        }

        private void bwLoadGameToPlay_DoWork(object sender, DoWorkEventArgs e)
        {
            //don't load here anymore, load in the team form to make it appear faster
        }

        private void bwLoadGameToPlay_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UseWaitCursor = false;
            frmTeams teamsForm = new frmTeams(selectedGame);
            Hide();
            teamsForm.ShowDialog();
            EnableAllControls();
            Show();
        }

        private void btnCreateGame_Click(object sender, EventArgs e) //Create Game Button
        {
            DisableAllControls();
            frmCreateGame createGameForm = new frmCreateGame();
            Hide();
            createGameForm.ShowDialog();
            if (!bwLoadGames.IsBusy)
            {
                bwLoadGames.RunWorkerAsync();
            }
            Show();
        }

        private void btnEditGame_Click(object sender, EventArgs e) //Edit Game Button
        {
            UseWaitCursor = true;
            lblStatus.Text = "Loading Game...";
            DisableAllControls();
            bwLoadGameToEdit.RunWorkerAsync();
        }

        private void bwLoadGameToEdit_DoWork(object sender, DoWorkEventArgs e)
        {
            selectedGame = DB_Select.SelectGame_ByGameId(selectedGame.Id);
        }

        private void bwLoadGameToEdit_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UseWaitCursor = false;
            frmEditGame createGameForm = new frmEditGame(selectedGame);
            Hide();
            createGameForm.ShowDialog();
            bwLoadGames.RunWorkerAsync();
            Show();
        }

        private void btnDeleteGame_Click(object sender, EventArgs e) //Delete Game button
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this game? All questions in this game will also be deleted.", "Confirm Delete", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                UseWaitCursor = true;
                lblStatus.Text = "Deleting Game...";
                DisableAllControls();
                bwDeleteGame.RunWorkerAsync();
            }
        }

        private void bwDeleteGame_DoWork(object sender, DoWorkEventArgs e)
        {
            int numRows = DB_Delete.DeleteGame(selectedGame.Id);
        }

        private void bwDeleteGame_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UseWaitCursor = false;
            bwLoadGames.RunWorkerAsync();
        }

        private void btnExportGame_Click(object sender, EventArgs e) //Export Game button
        {
            UseWaitCursor = true;
            lblStatus.Text = "Exporting...";
            DisableAllControls();
            bwLoadGameToExport.RunWorkerAsync();
        }

        private void bwLoadGameToExport_DoWork(object sender, DoWorkEventArgs e)
        {
            selectedGame = DB_Select.SelectGame_ByGameId(selectedGame.Id);
        }

        private void bwLoadGameToExport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UseWaitCursor = false;
            XML_IO.exportXML(selectedGame);
            EnableAllControls();
        }

        private void btnImportGame_Click(object sender, EventArgs e) //Import Game button
        {
            lblStatus.Text = "Importing...";
            DisableAllControls();
            using (var fbd = new OpenFileDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.FileName))
                {
                    if (Path.GetExtension(fbd.FileName) == ".xml")
                    {
                        string fileName = Path.GetFileNameWithoutExtension(fbd.FileName);
                        XML_IO.importXML(fbd.FileName, fileName);
                    }
                    else
                    {
                        MessageBox.Show("The file needs to be an xml");
                    }
                }
                bwLoadGames.RunWorkerAsync();
            }
        }

        //MARK Tool Strip Event Handlers - Most of these just trigger the associated button event handlers
        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnCreateGame_Click(sender, e);
        }

        private void editGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnEditGame_Click(sender, e);
        }

        private void deleteGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDeleteGame_Click(sender, e);
        }

        private void importGameFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnImportGame_Click(sender, e);
        }

        private void exportGameToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnExportGame_Click(sender, e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form about = new frmAbout();
            about.ShowDialog();
        }

        private void helpToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            DB_Conn.OpenHelpFile();
        }

        private void troubleshootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Forms.Admin.frmTroubleshooter TroubleshooterForm = new Forms.Admin.frmTroubleshooter();
            TroubleshooterForm.ShowDialog();
            bwLoadGames.RunWorkerAsync();
        }

        //MARK: Other Private Utility Methods
        private void RefreshListBox()
        {
            lstGamesFromDB.Items.Clear();
            foreach (Game g in allGames)
            {
                lstGamesFromDB.Items.Add(g.GameName);
            }
            EnableAllControls();
        }

        private void DisableAllControls()
        {
            lstGamesFromDB.Enabled = false;

            newGameToolStripMenuItem.Enabled = false;
            editGameToolStripMenuItem.Enabled = false;
            deleteGameToolStripMenuItem.Enabled = false;
            importGameFromFileToolStripMenuItem.Enabled = false;
            exportGameToFileToolStripMenuItem.Enabled = false;

            btnPlayGame.Enabled = false;
            btnCreateGame.Enabled = false;
            btnEditGame.Enabled = false;
            btnDeleteGame.Enabled = false;
            btnImportGame.Enabled = false;
            btnExportGame.Enabled = false;
        }

        private void EnableAllControls()
        {
            lblStatus.Text = "";

            if (lstGamesFromDB.Enabled == false)
            {
                lstGamesFromDB.Enabled = true;
            }

            if (lstGamesFromDB.SelectedIndex != -1)
            {
                selectedGame = allGames[lstGamesFromDB.SelectedIndex];

                newGameToolStripMenuItem.Enabled = true;
                editGameToolStripMenuItem.Enabled = true;
                deleteGameToolStripMenuItem.Enabled = true;
                importGameFromFileToolStripMenuItem.Enabled = true;
                exportGameToFileToolStripMenuItem.Enabled = true;

                btnPlayGame.Enabled = true;
                btnCreateGame.Enabled = true;
                btnEditGame.Enabled = true;
                btnDeleteGame.Enabled = true;
                btnImportGame.Enabled = true;
                btnExportGame.Enabled = true;

                btnPlayGame.Focus();
            }
            else //disable buttons that need a game to be selected to work
            {
                lblStatus.Text = "Select a Game";

                newGameToolStripMenuItem.Enabled = true;
                editGameToolStripMenuItem.Enabled = false;
                deleteGameToolStripMenuItem.Enabled = false;
                importGameFromFileToolStripMenuItem.Enabled = true;
                exportGameToFileToolStripMenuItem.Enabled = false;

                btnPlayGame.Enabled = false;
                btnCreateGame.Enabled = true;
                btnEditGame.Enabled = false;
                btnDeleteGame.Enabled = false;
                btnImportGame.Enabled = true;
                btnExportGame.Enabled = false;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //DB_Conn.CompactAndRepair();
        }
    }
}
