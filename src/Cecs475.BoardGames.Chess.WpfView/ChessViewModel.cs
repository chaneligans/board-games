using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Model;
using Cecs475.BoardGames.WpfView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Cecs475.BoardGames.ComputerOpponent;



namespace Cecs475.BoardGames.Chess.WpfView {

	/// <summary>
	/// Represents one square on the Chess board grid.
	/// </summary>
	public class ChessSquare : INotifyPropertyChanged {
		private int mPlayer;
		/// <summary>
		/// The player that has a piece in the given square, or 0 if empty.
		/// </summary>
		public int Player {
			get { return mPlayer; }
			set {
				if (value != mPlayer) {
					mPlayer = value;
					OnPropertyChanged(nameof(Player));
				}
			}
		}

		private ChessPiece mPiece;
		public ChessPiece Piece {
			get { return mPiece; }
			set {
				if (!mPiece.Equals(value)) {
					mPiece = value;
					OnPropertyChanged(nameof(Piece));
				}
			}
		}

		/// <summary>
		/// The position of the square.
		/// </summary>
		public BoardPosition Position {
			get; set;
		}


		private bool mIsHighlighted;
		/// <summary>
		/// Whether the square should be highlighted because of a user action.
		/// </summary>
		public bool IsHighlighted {
			get { return mIsHighlighted; }
			set {
				if (value != mIsHighlighted) {
					mIsHighlighted = value;
					OnPropertyChanged(nameof(IsHighlighted));
				}
			}
		}

		private bool mIsSelected;
		/// <summary>
		/// Whether the square should be selected because of a user action.
		/// </summary>
		public bool IsSelected {
			get { return mIsSelected; }
			set {
				if (value != mIsSelected) {
					mIsSelected = value;
					OnPropertyChanged(nameof(IsSelected));
				}
			}
		}

		private bool mIsCheck;
		/// <summary>
		/// Whether the square should be highlighted because a king is in check.
		/// </summary>
		public bool IsCheck
		{
			get { return mIsCheck; }
			set
			{
				if (value != mIsCheck)
				{
					mIsCheck = value;
					OnPropertyChanged(nameof(IsCheck));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}


	public class ChessViewModel : INotifyPropertyChanged, IGameViewModel {

		private ChessBoard mBoard;
		private ObservableCollection<ChessSquare> mSquares;
		public event EventHandler GameFinished;
		private const int MAX_AI_DEPTH = 4;
		private IGameAi mGameAi = new MinimaxAi(MAX_AI_DEPTH);

		public ChessViewModel() {
			mBoard = new ChessBoard();

			if (Players == NumberOfPlayers.One && !mBoard.IsFinished) {
				var bestMove = mGameAi.FindBestMove(mBoard);
				if (bestMove != null) {
					mBoard.ApplyMove(bestMove as ChessMove);
				}
			}


			// Initialize the squares objects based on the board's initial state.
			mSquares = new ObservableCollection<ChessSquare>(
				BoardPosition.GetRectangularPositions(8, 8)
				.Select(pos => new ChessSquare() {
					Position = pos,
					Player = mBoard.GetPlayerAtPosition(pos),
					Piece = mBoard.GetPieceAtPosition(pos)
				})
			);

			PossibleStartMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.StartPosition
			);

			PossibleEndMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.EndPosition
			);

			PossibleMoves = new HashSet<ChessMove>(mBoard.GetPossibleMoves());
		}

		/// <summary>
		/// Applies a move for the current player at the given position.
		/// </summary>
		public async Task ApplyMove(BoardPosition startPosition, BoardPosition endPosition, ChessPieceType type) {
			var possMoves = mBoard.GetPossibleMoves() as IEnumerable<ChessMove>;
			ChessMove promoMove;
			// Validate the move as possible.

			foreach (var move in possMoves) {
				if (move.StartPosition.Equals(startPosition) && move.EndPosition.Equals(endPosition) && mBoard.GetPieceAtPosition(startPosition).PieceType == type) {
					mBoard.ApplyMove(move);
					break;
				}
				else if (move.StartPosition.Equals(startPosition) && move.EndPosition.Equals(endPosition) && mBoard.GetPieceAtPosition(startPosition).PieceType != type)
				{
					promoMove = new ChessMove(startPosition, endPosition, type, ChessMoveType.PawnPromote);
					mBoard.ApplyMove(promoMove);
					break;
				}
			}

			if (Players == NumberOfPlayers.One && !mBoard.IsFinished)
			{
				var bestMove = await Task.Run( ()=>mGameAi.FindBestMove(mBoard) );
				if (bestMove != null)
				{
					mBoard.ApplyMove(bestMove as ChessMove);
				}
			}

			RebindState();

			if (mBoard.IsFinished) {
				GameFinished?.Invoke(this, new EventArgs());
			}
		}

		private void RebindState() {
			// Rebind the possible moves, now that the board has changed.
			PossibleEndMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.EndPosition
			);

			PossibleStartMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.StartPosition
			);

			PossibleMoves = new HashSet<ChessMove>(mBoard.GetPossibleMoves());

			// Update the collection of squares by examining the new board state.
			var newSquares = BoardPosition.GetRectangularPositions(8, 8);
			int i = 0;
			foreach (var pos in newSquares) {
				mSquares[i].Player = mBoard.GetPlayerAtPosition(pos);
				mSquares[i].Piece = mBoard.GetPieceAtPosition(pos);

				if (mBoard.GetPieceAtPosition(pos).PieceType == ChessPieceType.King && mBoard.GetPlayerAtPosition(pos) == CurrentPlayer && mBoard.IsCheck) {
					mSquares[i].IsCheck = true;
				}
				else
				{
					mSquares[i].IsCheck = false;
				}
				
				i++;
			}
			OnPropertyChanged(nameof(BoardAdvantage));
			OnPropertyChanged(nameof(CurrentPlayer));
			OnPropertyChanged(nameof(CanUndo));
		}

		/// <summary>
		/// A collection of 64 ChessSquare objects representing the state of the 
		/// game board.
		/// </summary>
		public ObservableCollection<ChessSquare> Squares {
			get { return mSquares; }
		}

		/// <summary>
		/// A set of board moves where the current player can move.
		/// </summary>
		public HashSet<ChessMove> PossibleMoves
		{
			get; private set;
		}

		/// <summary>
		/// A set of board end positions where the current player can move.
		/// </summary>
		public HashSet<BoardPosition> PossibleEndMoves {
			get; private set;
		}

		/// <summary>
		/// A set of board start positions where the current player can move.
		/// </summary>
		public HashSet<BoardPosition> PossibleStartMoves
		{
			get; private set;
		}

		/// <summary>
		/// The player whose turn it currently is.
		/// </summary>
		public int CurrentPlayer {
			get { return mBoard.CurrentPlayer; }
		}

		/// <summary>
		/// The value of the chess board.
		/// </summary>

		public GameAdvantage BoardAdvantage => mBoard.CurrentAdvantage;

		public bool CanUndo => mBoard.MoveHistory.Any();

		public NumberOfPlayers Players { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public void UndoMove() {
			if (CanUndo) {
				mBoard.UndoLastMove();
				// In one-player mode, Undo has to remove an additional move to return to the
				// human player's turn.
				if (Players == NumberOfPlayers.One && CanUndo) {
					mBoard.UndoLastMove();
				}
				RebindState();
			}
		}
	}

}
