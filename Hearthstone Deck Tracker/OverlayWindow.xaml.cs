﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;

namespace Hearthstone_Deck_Tracker
{
    /// <summary>
    ///     Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly Config _config;
        private Hearthstone _hearthstone;

        public OverlayWindow(Config config, Hearthstone hearthstone)
        {
            InitializeComponent();
            _config = config;
            _hearthstone = hearthstone;

            ListViewPlayer.ItemsSource = _hearthstone.PlayerDeck;
            ListViewEnemy.ItemsSource = _hearthstone.EnemyCards;


        }

        private void SortViews()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListViewPlayer.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            CollectionView view1 = (CollectionView)CollectionViewSource.GetDefaultView(ListViewEnemy.ItemsSource);
            view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
            view1.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void SetEnemyCardCount(int count)
        {
            LblEnemyCardCount.Content = "Cards in Hand: " + count;
        }

        private void SetCardCount(int p, int cardsLeftInDeck)
        {
            LblCardCount.Content = "Cards in Hand: " + p;
            if (cardsLeftInDeck <= 0) return;

            if (Hearthstone.IsUsingPremade)
            {

                LblDrawChance2.Content = "[2]: " + Math.Round(200.0f/cardsLeftInDeck, 2) + "%";
                LblDrawChance1.Content = "[1]: " + Math.Round(100.0f/cardsLeftInDeck, 2) + "%";
            }
            else
            {
                LblDrawChance2.Content = "[2]: " + Math.Round(200.0f / (30 - cardsLeftInDeck), 2) + "%";
                LblDrawChance1.Content = "[1]: " + Math.Round(100.0f / (30 - cardsLeftInDeck), 2) + "%";
            }
        }
    


        public void EnableCanvas(bool enable)
        {
            CanvasInfo.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
        }

        private void SetRect(int top, int left, int width, int height)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
            CanvasInfo.Height = height;
            CanvasInfo.Width = width;
        }

        private void ReSizePosLists()
        {
            ListViewEnemy.Height = 35 * ListViewEnemy.Items.Count * 1.07;
            ListViewPlayer.Height = 35 * ListViewPlayer.Items.Count * 1.07;

            ListViewPlayer.Width = ListViewPlayer.ActualWidth;
            
            Canvas.SetTop(ListViewEnemy, Height * 0.17);
            Canvas.SetTop(ListViewPlayer, Height * 0.17);
            Canvas.SetLeft(ListViewPlayer, Width - ListViewPlayer.Width - 5);

            Canvas.SetTop(LblDrawChance2, Height * 0.17 + ListViewPlayer.ActualHeight + 2 - (ListViewPlayer.Items.Count * 4));
            Canvas.SetLeft(LblDrawChance2, Width - (ListViewPlayer.Width / 2) - LblDrawChance1.ActualWidth/2 - 5 - LblDrawChance2.ActualWidth / 2);
            Canvas.SetTop(LblDrawChance1, Height * 0.17 + ListViewPlayer.ActualHeight + 2 - (ListViewPlayer.Items.Count *4));
            Canvas.SetLeft(LblDrawChance1, Width - (ListViewPlayer.Width / 2) - 5 - LblDrawChance1.ActualWidth/2 + LblDrawChance2.ActualWidth/2);

            Canvas.SetTop(LblCardCount, Height * 0.17 + ListViewPlayer.Height + 2 + LblDrawChance1.ActualHeight / 2 - (ListViewPlayer.Items.Count *4));
            Canvas.SetLeft(LblCardCount, Width - ListViewPlayer.Width / 2 - 5 - LblCardCount.ActualWidth / 2);

            Canvas.SetTop(LblEnemyCardCount, Height * 0.17 + ListViewEnemy.Height + 2 - (ListViewEnemy.Items.Count*4));
            Canvas.SetLeft(LblEnemyCardCount, 5 + ListViewEnemy.Width / 2 - LblEnemyCardCount.ActualWidth / 2);
        
        }

        private void Window_SourceInitialized_1(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            User32.SetWindowExTransparent(hwnd);
        }

        public void Update()
        {
            if (_config.HideDrawChances)
            {
                LblDrawChance1.Visibility = Visibility.Hidden;
                LblDrawChance2.Visibility = Visibility.Hidden;
            }
            else
            {
                LblDrawChance1.Visibility = Visibility.Visible;
                LblDrawChance2.Visibility = Visibility.Visible;
            }
            LblEnemyCardCount.Visibility = _config.HideEnemyCardCount ? Visibility.Hidden : Visibility.Visible;
            ListViewEnemy.Visibility = _config.HideEnemyCards ? Visibility.Hidden : Visibility.Visible;
            LblCardCount.Visibility = _config.HidePlayerCardCount ? Visibility.Hidden : Visibility.Visible;

            SetCardCount(_hearthstone.PlayerHandCount, _hearthstone.PlayerDeck.Sum(deckcard => deckcard.Count));
            SetEnemyCardCount(_hearthstone.EnemyHandCount);
            SortViews();
            ReSizePosLists();
        }

        public void UpdatePosition()
        {
            var hsRect = new User32.Rect();
            User32.GetWindowRect(User32.FindWindow(null, "Hearthstone"), ref hsRect);
            SetRect(hsRect.top, hsRect.left, hsRect.right - hsRect.left, hsRect.bottom - hsRect.top);
            ReSizePosLists();
        }
    }
}