using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Model;
using System;
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
using System.Windows.Shapes;

namespace Cecs475.BoardGames.Chess.WpfView
{
	/// <summary>
	/// Interaction logic for PawnPromotion.xaml
	/// </summary>
	public partial class PawnPromotion : Window
	{
		private BoardPosition StartPosition { get; set; }
		private BoardPosition EndPosition { get; set; }
		private ChessViewModel vm { get; set; }

		public PawnPromotion(ChessViewModel viewM, BoardPosition start, BoardPosition end)
		{
			InitializeComponent();

			StartPosition = start;
			EndPosition = end;

			vm = viewM;
			
			if (vm.CurrentPlayer == 1)
			{
				black_knight.Visibility = Visibility.Hidden;
				black_bishop.Visibility = Visibility.Hidden;
				black_rook.Visibility = Visibility.Hidden;
				black_queen.Visibility = Visibility.Hidden;
			}
			else
			{
				white_knight.Visibility = Visibility.Hidden;
				white_bishop.Visibility = Visibility.Hidden;
				white_rook.Visibility = Visibility.Hidden;
				white_queen.Visibility = Visibility.Hidden;
			}
		}

		private void Promo_MouseEnter(object sender, MouseEventArgs e)
		{
			Border b = sender as Border;
			Image i = sender as Image;

			if (b == border_knight || i == black_knight || i == white_knight)
			{
				border_knight.Background = Brushes.CornflowerBlue;
			}

			if (b == border_bishop || i == black_bishop || i == white_bishop)
			{
				border_bishop.Background = Brushes.CornflowerBlue;
			}

			if (b == border_rook || i == black_rook || i == white_rook)
			{
				border_rook.Background = Brushes.CornflowerBlue;
			}

			if (b == border_queen || i == black_queen || i == white_queen)
			{
				border_queen.Background = Brushes.CornflowerBlue;
			}
		}

		private void Promo_MouseLeave(object sender, MouseEventArgs e)
		{
			Border b = sender as Border;
			Image i = sender as Image;

			if (b == border_knight || i == black_knight || i == white_knight)
			{
				border_knight.Background = Brushes.White;
			}

			if (b == border_bishop || i == black_bishop || i == white_bishop)
			{
				border_bishop.Background = Brushes.White;
			}

			if (b == border_rook || i == black_rook || i == white_rook)
			{
				border_rook.Background = Brushes.White;
			}

			if (b == border_queen || i == black_queen || i == white_queen)
			{
				border_queen.Background = Brushes.White;
			}
		}

		private async void Promo_MouseUp(object sender, MouseEventArgs e)
		{
			Border b = sender as Border;
			Image i = sender as Image;

			if (b == border_knight || i == black_knight || i == white_knight)
			{
				await vm.ApplyMove(StartPosition, EndPosition, ChessPieceType.Knight);
				this.Hide();
			}

			if (b == border_bishop || i == black_bishop || i == white_bishop)
			{
				await vm.ApplyMove(StartPosition, EndPosition, ChessPieceType.Bishop);
				this.Hide();
			}

			if (b == border_rook || i == black_rook || i == white_rook)
			{
				await vm.ApplyMove(StartPosition, EndPosition, ChessPieceType.Rook);
				this.Hide();
			}

			if (b == border_queen || i == black_queen || i == white_queen)
			{
				await vm.ApplyMove(StartPosition, EndPosition, ChessPieceType.Queen);
				this.Hide();
			}

			this.Hide();
		}

	}
}
