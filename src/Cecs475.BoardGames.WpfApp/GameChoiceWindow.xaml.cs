﻿using Cecs475.BoardGames.WpfView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cecs475.BoardGames.WpfApp {
	/// <summary>
	/// Interaction logic for GameChoiceWindow.xaml
	/// </summary>
	public partial class GameChoiceWindow : Window {
		public GameChoiceWindow() {
			Type type = typeof(IWpfGameFactory);
			string gamesFolder = @"games";
			var allfiles = Directory.GetFiles(gamesFolder, "*.*", SearchOption.AllDirectories);

			foreach (var file in allfiles) {
				FileInfo info = new FileInfo(file);
				if (info.Extension == ".dll") {
					Assembly.Load(info.Name + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=68e71c13048d452a");
				}
			}

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var filtered = new List<Type>();
			foreach (var assembly in assemblies) {
				var types = assembly.GetTypes();
				IEnumerable<Type> q = types.Where(t => type.IsAssignableFrom(t) && t.IsClass);
				filtered.AddRange(q);
			}

			List<IWpfGameFactory> wpfGameFactories = new List<IWpfGameFactory>();
			foreach (Type filteredType in filtered) {
				wpfGameFactories.Add((IWpfGameFactory) Activator.CreateInstance(filteredType));
			}

			this.Resources.Add("GameTypes", wpfGameFactories);

			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e) {
			Button b = sender as Button;
			// Retrieve the game type bound to the button
			IWpfGameFactory gameType = b.DataContext as IWpfGameFactory;
			// Construct a GameWindow to play the game.
			var gameWindow = new GameWindow(gameType,
				mHumanBtn.IsChecked.Value ? NumberOfPlayers.Two : NumberOfPlayers.One) {
				Title = gameType.GameName
			};
			// When the GameWindow closes, we want to show this window again.
			gameWindow.Closed += GameWindow_Closed;

			// Show the GameWindow, hide the Choice window.
			gameWindow.Show();
			this.Hide();
		}

		private void GameWindow_Closed(object sender, EventArgs e) {
			this.Show();
		}
	}
}
