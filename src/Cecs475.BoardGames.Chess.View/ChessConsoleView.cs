using System;
using System.Text;
using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Model;
using Cecs475.BoardGames.View;

namespace Cecs475.BoardGames.Chess.View {
	/// <summary>
	/// A chess game view for string-based console input and output.
	/// </summary>
	public class ChessConsoleView : IConsoleView {
		private static char[] LABELS = { '.', 'P', 'R', 'N', 'B', 'Q', 'K' };
		
		// Public methods.
		public string BoardToString(ChessBoard board) {
			StringBuilder str = new StringBuilder();

			for (int i = 0; i < ChessBoard.BoardSize; i++) {
				str.Append(8 - i);
				str.Append(" ");
				for (int j = 0; j < ChessBoard.BoardSize; j++) {
					var space = board.GetPieceAtPosition(new BoardPosition(i, j));
					if (space.PieceType == ChessPieceType.Empty)
						str.Append(". ");
					else if (space.Player == 1)
						str.Append($"{LABELS[(int)space.PieceType]} ");
					else
						str.Append($"{char.ToLower(LABELS[(int)space.PieceType])} ");
				}
				str.AppendLine();
			}
			str.AppendLine("  a b c d e f g h");
			return str.ToString();
		}

		/// <summary>
		/// Converts the given ChessMove to a string representation in the form
		/// "(start, end)", where start and end are board positions in algebraic
		/// notation (e.g., "a5").
		/// 
		/// If this move is a pawn promotion move, the selected promotion piece 
		/// must also be in parentheses after the end position, as in 
		/// "(a7, a8, Queen)".
		/// </summary>
		public string MoveToString(ChessMove move) {
			StringBuilder str = new StringBuilder();
			str.Append("(");
			str.Append(PositionToString(move.StartPosition));
			str.Append(", ");
			str.Append(PositionToString(move.EndPosition));
			if (move.MoveType != ChessMoveType.Normal) {
				str.Append($", {move.MoveType}");
			}
			str.Append(")");
			return str.ToString();
		}

		public string PlayerToString(int player) {
			return player == 1 ? "White" : "Black";
		}

		/// <summary>
		/// Converts a string representation of a move into a ChessMove object.
		/// Must work with any string representation created by MoveToString.
		/// </summary>
		public ChessMove ParseMove(string moveText) {

			moveText = moveText.Replace("(", "");
			moveText = moveText.Replace(")", "");
			string[] split = moveText.Split(',');
			BoardPosition start = ParsePosition(split[0].Trim());
			BoardPosition end = ParsePosition(split[1].Trim());
			ChessMoveType type;
			if (split.Length > 2) {
				split[2] = split[2].Trim();
				if (split[2] == ChessMoveType.Normal.ToString()) {
					type = ChessMoveType.Normal;
				}
				else if (split[2] == ChessMoveType.CastleKingSide.ToString()) {
					type = ChessMoveType.CastleKingSide;
				}
				else if (split[2] == ChessMoveType.CastleQueenSide.ToString()) {
					type = ChessMoveType.CastleQueenSide;
				}
				else if (split[2] == ChessMoveType.EnPassant.ToString()) {
					type = ChessMoveType.EnPassant;
				}
				else if (split[2].Equals("Queen", StringComparison.InvariantCultureIgnoreCase)) {
					return new ChessMove(start, end, ChessPieceType.Queen, ChessMoveType.PawnPromote);
				}
				else if (split[2].Equals("Rook", StringComparison.InvariantCultureIgnoreCase)) {
					return new ChessMove(start, end, ChessPieceType.Rook, ChessMoveType.PawnPromote);
				}
				else if (split[2].Equals("Bishop", StringComparison.InvariantCultureIgnoreCase)) {
					return new ChessMove(start, end, ChessPieceType.Bishop, ChessMoveType.PawnPromote);
				}
				else if (split[2].Equals("Knight", StringComparison.InvariantCultureIgnoreCase)) {
					return new ChessMove(start, end, ChessPieceType.Knight, ChessMoveType.PawnPromote);
				}
				else { // hopefully never true?
					type = ChessMoveType.Normal;
				}
			}
			else {
				type = ChessMoveType.Normal;
			}
			//ChessMoveType type = split.Length > 2 ? (ChessMoveType)Enum.Parse(typeof(ChessMoveType), split[2], true) : ChessMoveType.Normal;
			ChessMove move = new ChessMove(start, end, type);
			return move;

		}

		public static BoardPosition ParsePosition(string pos) {
			return new BoardPosition(8 - (pos[1] - '0'), pos[0] - 'a');
		}

		public static string PositionToString(BoardPosition pos) {
			return $"{(char)(pos.Col + 'a')}{8 - pos.Row}";
		}

		#region Explicit interface implementations
		// Explicit method implementations. Do not modify these.
		string IConsoleView.BoardToString(IGameBoard board) {
			return BoardToString(board as ChessBoard);
		}

		string IConsoleView.MoveToString(IGameMove move) {
			return MoveToString(move as ChessMove);
		}

		IGameMove IConsoleView.ParseMove(string moveText) {
			return ParseMove(moveText);
		}
		#endregion
	}
}
