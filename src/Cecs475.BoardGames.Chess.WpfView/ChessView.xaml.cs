using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.WpfView;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cecs475.BoardGames.Chess.WpfView {
	/// <summary>
	/// Interaction logic for OthelloView.xaml
	/// </summary>
	public partial class ChessView : UserControl, IWpfGameView {
		public ChessViewModel ChessViewModel => FindResource("vm") as ChessViewModel;
		public Control ViewControl => this;
		public IGameViewModel ViewModel => ChessViewModel;

		public ChessView() {
			InitializeComponent();
		}

		private void Border_MouseEnter(object sender, MouseEventArgs e) {
			if (!IsEnabled)
			{
				return;
			}

			Border b = sender as Border;
			var square = b.DataContext as ChessSquare;
			var vm = FindResource("vm") as ChessViewModel;
			IEnumerable<ChessSquare> selectedSquares = vm.Squares.Where(s => s.IsSelected == true);

			if (selectedSquares.Count() == 0)
			{
				//square hovered (start pos) is possible
				if (vm.PossibleStartMoves.Contains(square.Position))
				{
					// user has selected a piece 
					square.IsHighlighted = true;
				}
			}
			else if (selectedSquares.Count() == 1)
			{
				//list of possible moves with current start and end points
				IEnumerable<ChessMove> PossibleMovesList = vm.PossibleMoves.Where(s => (s.EndPosition == square.Position) & (s.StartPosition == selectedSquares.FirstOrDefault().Position));

				//square hovered(end pos) is possible
				if (PossibleMovesList.Count() > 0)
				{
					// user has selected a piece 
					square.IsHighlighted = true;
				}
			}
		}

		private void Border_MouseLeave(object sender, MouseEventArgs e) {
			Border b = sender as Border;
			var square = b.DataContext as ChessSquare;
			square.IsHighlighted = false;
		}

		private async void Border_MouseUp(object sender, MouseButtonEventArgs e) {
			if (!IsEnabled)
			{
				return;
			}
			Border b = sender as Border;
			var square = b.DataContext as ChessSquare;
			var vm = FindResource("vm") as ChessViewModel;

			IEnumerable<ChessSquare> playerSquares = vm.Squares.Where(s => s.Player == vm.CurrentPlayer);
			IEnumerable<ChessSquare> selectedSquares = vm.Squares.Where(s => s.IsSelected == true );
			
			//first click, select piece to be moved
			if (selectedSquares.Count() == 0) {
				if (playerSquares.Contains(square))
				{
					square.IsSelected = true;
					square.IsHighlighted = false;
				}
			}

			//second click
			else if (selectedSquares.Count() == 1) {

				//click on possible move, move is applied
				if (vm.PossibleEndMoves.Contains(square.Position))
				{
					if (selectedSquares.FirstOrDefault().Piece.PieceType == ChessPieceType.Pawn) {
						if (selectedSquares.FirstOrDefault().Player == 1 && square.Position.Row == 0) {
							var p = new PawnPromotion(vm, selectedSquares.FirstOrDefault().Position, square.Position);
							p.ShowDialog();
						}
						else if (selectedSquares.FirstOrDefault().Player == 2 && square.Position.Row == 7) {
							var p = new PawnPromotion(vm, selectedSquares.FirstOrDefault().Position, square.Position);
							p.ShowDialog();
						}
						else
						{
							IsEnabled = false;
							await vm.ApplyMove(selectedSquares.FirstOrDefault().Position, square.Position, selectedSquares.FirstOrDefault().Piece.PieceType);
							IsEnabled = true;
						}
					}
					else
					{
						IsEnabled = false;
						await vm.ApplyMove(selectedSquares.FirstOrDefault().Position, square.Position, selectedSquares.FirstOrDefault().Piece.PieceType);
						IsEnabled = true;
					}
					square.IsHighlighted = false;
					selectedSquares.FirstOrDefault().IsSelected = false;
				}

				//click on not possible move, move is cancelled and is no longer selected
				else
				{
					selectedSquares.FirstOrDefault().IsSelected = false;
				}
			}

		}

	}
}
