using System;
using System.Collections.Generic;
using System.Text;
using Cecs475.BoardGames.Model;
using System.Linq;

namespace Cecs475.BoardGames.Chess.Model {
	/// <summary>
	/// Represents the board state of a game of chess. Tracks which squares of the 8x8 board are occupied
	/// by which player's pieces.
	/// </summary>
	public class ChessBoard : IGameBoard
	{
		#region Member fields.
		// The history of moves applied to the board.
		private List<ChessMove> mMoveHistory = new List<ChessMove>();
		private List<int> mNumMovesNoPawnOrCaptureHistory = new List<int>(new int[] { 0 });


		private List<bool> mMoveIsCaptureHistory = new List<bool>();

		public const int BoardSize = 8;

		// constant piece values
		public const int PawnValue = 1;
		public const int BishopValue = 3;
		public const int KnightValue = 3;
		public const int RookValue = 5;
		public const int QueenValue = 9;


		// create a field for the board position array. You can hand-initialize
		// the starting entries of the array, or set them in the constructor.
		private byte[] mBoard;

		// Add a means of tracking miscellaneous board state, like captured pieces and the 50-move rule.

		//Need to do protect
		public long BoardWeight
		{
			get
			{
				long whiteOwnership = new GameAdvantage(1, mAdvantageValue).Advantage;
				long blackOwnership = new GameAdvantage(2, mAdvantageValue * -1).Advantage;
				long whitePawnMove = 0;
				long blackPawnMove = 0;
				long whiteProtect = 0;
				long blackProtect = 0;
				long whiteThreaten = 0;
				long blackThreaten = 0;

				var boardPositions = BoardPosition.GetRectangularPositions(8, 8);
				var whiteAttack = GetAttackedPositions(1);
				var blackAttack = GetAttackedPositions(2);

				foreach (BoardPosition pos in boardPositions) {
					var cPiece = GetPieceAtPosition(pos);
					if (cPiece.PieceType == ChessPieceType.Pawn && cPiece.Player == 1) {
						whitePawnMove += 6 - pos.Row;
					}
					else if (cPiece.PieceType == ChessPieceType.Pawn && cPiece.Player == 2) {
						blackPawnMove += pos.Row - 1;
					}
				}

				foreach (BoardPosition pos in whiteAttack) {
					var cPiece = GetPieceAtPosition(pos);
					if (cPiece.PieceType == ChessPieceType.Knight || cPiece.PieceType == ChessPieceType.Bishop && cPiece.Player == 1)
					{
						whiteThreaten += 1;
					}
					else if (cPiece.PieceType == ChessPieceType.Rook && cPiece.Player == 1)
					{
						whiteThreaten += 2;
					}
					else if (cPiece.PieceType == ChessPieceType.Queen && cPiece.Player == 1)
					{
						whiteThreaten += 5;
					}
					else if (cPiece.PieceType == ChessPieceType.King && cPiece.Player == 1)
					{
						whiteThreaten += 4;
					}
				}

				foreach (BoardPosition pos in blackAttack) {
					var cPiece = GetPieceAtPosition(pos);
					if (cPiece.PieceType == ChessPieceType.Knight || cPiece.PieceType == ChessPieceType.Bishop && cPiece.Player == 2)
					{
						blackThreaten += 1;
					}
					else if (cPiece.PieceType == ChessPieceType.Rook && cPiece.Player == 2)
					{
						blackThreaten += 2;
					}
					else if (cPiece.PieceType == ChessPieceType.Queen && cPiece.Player == 2)
					{
						blackThreaten += 5;
					}
					else if (cPiece.PieceType == ChessPieceType.King && cPiece.Player == 2)
					{
						blackThreaten += 4;
					}
				}

				return (whiteOwnership + whitePawnMove + whiteProtect + whiteThreaten) - (blackOwnership + blackPawnMove + blackProtect + blackThreaten);
			}
		}
		private List<ChessPiece> CapturedPieces { get; set; }
		private ISet<BoardPosition> tempList { get; set; }
		private int NumMovesNoPawnOrCapture { get; set; }
		private List<BoardPosition> WhitePawnStartingPositions { get; set; }
		private List<BoardPosition> BlackPawnStartingPositions { get; set; }
		private bool BlackA8RookMoved { get; set; }
		private bool BlackH8RookMoved { get; set; }
		private bool BlackKingMoved { get; set; }
		private bool WhiteA1RookMoved { get; set; }
		private bool WhiteH1RookMoved { get; set; }
		private bool WhiteKingMoved { get; set; }
		private bool LastMoveDoubleStep { get; set; }
		private BoardPosition DoubledSteppedPawn { get; set; }
		private bool MoveIsCapture;
		private BoardPosition ForwardPromotionPos;
		private BoardPosition LeftPromotionPos;
		private BoardPosition RightPromotionPos;

		// add a field for tracking the current player and the board advantage.		
		private int mAdvantageValue;
		private int mCurrentPlayer; // 1 = black, 2 = white
		#endregion
		private string x;

		#region Properties.
		// implement these properties.
		// You can choose to use auto properties, computed properties, or normal properties 
		// using a private field to back the property.

		// You can add set bodies if you think that is appropriate, as long as you justify
		// the access level (public, private).


		public bool IsFinished { get { return IsCheckmate || IsStalemate || IsDraw; } }

		public int CurrentPlayer { get { return mCurrentPlayer == 1 ? 1 : 2; } }

		public GameAdvantage CurrentAdvantage
		{
			get { return new GameAdvantage(mAdvantageValue > 0 ? 1 : mAdvantageValue < 0 ? 2 : 0, Math.Abs(mAdvantageValue)); }
		}

		public IReadOnlyList<ChessMove> MoveHistory => mMoveHistory;

		// implement IsCheck, IsCheckmate, IsStalemate
		public bool IsCheck
		{
			get { return PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, mCurrentPlayer).First(), mCurrentPlayer == 1 ? 2 : 1) && GetPossibleMoves().Count() > 0; }
		}

		public bool IsCheckmate
		{
			get { return PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, mCurrentPlayer).First(), mCurrentPlayer == 1 ? 2 : 1) && (GetPossibleMoves().Count() == 0); }
		}

		public bool IsStalemate
		{
			get { return GetPossibleMoves().Count() == 0 && !IsCheckmate; }
		}

		public bool IsDraw
		{
			get { return DrawCounter >= 100; }
		}

		/// <summary>
		/// Tracks the current draw counter, which goes up by 1 for each non-capturing, non-pawn move, and resets to 0
		/// for other moves. If the counter reaches 100 (50 full turns), the game is a draw.
		/// </summary>
		public int DrawCounter
		{
			get { return NumMovesNoPawnOrCapture; }
		}
		#endregion


		#region Public methods.

		/// <summary>
		/// TO DO: Castling, En Passant, Check, Pawn promotion
		/// </summary>
		public IEnumerable<ChessMove> GetPossibleMoves()
		{
			int Player = mCurrentPlayer == 1 ? 1 : 2;
			int otherPlayer = mCurrentPlayer == 1 ? 2 : 1;

			ISet<ChessMove> possibleMoves = new HashSet<ChessMove>();
			ISet<ChessMove> tempMoveList = new HashSet<ChessMove>();

			ISet<ChessMove> pawnMoves = PawnMove(Player);
			ISet<ChessMove> rookMoves = RookMove(Player);
			ISet<ChessMove> knightMoves = KnightMove(Player);
			ISet<ChessMove> bishopMoves = BishopMove(Player);
			ISet<ChessMove> queenMoves = QueenMove(Player);
			ISet<ChessMove> kingMoves = KingMove(Player);

			possibleMoves.UnionWith(pawnMoves);
			possibleMoves.UnionWith(rookMoves);
			possibleMoves.UnionWith(knightMoves);
			possibleMoves.UnionWith(bishopMoves);
			possibleMoves.UnionWith(queenMoves);
			possibleMoves.UnionWith(kingMoves);

			tempMoveList.UnionWith(possibleMoves);

			int preTest = DrawCounter;

			// can only return moves that put u out of check
			//if king is being attacked
			if (PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, Player).First(), otherPlayer))
			{
				//for each move in possible moves, check if king is being attacked after applying move then undoing the move
				foreach (ChessMove move in tempMoveList)
				{
					ApplyMove(move);
					//if king is still being attacked, remove the move from possiblemoves
					if (PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, Player).First(), otherPlayer))
					{
						possibleMoves.Remove(move);
					}
					UndoLastMove();
				}
			}

			tempMoveList.IntersectWith(possibleMoves);
			foreach (ChessMove move in tempMoveList)
			{
				ApplyMove(move);
				//if king is being attacked after move, remove the move from possiblemoves
				if (PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, Player).First(), otherPlayer))
				{
					possibleMoves.Remove(move);
				}
				UndoLastMove();
			}
			NumMovesNoPawnOrCapture = preTest; // proba  bad way to solve the prob of updating draw counter
			x += "\n\n" + BoardToString();
			return new List<ChessMove>(possibleMoves);
		}

		/// <summary>
		/// Does:
		/// 1. determine if the move is a capture
		/// 2. add the move to the movehistory list
		/// 3. update NumMovesNoPawnOrCapture
		/// 4. update the advantage
		/// 5. update king/rook moved variables
		/// 6. apply the move to the board (move piece, set old position to empty)
		/// 7. update to the next player
		/// </summary>
		public void ApplyMove(ChessMove m)
		{
			// STRONG RECOMMENDATION: any mutation to the board state should be run
			// through the method SetPieceAtPosition.
			
			ChessPiece startPiece = GetPieceAtPosition(m.StartPosition);
			ChessPieceType startType = startPiece.PieceType;
			ChessPiece endPiece = GetPieceAtPosition(m.EndPosition);
			ChessPieceType endType = GetPieceAtPosition(m.EndPosition).PieceType;
			int movingPlayer = startPiece.Player;
			
			
			if (m.Player == 0) {
				m.Player = movingPlayer;
			}
			mMoveHistory.Add(m);

			BoardPosition A1 = new BoardPosition(7, 0);
			BoardPosition H1 = new BoardPosition(7, 7);
			BoardPosition A8 = new BoardPosition(0, 0);
			BoardPosition H8 = new BoardPosition(0, 7);
			BoardPosition E1 = new BoardPosition(7, 4);
			BoardPosition E8 = new BoardPosition(0, 4);

			ChessPieceType f = GetPieceAtPosition(m.StartPosition).PieceType;
			if (m.MoveType == ChessMoveType.PawnPromote)
			{
				startPiece = new ChessPiece(m.PromoteTo, movingPlayer);
				UpdateAdvantage(ChessPieceType.Pawn, -movingPlayer); // calculate advantage for loss of pawn
				UpdateAdvantage(m.PromoteTo, movingPlayer); // calculate advantage for new piece
			}

			MoveIsCapture = false;
			if (PositionIsAttacked(m.EndPosition, movingPlayer) && !PositionIsEmpty(m.EndPosition))
			{
				MoveIsCapture = true;
				CapturedPieces.Add(endPiece);
				UpdateAdvantage(endType, movingPlayer); // calculate advantage
			}

			// if en passant, captures pawn and removes it from board
			else if (m.MoveType == (ChessMoveType)3)
			{
				MoveIsCapture = true;
				var lastMove = mMoveHistory[mMoveHistory.Count - 2];
				LastMoveDoubleStep = GetPieceAtPosition(lastMove.EndPosition).PieceType == ChessPieceType.Pawn
					&& Math.Abs(lastMove.EndPosition.Row - lastMove.StartPosition.Row) == 2;
				DoubledSteppedPawn = lastMove.EndPosition;

				CapturedPieces.Add(GetPieceAtPosition(DoubledSteppedPawn));
				UpdateAdvantage(GetPieceAtPosition(DoubledSteppedPawn).PieceType, movingPlayer);
				SetPieceAtPosition(DoubledSteppedPawn, new ChessPiece(ChessPieceType.Empty, 0));
			}
			mMoveIsCaptureHistory.Add(MoveIsCapture);

			//Checks if the move was a doublestep
			if (startType == ChessPieceType.Pawn && (Math.Abs(m.StartPosition.Row - m.EndPosition.Row) == 2))
			{
				LastMoveDoubleStep = true;
				DoubledSteppedPawn = m.EndPosition;
			}
			else
			{
				LastMoveDoubleStep = false;
			}

			// update NumMovesNoPawnOrCapture 
			// 50 move rule: // something got captured or a pawn moved => reset it 
			if (startType.Equals(ChessPieceType.Pawn) || MoveIsCapture)
			{
				NumMovesNoPawnOrCapture = 0;
			}
			else
			{
				// no piece has been captured and no pawn has moved
				NumMovesNoPawnOrCapture += 1;
			}
			mNumMovesNoPawnOrCaptureHistory.Add(NumMovesNoPawnOrCapture);

			// Checks if rooks or kings are moved from starting position
			WhiteA1RookMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(7, 0)) || cm.EndPosition.Equals(new BoardPosition(7, 0))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 0)).Equals(new ChessPiece(ChessPieceType.Rook, 1));
			WhiteH1RookMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(7, 7)) || cm.EndPosition.Equals(new BoardPosition(7, 7))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 7)).Equals(new ChessPiece(ChessPieceType.Rook, 1));
			BlackA8RookMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(0, 0)) || cm.EndPosition.Equals(new BoardPosition(0, 0))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 0)).Equals(new ChessPiece(ChessPieceType.Rook, 2));
			BlackH8RookMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(0, 7)) || cm.EndPosition.Equals(new BoardPosition(0, 7))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 7)).Equals(new ChessPiece(ChessPieceType.Rook, 2));
			WhiteKingMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(7, 4)) || cm.EndPosition.Equals(new BoardPosition(7, 4))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 4)).Equals(new ChessPiece(ChessPieceType.King, 1));
			BlackKingMoved = MoveHistory.Where(cm => cm.StartPosition.Equals(new BoardPosition(0, 4)) || cm.EndPosition.Equals(new BoardPosition(0, 4))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 4)).Equals(new ChessPiece(ChessPieceType.King, 2));

			// if castling, also move rook
			if (m.StartPosition.Equals(E1) && GetPieceAtPosition(m.StartPosition).Equals(new ChessPiece(ChessPieceType.King, movingPlayer)))
			{
				if (m.MoveType == ChessMoveType.CastleQueenSide)
				{
					//apply move for A1 rook to move to D1
					SetPieceAtPosition(new BoardPosition(7, 3), GetPieceAtPosition(A1));
					SetPieceAtPosition(A1, new ChessPiece(ChessPieceType.Empty, 0));
					WhiteA1RookMoved = true;
				}
				else if (m.MoveType == ChessMoveType.CastleKingSide)
				{
					//apply move for H1 rook to move to F1
					SetPieceAtPosition(new BoardPosition(7, 5), GetPieceAtPosition(H1));
					SetPieceAtPosition(H1, new ChessPiece(ChessPieceType.Empty, 0));
					WhiteH1RookMoved = true;
				}
			}
			else if (m.StartPosition.Equals(E8) && GetPieceAtPosition(m.StartPosition).Equals(new ChessPiece(ChessPieceType.King, movingPlayer)))
			{
				if (m.MoveType == ChessMoveType.CastleQueenSide)
				{
					//apply move for A8 rook to move to D8
					SetPieceAtPosition(new BoardPosition(0, 3), GetPieceAtPosition(A8));
					SetPieceAtPosition(A8, new ChessPiece(ChessPieceType.Empty, 0));
					BlackA8RookMoved = true;
				}
				else if (m.MoveType == ChessMoveType.CastleKingSide)
				{
					//apply move for H8 rook to move to F8
					SetPieceAtPosition(new BoardPosition(0, 5), GetPieceAtPosition(H8));
					SetPieceAtPosition(H8, new ChessPiece(ChessPieceType.Empty, 0));
					BlackH8RookMoved = true;
				}
			}
			// update the board
			SetPieceAtPosition(m.EndPosition, startPiece);
			SetPieceAtPosition(m.StartPosition, new ChessPiece(ChessPieceType.Empty, 0));

			// update the player
			mCurrentPlayer = (movingPlayer == 2 ? 1 : 2);
		}


		/// <summary>
		/// Undoes:
		/// 1. determine if the move is a capture???
		/// 2. remove the move from the movehistory list
		/// 3. update NumMovesNoPawnOrCapture
		/// 4. update the advantage
		/// 5. update king/rook moved variables
		/// 6. apply the move to the board (move piece, set old position to empty)
		/// 7. update to the previous player
		/// </summary>
		public void UndoLastMove()
		{
			ChessMove LastMove = mMoveHistory.Last();
			mMoveHistory.RemoveAt(mMoveHistory.Count - 1);

			ChessPiece piece = GetPieceAtPosition(LastMove.EndPosition);

			if (LastMove.MoveType == ChessMoveType.CastleQueenSide)
			{
				// put queenside rooks back
				if (LastMove.Player == 1)
				{ // white
					SetPieceAtPosition(new BoardPosition(7, 0), new ChessPiece(ChessPieceType.Rook, 1));
					SetPieceAtPosition(new BoardPosition(7, 3), new ChessPiece(ChessPieceType.Empty, 0));
					WhiteA1RookMoved = false;
					WhiteKingMoved = false;

				}
				else
				{ // black
					SetPieceAtPosition(new BoardPosition(0, 0), new ChessPiece(ChessPieceType.Rook, 2));
					SetPieceAtPosition(new BoardPosition(0, 3), new ChessPiece(ChessPieceType.Empty, 0));
					BlackA8RookMoved = false;
					BlackKingMoved = false;
				}
			}
			else if (LastMove.MoveType == ChessMoveType.CastleKingSide)
			{
				// put kingside rooks back
				if (LastMove.Player == 1)
				{ // white
					SetPieceAtPosition(new BoardPosition(7, 7), new ChessPiece(ChessPieceType.Rook, 1));
					SetPieceAtPosition(new BoardPosition(7, 5), new ChessPiece(ChessPieceType.Empty, 0));
					WhiteH1RookMoved = false;
					WhiteKingMoved = false;
				}
				else
				{ // black
					SetPieceAtPosition(new BoardPosition(0, 7), new ChessPiece(ChessPieceType.Rook, 2));
					SetPieceAtPosition(new BoardPosition(0, 5), new ChessPiece(ChessPieceType.Empty, 0));
					BlackH8RookMoved = false;
					BlackKingMoved = false;
				}
			}
			else if (LastMove.MoveType == ChessMoveType.PawnPromote)
			{
				UpdateAdvantage(piece.PieceType, -LastMove.Player); // lose a promoted piece
				UpdateAdvantage(ChessPieceType.Pawn, LastMove.Player); // gain a pawn
				piece = new ChessPiece(ChessPieceType.Pawn, piece.Player);
			}

			mNumMovesNoPawnOrCaptureHistory.RemoveAt(mNumMovesNoPawnOrCaptureHistory.Count - 1);
			NumMovesNoPawnOrCapture = NumMovesNoPawnOrCapture == 0 ? mNumMovesNoPawnOrCaptureHistory.Last() : NumMovesNoPawnOrCapture - 1;

			MoveIsCapture = mMoveIsCaptureHistory.Last();
			mMoveIsCaptureHistory.RemoveAt(mMoveIsCaptureHistory.Count - 1);

			if (MoveIsCapture && LastMove.MoveType == ChessMoveType.EnPassant)
			{
				ChessMove lastDoubleMove = mMoveHistory.Last();

				ChessPiece captured = CapturedPieces.Last();
				SetPieceAtPosition(lastDoubleMove.EndPosition, captured); // put the piece back
				SetPieceAtPosition(LastMove.EndPosition, new ChessPiece(ChessPieceType.Empty, 0)); // empty spot where capturing pawn is
				CapturedPieces.RemoveAt(CapturedPieces.Count - 1);

				// calculate advantage: Multiply player by -1 to subtract the advantage from their score
				UpdateAdvantage(captured.PieceType, -LastMove.Player);
			}
			else if (MoveIsCapture)
			{
				ChessPiece captured = CapturedPieces.Last();
				SetPieceAtPosition(LastMove.EndPosition, captured); // put the piece back
				CapturedPieces.RemoveAt(CapturedPieces.Count - 1);

				// calculate advantage: Multiply player by -1 to subtract the advantage from their score
				UpdateAdvantage(captured.PieceType, -LastMove.Player);
			}
			else
			{
				SetPieceAtPosition(LastMove.EndPosition, new ChessPiece(ChessPieceType.Empty, 0));
			}

			SetPieceAtPosition(LastMove.StartPosition, piece);

			if (LastMove.MoveType != ChessMoveType.CastleKingSide && LastMove.MoveType != ChessMoveType.CastleQueenSide)
			{
				WhiteA1RookMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(7, 0)) || m.EndPosition.Equals(new BoardPosition(7, 0))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 0)).Equals(new ChessPiece(ChessPieceType.Rook, 1));
				WhiteH1RookMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(7, 7)) || m.EndPosition.Equals(new BoardPosition(7, 7))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 7)).Equals(new ChessPiece(ChessPieceType.Rook, 1));
				BlackA8RookMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(0, 0)) || m.EndPosition.Equals(new BoardPosition(0, 0))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 0)).Equals(new ChessPiece(ChessPieceType.Rook, 2));
				BlackH8RookMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(0, 7)) || m.EndPosition.Equals(new BoardPosition(0, 7))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 7)).Equals(new ChessPiece(ChessPieceType.Rook, 2));
				WhiteKingMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(7, 4)) || m.EndPosition.Equals(new BoardPosition(7, 4))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(7, 4)).Equals(new ChessPiece(ChessPieceType.King, 1));
				BlackKingMoved = MoveHistory.Where(m => m.StartPosition.Equals(new BoardPosition(0, 4)) || m.EndPosition.Equals(new BoardPosition(0, 4))).Count() > 0 || !GetPieceAtPosition(new BoardPosition(0, 4)).Equals(new ChessPiece(ChessPieceType.King, 2));
			}


			// update the player
			mCurrentPlayer = (mCurrentPlayer == 2 ? 1 : 2);

		}

		/// <summary>
		/// Returns whatever chess piece is occupying the given position.
		/// </summary>
		public ChessPiece GetPieceAtPosition(BoardPosition position)
		{
			// transform the position into an array index
			int index = (position.Row * 4) + (position.Col / 2);

			byte pos;
			// bit mask to strip off the 4 bits associated with that position
			if (position.Col % 2 == 0)
			{
				// use left-most 4 bits
				pos = (byte)(mBoard[index] >> 4);
			}
			else
			{
				// use right-most
				pos = (byte)(mBoard[index] & 0b_0000_1111);
			}

			// bit mask to determine the player & piece at the position
			byte bPlayer = (byte)(pos & 0b_1000);
			byte bPiece = (byte)(pos & 0b_0111);

			int player;

			// no piece = empty spot
			if (bPiece == 0b_0000)
			{
				player = 0;
			}
			else
			{
				player = (bPlayer == 0b_0 ? 1 : 2);
			}

			ChessPiece piece = new ChessPiece((ChessPieceType)bPiece, player);
			return piece;
		}

		/// <summary>
		/// Returns whatever player is occupying the given position.
		/// </summary>
		public int GetPlayerAtPosition(BoardPosition pos)
		{
			// As a hint, you should call GetPieceAtPosition.
			// 0: no player
			if (PositionInBounds(pos))
			{
				ChessPiece piece = GetPieceAtPosition(pos);
				int player = piece.Player; // player 1 or 2
				return player;
			}
			return 0; // no player
		}

		/// <summary>
		/// Returns true if the given position on the board is empty.
		/// </summary>
		/// <remarks>returns false if the position is not in bounds</remarks>
		public bool PositionIsEmpty(BoardPosition pos)
		{
			if (PositionInBounds(pos))
			{
				ChessPiece piece = GetPieceAtPosition(pos);
				return piece.PieceType == 0;
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given position contains a piece that is the enemy of the given player.
		/// </summary>
		/// <remarks>returns false if the position is not in bounds</remarks>
		public bool PositionIsEnemy(BoardPosition pos, int player)
		{
			return PositionInBounds(pos) && (player != GetPieceAtPosition(pos).Player) && (GetPieceAtPosition(pos).PieceType != ChessPieceType.Empty);
		}

		/// <summary>
		/// Returns true if the given position is in the bounds of the board.
		/// </summary>
		public static bool PositionInBounds(BoardPosition pos)
		{
			return pos.Row >= 0 && pos.Col >= 0 && pos.Row < BoardSize && pos.Col < BoardSize; // 0-7?
		}

		/// <summary>
		/// Returns all board positions where the given piece can be found.
		/// </summary>
		public IEnumerable<BoardPosition> GetPositionsOfPiece(ChessPieceType piece, int player)
		{
			BoardPosition pos;
			ChessPiece current;
			List<BoardPosition> positions = new List<BoardPosition>();
			for (int row = 0; row < BoardSize; row++)
			{
				for (int col = 0; col < BoardSize; col++)
				{
					pos = new BoardPosition(row, col);
					current = GetPieceAtPosition(pos);
					if (current.PieceType == piece && current.Player == player)
					{
						positions.Add(pos);
					}
				}
			}
			return positions;
		}

		/// <summary>
		/// Returns true if the given player's pieces are attacking the given position.
		/// </summary>
		public bool PositionIsAttacked(BoardPosition position, int byPlayer)
		{
			ISet<BoardPosition> attackedPositions = new HashSet<BoardPosition>();
			attackedPositions = GetAttackedPositions(byPlayer);

			if (attackedPositions.Contains(position))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by the given player.
		/// </summary>
		public ISet<BoardPosition> GetAttackedPositions(int byPlayer)
		{
			ISet<BoardPosition> attackedPositions = new HashSet<BoardPosition>();

			ISet<BoardPosition> pawnAttacks = PawnAttack(byPlayer);
			ISet<BoardPosition> rookAttacks = RookAttack(byPlayer);
			ISet<BoardPosition> knightAttacks = KnightAttack(byPlayer);
			ISet<BoardPosition> bishopAttacks = BishopAttack(byPlayer);
			ISet<BoardPosition> queenAttacks = QueenAttack(byPlayer);
			ISet<BoardPosition> kingAttacks = KingAttack(byPlayer);

			attackedPositions.UnionWith(pawnAttacks);
			attackedPositions.UnionWith(rookAttacks);
			attackedPositions.UnionWith(knightAttacks);
			attackedPositions.UnionWith(bishopAttacks);
			attackedPositions.UnionWith(queenAttacks);
			attackedPositions.UnionWith(kingAttacks);
			tempList = attackedPositions;
			return attackedPositions;
		}
		#endregion

		#region Private methods.

		///<summary>
		/// Update the current advantage given a piece type and player.
		///</summary>
		private void UpdateAdvantage(ChessPieceType type, int player)
		{
			// positive multiplier if player == 1, negative multiplier if player == 2
			int playerMultiplier;

			// if player is negative, we are p much undoing an update 
			if (player < 0)
			{
				playerMultiplier = player == -1 ? -1 : 1;
			}
			else
			{
				playerMultiplier = player == 1 ? 1 : -1;
			}

			if (type == ChessPieceType.Pawn)
			{
				mAdvantageValue += playerMultiplier * PawnValue;
			}
			else if (type == ChessPieceType.Bishop)
			{
				mAdvantageValue += playerMultiplier * BishopValue;
			}
			else if (type == ChessPieceType.Knight)
			{
				mAdvantageValue += playerMultiplier * KnightValue;
			}
			else if (type == ChessPieceType.Rook)
			{
				mAdvantageValue += playerMultiplier * RookValue;
			}
			else if (type == ChessPieceType.Queen)
			{
				mAdvantageValue += playerMultiplier * QueenValue;
			}
		}


		/// <summary>
		/// Mutates the board state so that the given piece is at the given position.
		/// </summary>
		private void SetPieceAtPosition(BoardPosition position, ChessPiece piece)
		{
			byte player = (byte)((piece.Player == 1) ? 0b_0 : 0b_1);
			int pieceType = (int)piece.PieceType;

			// taken from #6 from lab5; combine the player and piecetype bits
			byte elt = (byte)((player << 3) | pieceType); // 4 bits

			// get the index
			int index = (position.Row * 4) + (position.Col / 2);

			byte pos;
			// check if the position should be left or right most bits
			if (position.Col % 2 == 0)
			{
				// use the left 4 bits
				// take the 8bit number at the index that we want to replace
				// and convert the current left bits to 0
				pos = (byte)(mBoard[index] & 0b_0000_1111);

				// convert the new thing to 8bit by shifting it left
				elt = (byte)(elt << 4);
			}
			else
			{
				// use the right 4 bits
				// take the 8bit number at the index that we want to replace
				// and convert the current right bits to 0
				pos = (byte)(mBoard[index] & 0b_1111_0000);
			}

			// or the new one with the old one to place the new bits into their position
			// sets it on the board
			mBoard[index] = (byte)(pos | elt);
			//x = BoardToString();
		}


		private bool EnPassantPossible(BoardPosition pawnPos)
		{
			if (mMoveHistory.Count > 0)
			{
				var lastMove = mMoveHistory.Last();
				LastMoveDoubleStep = GetPieceAtPosition(lastMove.EndPosition).PieceType == ChessPieceType.Pawn
					&& Math.Abs(lastMove.EndPosition.Row - lastMove.StartPosition.Row) == 2;
				DoubledSteppedPawn = lastMove.EndPosition;

				if (CurrentPlayer == 1 && pawnPos.Row == 3 && LastMoveDoubleStep)
				{
					if (DoubledSteppedPawn.Col == pawnPos.Col - 1 || DoubledSteppedPawn.Col == pawnPos.Col + 1)
					{
						return true;
					}
				}
				else if (CurrentPlayer == 2 && pawnPos.Row == 4 && LastMoveDoubleStep)
				{
					if (DoubledSteppedPawn.Col == pawnPos.Col - 1 || DoubledSteppedPawn.Col == pawnPos.Col + 1)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool PromotionPossible(BoardPosition pawnPos)
		{
			BoardPosition tempPos;
			BoardPosition leftTempPos;
			BoardPosition rightTempPos;

			if (mCurrentPlayer == 1)
			{
				tempPos = new BoardPosition(pawnPos.Row - 1, pawnPos.Col);
				leftTempPos = new BoardPosition(pawnPos.Row - 1, pawnPos.Col - 1);
				rightTempPos = new BoardPosition(pawnPos.Row - 1, pawnPos.Col + 1);
				if (pawnPos.Row == 1 && (PositionIsEmpty(tempPos) || PawnAttack(1).Contains(leftTempPos) || PawnAttack(1).Contains(rightTempPos)))
				{
					if (PositionIsEmpty(tempPos))
					{
						ForwardPromotionPos = pawnPos;
					}
					if (PawnAttack(1).Contains(leftTempPos) && PositionInBounds(leftTempPos) && !PositionIsEmpty(leftTempPos))
					{
						LeftPromotionPos = pawnPos;
					}
					if (PawnAttack(1).Contains(rightTempPos) && PositionInBounds(rightTempPos) && !PositionIsEmpty(rightTempPos))
					{
						RightPromotionPos = pawnPos;
					}
					return true;
				}
			}
			else
			{
				tempPos = new BoardPosition(pawnPos.Row + 1, pawnPos.Col);
				leftTempPos = new BoardPosition(tempPos.Row, tempPos.Col - 1);
				rightTempPos = new BoardPosition(tempPos.Row, tempPos.Col + 1);
				bool a = PositionIsEmpty(tempPos);
				bool b = PawnAttack(2).Contains(leftTempPos);
				bool c = PawnAttack(2).Contains(rightTempPos);
				if (pawnPos.Row == 6 && (PositionIsEmpty(tempPos) || PawnAttack(2).Contains(leftTempPos) || PawnAttack(2).Contains(rightTempPos)))
				{
					if (PositionIsEmpty(tempPos))
					{
						ForwardPromotionPos = pawnPos;
					}
					if (PawnAttack(2).Contains(leftTempPos) && PositionInBounds(leftTempPos) && !PositionIsEmpty(leftTempPos))
					{
						LeftPromotionPos = pawnPos;
					}
					if (PawnAttack(2).Contains(rightTempPos) && PositionInBounds(rightTempPos) && !PositionIsEmpty(rightTempPos))
					{
						RightPromotionPos = pawnPos;
					}
					return true;
				}
			}
			return false;
		}

		private bool CastlingPossible(BoardPosition rookPos)
		{
			BoardPosition oneAwayFromKing;
			BoardPosition twoAwayFromKing;
			BoardPosition threeAwayFromKing;

			//White Rook A1
			if (rookPos.Equals(new BoardPosition(7, 0)))
			{
				oneAwayFromKing = new BoardPosition(7, 3);
				twoAwayFromKing = new BoardPosition(7, 2);
				threeAwayFromKing = new BoardPosition(7, 1);
				//Checks for rook not moved, king not moved, king not in check, king doesn't pass through attacked/occupied squares, king doesn't end in check
				if (!WhiteA1RookMoved
						&& !WhiteKingMoved
						&& GetPieceAtPosition(new BoardPosition(7, 4)).PieceType.Equals(ChessPieceType.King)
						&& !PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, 1).First(), 2)
						&& PositionIsEmpty(oneAwayFromKing)
						&& PositionIsEmpty(twoAwayFromKing)
						&& PositionIsEmpty(threeAwayFromKing)
						&& !PositionIsAttacked(oneAwayFromKing, 2)
						&& !PositionIsAttacked(twoAwayFromKing, 2))
				{
					return true;
				}
			}
			//White Rook H1
			else if (rookPos.Equals(new BoardPosition(7, 7)))
			{
				oneAwayFromKing = new BoardPosition(7, 5);
				twoAwayFromKing = new BoardPosition(7, 6);
				//Checks for rook not moved, king not moved, king not in check, king doesn't pass through attacked/occupied squares, king doesn't end in check
				if (!WhiteH1RookMoved
						&& !WhiteKingMoved
						&& GetPieceAtPosition(new BoardPosition(7, 4)).PieceType.Equals(ChessPieceType.King)
						&& !PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, 1).First(), 2)
						&& PositionIsEmpty(oneAwayFromKing)
						&& PositionIsEmpty(twoAwayFromKing)
						&& !PositionIsAttacked(oneAwayFromKing, 2)
						&& !PositionIsAttacked(twoAwayFromKing, 2))
				{
					return true;
				}
			}
			//Black Rook A8
			else if (rookPos.Equals(new BoardPosition(0, 0)))
			{
				oneAwayFromKing = new BoardPosition(0, 3);
				twoAwayFromKing = new BoardPosition(0, 2);
				threeAwayFromKing = new BoardPosition(0, 1);
				//Checks for rook not moved, king not moved, king not in check, king doesn't pass through attacked/occupied squares, king doesn't end in check
				if (!BlackA8RookMoved
						&& !BlackKingMoved
						&& GetPieceAtPosition(new BoardPosition(0, 4)).PieceType.Equals(ChessPieceType.King)
						&& !PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, 2).First(), 1)
						&& PositionIsEmpty(oneAwayFromKing)
						&& PositionIsEmpty(twoAwayFromKing)
						&& PositionIsEmpty(threeAwayFromKing)
						&& !PositionIsAttacked(oneAwayFromKing, 1)
						&& !PositionIsAttacked(twoAwayFromKing, 1))
				{
					return true;
				}
			}
			//Black Rook H8
			else if (rookPos.Equals(new BoardPosition(0, 7)))
			{
				oneAwayFromKing = new BoardPosition(0, 5);
				twoAwayFromKing = new BoardPosition(0, 6);
				//Checks for rook not moved, king not moved, king not in check, king doesn't pass through attacked/occupied squares, king doesn't end in check
				if (!BlackH8RookMoved
						&& !BlackKingMoved
						&& GetPieceAtPosition(new BoardPosition(0, 4)).PieceType.Equals(ChessPieceType.King)
						&& !PositionIsAttacked(GetPositionsOfPiece(ChessPieceType.King, 2).First(), 1)
						&& PositionIsEmpty(oneAwayFromKing)
						&& PositionIsEmpty(twoAwayFromKing)
						&& !PositionIsAttacked(oneAwayFromKing, 1)
						&& !PositionIsAttacked(twoAwayFromKing, 1))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's pawn.
		/// </summary>
		private ISet<ChessMove> PawnMove(int byPlayer)
		{
			ISet<ChessMove> pawnMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece(ChessPieceType.Pawn, byPlayer);
			BoardPosition tempPos;
			ChessMove tempMove;
			Boolean forwardPromote;
			Boolean leftPromote;
			Boolean rightPromote;
			BoardPosition midUp;
			BoardPosition midUpTwo;
			BoardPosition upLeft;
			BoardPosition upRight;

			BoardPosition midDown;
			BoardPosition midDownTwo;
			BoardPosition downLeft;
			BoardPosition downRight;

			//if player is controlling white pieces (bottom)
			if (byPlayer == 1)
			{
				foreach (BoardPosition pos in positions)
				{
					tempPos = pos;
					forwardPromote = false;
					leftPromote = false;
					rightPromote = false;

					midUp = new BoardPosition(pos.Row - 1, pos.Col);
					midUpTwo = new BoardPosition(pos.Row - 2, pos.Col);
					upLeft = new BoardPosition(pos.Row - 1, pos.Col - 1);
					upRight = new BoardPosition(pos.Row - 1, pos.Col + 1);

					if (PromotionPossible(pos))
					{
						if (pos.Equals(ForwardPromotionPos))
						{
							tempMove = new ChessMove(pos, midUp, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midUp, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midUp, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midUp, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							forwardPromote = true;
						}
						if (pos.Equals(LeftPromotionPos))
						{
							tempMove = new ChessMove(pos, upLeft, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upLeft, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upLeft, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upLeft, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							leftPromote = true;
						}
						if (pos.Equals(RightPromotionPos))
						{
							tempMove = new ChessMove(pos, upRight, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upRight, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upRight, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, upRight, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							rightPromote = true;
						}
					}

					if (WhitePawnStartingPositions.Contains(pos) && PositionIsEmpty(midUpTwo) && PositionIsEmpty(midUp))
					{
						tempMove = new ChessMove(pos, midUpTwo);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEmpty(midUp) && forwardPromote == false)
					{
						tempMove = new ChessMove(pos, midUp);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEnemy(upLeft, byPlayer) && leftPromote == false)
					{
						tempMove = new ChessMove(pos, upLeft);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEnemy(upRight, byPlayer) && rightPromote == false)
					{
						tempMove = new ChessMove(pos, upRight);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (EnPassantPossible(pos))
					{
						tempPos = new BoardPosition(DoubledSteppedPawn.Row - 1, DoubledSteppedPawn.Col);
						tempMove = new ChessMove(pos, tempPos, (ChessMoveType)3);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
				}
			}

			//if player is controlling black pieces
			else
			{
				foreach (BoardPosition pos in positions)
				{
					forwardPromote = false;
					leftPromote = false;
					rightPromote = false;

					midDown = new BoardPosition(pos.Row + 1, pos.Col);
					midDownTwo = new BoardPosition(pos.Row + 2, pos.Col);
					downLeft = new BoardPosition(pos.Row + 1, pos.Col - 1);
					downRight = new BoardPosition(pos.Row + 1, pos.Col + 1);

					if (pos.Row == 6 && pos.Col == 7)
					{
						BoardPosition afd = new BoardPosition(pos.Row + 1, pos.Col);
						BoardPosition leftTempPos = new BoardPosition(afd.Row, afd.Col - 1);
						BoardPosition rightTempPos = new BoardPosition(afd.Row, afd.Col + 1);
						bool a = PositionIsEmpty(afd);
						bool b = PawnAttack(2).Contains(leftTempPos);
						bool c = PawnAttack(2).Contains(afd);
						bool pp = PromotionPossible(pos);
					}

					if (PromotionPossible(pos))
					{
						if (pos.Equals(ForwardPromotionPos))
						{
							tempMove = new ChessMove(pos, midDown, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midDown, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midDown, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, midDown, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							forwardPromote = true;
						}
						if (pos.Equals(LeftPromotionPos))
						{
							tempMove = new ChessMove(pos, downLeft, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downLeft, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downLeft, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downLeft, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							leftPromote = true;
						}
						if (pos.Equals(RightPromotionPos))
						{
							tempMove = new ChessMove(pos, downRight, (ChessPieceType)2, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downRight, (ChessPieceType)3, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downRight, (ChessPieceType)4, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							tempMove = new ChessMove(pos, downRight, (ChessPieceType)5, (ChessMoveType)4);
							tempMove.Player = byPlayer;
							pawnMoveList.Add(tempMove);

							rightPromote = true;
						}
					}

					if (BlackPawnStartingPositions.Contains(pos) && PositionIsEmpty(midDown) && PositionIsEmpty(midDownTwo))
					{
						tempMove = new ChessMove(pos, midDownTwo);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEmpty(midDown) && forwardPromote == false)
					{
						tempMove = new ChessMove(pos, midDown);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEnemy(downLeft, byPlayer) && leftPromote == false)
					{
						tempMove = new ChessMove(pos, downLeft);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (PositionIsEnemy(downRight, byPlayer) && rightPromote == false)
					{
						tempMove = new ChessMove(pos, downRight);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
					if (EnPassantPossible(pos))
					{
						tempPos = new BoardPosition(DoubledSteppedPawn.Row + 1, DoubledSteppedPawn.Col);
						tempMove = new ChessMove(pos, tempPos, (ChessMoveType)3);
						tempMove.Player = byPlayer;
						pawnMoveList.Add(tempMove);
					}
				}
			}
			return pawnMoveList;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's rook.
		/// </summary>
		private ISet<ChessMove> RookMove(int byPlayer)
		{
			ISet<ChessMove> rookMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)2, byPlayer);
			BoardPosition tempPos;
			ChessMove tempMove;

			foreach (BoardPosition pos in positions)
			{
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);

				while (PositionIsEmpty(middleLeft))
				{
					tempMove = new ChessMove(pos, middleLeft);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
					middleLeft = middleLeft.Translate(0, -1);
				}
				if (PositionIsEnemy(middleLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, middleLeft);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleUp))
				{
					tempMove = new ChessMove(pos, middleUp);
					rookMoveList.Add(tempMove);
					middleUp = middleUp.Translate(1, 0);
				}
				if (PositionIsEnemy(middleUp, byPlayer))
				{
					tempMove = new ChessMove(pos, middleUp);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleDown))
				{
					tempMove = new ChessMove(pos, middleDown);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
					middleDown = middleDown.Translate(-1, 0);
				}
				if (PositionIsEnemy(middleDown, byPlayer))
				{
					tempMove = new ChessMove(pos, middleDown);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleRight))
				{
					tempMove = new ChessMove(pos, middleRight);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
					middleRight = middleRight.Translate(0, 1);
				}
				if (PositionIsEnemy(middleRight, byPlayer))
				{
					tempMove = new ChessMove(pos, middleRight);
					tempMove.Player = byPlayer;
					rookMoveList.Add(tempMove);
				}

			}
			return rookMoveList;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's knight.
		/// </summary>
		private ISet<ChessMove> KnightMove(int byPlayer)
		{
			ISet<ChessMove> knightMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)3, byPlayer);
			List<BoardPosition> movePositions;
			BoardPosition tempPos;
			ChessMove tempMove;

			foreach (BoardPosition pos in positions)
			{
				movePositions = new List<BoardPosition>();
				tempPos = pos;
				///naming convention of variables: twospaces[direction]onespace[direction]
				///ex: leftUp means twospaces[left]onespace[up]
				BoardPosition leftUp = tempPos.Translate(1, -2);
				tempPos = pos;
				BoardPosition upLeft = tempPos.Translate(2, -1);
				tempPos = pos;
				BoardPosition rightUp = tempPos.Translate(1, 2);
				tempPos = pos;
				BoardPosition upRight = tempPos.Translate(2, 1);
				tempPos = pos;
				BoardPosition leftDown = tempPos.Translate(-1, -2);
				tempPos = pos;
				BoardPosition downLeft = tempPos.Translate(-2, -1);
				tempPos = pos;
				BoardPosition rightDown = tempPos.Translate(-1, 2);
				tempPos = pos;
				BoardPosition downRight = tempPos.Translate(-2, 1);

				movePositions.Add(leftUp);
				movePositions.Add(upLeft);
				movePositions.Add(rightUp);
				movePositions.Add(upRight);
				movePositions.Add(leftDown);
				movePositions.Add(downLeft);
				movePositions.Add(rightDown);
				movePositions.Add(downRight);

				foreach (BoardPosition movePos in movePositions)
				{
					if (PositionIsEnemy(movePos, byPlayer) || PositionIsEmpty(movePos))
					{
						tempMove = new ChessMove(pos, movePos);
						tempMove.Player = byPlayer;
						knightMoveList.Add(tempMove);
					}
				}
			}
			return knightMoveList;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's bishop.
		/// </summary>
		private ISet<ChessMove> BishopMove(int byPlayer)
		{
			ISet<ChessMove> bishopMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)4, byPlayer);
			BoardPosition tempPos;
			ChessMove tempMove;

			foreach (BoardPosition pos in positions)
			{
				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				while (PositionIsEmpty(topLeft))
				{
					tempMove = new ChessMove(pos, topLeft);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
					topLeft = topLeft.Translate(1, -1);
				}
				if (PositionIsEnemy(topLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, topLeft);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(bottomLeft))
				{
					tempMove = new ChessMove(pos, bottomLeft);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
					bottomLeft = bottomLeft.Translate(-1, -1);
				}
				if (PositionIsEnemy(bottomLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, bottomLeft);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(topRight))
				{
					tempMove = new ChessMove(pos, topRight);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
					topRight = topRight.Translate(1, 1);
				}
				if (PositionIsEnemy(topRight, byPlayer))
				{
					tempMove = new ChessMove(pos, topRight);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(bottomRight))
				{
					tempMove = new ChessMove(pos, bottomRight);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
					bottomRight = bottomRight.Translate(-1, 1);
				}
				if (PositionIsEnemy(bottomRight, byPlayer))
				{
					tempMove = new ChessMove(pos, bottomRight);
					tempMove.Player = byPlayer;
					bishopMoveList.Add(tempMove);
				}
			}
			return bishopMoveList;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's queen.
		/// </summary>
		private ISet<ChessMove> QueenMove(int byPlayer)
		{
			ISet<ChessMove> queenMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)5, byPlayer);
			BoardPosition tempPos;
			ChessMove tempMove;

			foreach (BoardPosition pos in positions)
			{

				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				while (PositionIsEmpty(topLeft))
				{
					tempMove = new ChessMove(pos, topLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					topLeft = topLeft.Translate(1, -1);
				}
				if (PositionIsEnemy(topLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, topLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleLeft))
				{
					tempMove = new ChessMove(pos, middleLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					middleLeft = middleLeft.Translate(0, -1);
				}
				if (PositionIsEnemy(middleLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, middleLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(bottomLeft))
				{
					tempMove = new ChessMove(pos, bottomLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					bottomLeft = bottomLeft.Translate(-1, -1);
				}
				if (PositionIsEnemy(bottomLeft, byPlayer))
				{
					tempMove = new ChessMove(pos, bottomLeft);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleUp))
				{
					tempMove = new ChessMove(pos, middleUp);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					middleUp = middleUp.Translate(1, 0);
				}
				if (PositionIsEnemy(middleUp, byPlayer))
				{
					tempMove = new ChessMove(pos, middleUp);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleDown))
				{
					tempMove = new ChessMove(pos, middleDown);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					middleDown = middleDown.Translate(-1, 0);
				}
				if (PositionIsEnemy(middleDown, byPlayer))
				{
					tempMove = new ChessMove(pos, middleDown);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(topRight))
				{
					tempMove = new ChessMove(pos, topRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					topRight = topRight.Translate(1, 1);
				}
				if (PositionIsEnemy(topRight, byPlayer))
				{
					tempMove = new ChessMove(pos, topRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(middleRight))
				{
					tempMove = new ChessMove(pos, middleRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					middleRight = middleRight.Translate(0, 1);
				}
				if (PositionIsEnemy(middleRight, byPlayer))
				{
					tempMove = new ChessMove(pos, middleRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}

				while (PositionIsEmpty(bottomRight))
				{
					tempMove = new ChessMove(pos, bottomRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
					bottomRight = bottomRight.Translate(-1, 1);
				}
				if (PositionIsEnemy(bottomRight, byPlayer))
				{
					tempMove = new ChessMove(pos, bottomRight);
					tempMove.Player = byPlayer;
					queenMoveList.Add(tempMove);
				}
			}
			return queenMoveList;
		}

		/// <summary>
		/// Returns a set of all possible ChessMoves for a player's king.
		/// </summary>
		private ISet<ChessMove> KingMove(int byPlayer)
		{
			ISet<ChessMove> kingMoveList = new HashSet<ChessMove>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)6, byPlayer);
			List<BoardPosition> movePositions;
			BoardPosition tempPos;
			ChessMove tempMove;

			BoardPosition WhiteKingPos = new BoardPosition(7, 4);
			BoardPosition BlackKingPos = new BoardPosition(0, 4);
			if (byPlayer == 1)
			{
				if (CastlingPossible(new BoardPosition(7, 0)))
				{ //a1
					tempMove = new ChessMove(WhiteKingPos, new BoardPosition(7, 2), ChessMoveType.CastleQueenSide);
					tempMove.Player = byPlayer;
					kingMoveList.Add(tempMove);
				}
				// h1
				if (CastlingPossible(new BoardPosition(7, 7)))
				{
					tempMove = new ChessMove(WhiteKingPos, new BoardPosition(7, 6), ChessMoveType.CastleKingSide);
					tempMove.Player = byPlayer;
					kingMoveList.Add(tempMove);
				}
			}
			else
			{
				if (CastlingPossible(new BoardPosition(0, 0)))
				{ // a8
					tempMove = new ChessMove(BlackKingPos, new BoardPosition(0, 2), ChessMoveType.CastleQueenSide);
					tempMove.Player = byPlayer;
					kingMoveList.Add(tempMove);
				}
				if (CastlingPossible(new BoardPosition(0, 7)))
				{ // h8
					tempMove = new ChessMove(BlackKingPos, new BoardPosition(0, 6), ChessMoveType.CastleKingSide);
					tempMove.Player = byPlayer;
					kingMoveList.Add(tempMove);
				}
			}

			foreach (BoardPosition pos in positions)
			{
				movePositions = new List<BoardPosition>();

				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				movePositions.Add(topLeft);
				movePositions.Add(middleLeft);
				movePositions.Add(bottomLeft);
				movePositions.Add(middleUp);
				movePositions.Add(middleDown);
				movePositions.Add(topRight);
				movePositions.Add(middleRight);
				movePositions.Add(bottomRight);

				foreach (BoardPosition movePos in movePositions)
				{
					if (PositionIsEnemy(movePos, byPlayer) || PositionIsEmpty(movePos))
					{
						tempMove = new ChessMove(pos, movePos);
						tempMove.Player = byPlayer;
						kingMoveList.Add(tempMove);
					}
				}

			}
			return kingMoveList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's pawn.
		/// </summary>
		private ISet<BoardPosition> PawnAttack(int byPlayer)
		{
			ISet<BoardPosition> pawnAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)1, byPlayer);
			BoardPosition tempPos;

			//if player is controlling black pieces
			if (byPlayer == 1)
			{
				foreach (BoardPosition pos in positions)
				{
					tempPos = pos;
					BoardPosition bottomLeft = tempPos.Translate(-1, -1);
					tempPos = pos;
					BoardPosition bottomRight = tempPos.Translate(-1, 1);

					if (PositionInBounds(bottomLeft))
					{
						pawnAttackList.Add(bottomLeft);
					}
					if (PositionInBounds(bottomRight))
					{
						pawnAttackList.Add(bottomRight);
					}
					if (EnPassantPossible(pos))
					{
						pawnAttackList.Add(DoubledSteppedPawn);
					}
				}
			}

			//if player is controlling white pieces
			else
			{
				foreach (BoardPosition pos in positions)
				{
					tempPos = pos;
					BoardPosition topLeft = tempPos.Translate(1, -1);
					tempPos = pos;
					BoardPosition topRight = tempPos.Translate(1, 1);

					if (PositionInBounds(topLeft))
					{
						pawnAttackList.Add(topLeft);
					}
					if (PositionInBounds(topRight))
					{
						pawnAttackList.Add(topRight);
					}
					if (EnPassantPossible(pos))
					{
						pawnAttackList.Add(DoubledSteppedPawn);
					}
				}
			}

			return pawnAttackList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's rook.
		/// </summary>
		private ISet<BoardPosition> RookAttack(int byPlayer)
		{
			ISet<BoardPosition> rookAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)2, byPlayer);
			BoardPosition tempPos;

			foreach (BoardPosition pos in positions)
			{
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);

				while (PositionIsEmpty(middleLeft))
				{
					rookAttackList.Add(middleLeft);
					middleLeft = middleLeft.Translate(0, -1);
				}
				if (PositionInBounds(middleLeft))
				{
					rookAttackList.Add(middleLeft);
				}

				while (PositionIsEmpty(middleUp))
				{
					rookAttackList.Add(middleUp);
					middleUp = middleUp.Translate(1, 0);
				}
				if (PositionInBounds(middleUp))
				{
					rookAttackList.Add(middleUp);
				}

				while (PositionIsEmpty(middleDown))
				{
					rookAttackList.Add(middleDown);
					middleDown = middleDown.Translate(-1, 0);
				}
				if (PositionInBounds(middleDown))
				{
					rookAttackList.Add(middleDown);
				}

				while (PositionIsEmpty(middleRight))
				{
					rookAttackList.Add(middleRight);
					middleRight = middleRight.Translate(0, 1);
				}
				if (PositionInBounds(middleRight))
				{
					rookAttackList.Add(middleRight);
				}
			}
			return rookAttackList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's knight.
		/// </summary>
		private ISet<BoardPosition> KnightAttack(int byPlayer)
		{
			ISet<BoardPosition> knightAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)3, byPlayer);
			List<BoardPosition> attackPositions;
			BoardPosition tempPos;

			foreach (BoardPosition pos in positions)
			{
				attackPositions = new List<BoardPosition>();

				tempPos = pos;
				///naming convention of variables: twospaces[direction]onespace[direction]
				///ex: leftUp means twospaces[left]onespace[up]
				BoardPosition leftUp = tempPos.Translate(1, -2);
				tempPos = pos;
				BoardPosition upLeft = tempPos.Translate(2, -1);
				tempPos = pos;
				BoardPosition rightUp = tempPos.Translate(1, 2);
				tempPos = pos;
				BoardPosition upRight = tempPos.Translate(2, 1);
				tempPos = pos;
				BoardPosition leftDown = tempPos.Translate(-1, -2);
				tempPos = pos;
				BoardPosition downLeft = tempPos.Translate(-2, -1);
				tempPos = pos;
				BoardPosition rightDown = tempPos.Translate(-1, 2);
				tempPos = pos;
				BoardPosition downRight = tempPos.Translate(-2, 1);

				attackPositions.Add(leftUp);
				attackPositions.Add(upLeft);
				attackPositions.Add(rightUp);
				attackPositions.Add(upRight);
				attackPositions.Add(leftDown);
				attackPositions.Add(downLeft);
				attackPositions.Add(rightDown);
				attackPositions.Add(downRight);

				foreach (BoardPosition attackedPos in attackPositions)
				{
					if (PositionInBounds(attackedPos))
					{
						knightAttackList.Add(attackedPos);
					}
				}
			}
			return knightAttackList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's bishop.
		/// </summary>
		private ISet<BoardPosition> BishopAttack(int byPlayer)
		{
			ISet<BoardPosition> bishopAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)4, byPlayer);
			BoardPosition tempPos;

			foreach (BoardPosition pos in positions)
			{
				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				while (PositionIsEmpty(topLeft))
				{
					bishopAttackList.Add(topLeft);
					topLeft = topLeft.Translate(1, -1);
				}
				if (PositionInBounds(topLeft))
				{
					bishopAttackList.Add(topLeft);
				}

				while (PositionIsEmpty(bottomLeft))
				{
					bishopAttackList.Add(bottomLeft);
					bottomLeft = bottomLeft.Translate(-1, -1);
				}
				if (PositionInBounds(bottomLeft))
				{
					bishopAttackList.Add(bottomLeft);
				}

				while (PositionIsEmpty(topRight))
				{
					bishopAttackList.Add(topRight);
					topRight = topRight.Translate(1, 1);
				}
				if (PositionInBounds(topRight))
				{
					bishopAttackList.Add(topRight);
				}

				while (PositionIsEmpty(bottomRight))
				{
					bishopAttackList.Add(bottomRight);
					bottomRight = bottomRight.Translate(-1, 1);
				}
				if (PositionInBounds(bottomRight))
				{
					bishopAttackList.Add(bottomRight);
				}
			}
			return bishopAttackList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's queen.
		/// </summary>
		private ISet<BoardPosition> QueenAttack(int byPlayer)
		{
			ISet<BoardPosition> queenAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)5, byPlayer);
			BoardPosition tempPos;

			foreach (BoardPosition pos in positions)
			{

				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				while (PositionIsEmpty(topLeft))
				{
					queenAttackList.Add(topLeft);
					topLeft = topLeft.Translate(1, -1);
				}
				if (PositionInBounds(topLeft))
				{
					queenAttackList.Add(topLeft);
				}

				while (PositionIsEmpty(middleLeft))
				{
					queenAttackList.Add(middleLeft);
					middleLeft = middleLeft.Translate(0, -1);
				}
				if (PositionInBounds(middleLeft))
				{
					queenAttackList.Add(middleLeft);
				}

				while (PositionIsEmpty(bottomLeft))
				{
					queenAttackList.Add(bottomLeft);
					bottomLeft = bottomLeft.Translate(-1, -1);
				}
				if (PositionInBounds(bottomLeft))
				{
					queenAttackList.Add(bottomLeft);
				}

				while (PositionIsEmpty(middleUp))
				{
					queenAttackList.Add(middleUp);
					middleUp = middleUp.Translate(1, 0);
				}
				if (PositionInBounds(middleUp))
				{
					queenAttackList.Add(middleUp);
				}

				while (PositionIsEmpty(middleDown))
				{
					queenAttackList.Add(middleDown);
					middleDown = middleDown.Translate(-1, 0);
				}
				if (PositionInBounds(middleDown))
				{
					queenAttackList.Add(middleDown);
				}

				while (PositionIsEmpty(topRight))
				{
					queenAttackList.Add(topRight);
					topRight = topRight.Translate(1, 1);
				}
				if (PositionInBounds(topRight))
				{
					queenAttackList.Add(topRight);
				}

				while (PositionIsEmpty(middleRight))
				{
					queenAttackList.Add(middleRight);
					middleRight = middleRight.Translate(0, 1);
				}
				if (PositionInBounds(middleRight))
				{
					queenAttackList.Add(middleRight);
				}

				while (PositionIsEmpty(bottomRight))
				{
					queenAttackList.Add(bottomRight);
					bottomRight = bottomRight.Translate(-1, 1);
				}
				if (PositionInBounds(bottomRight))
				{
					queenAttackList.Add(bottomRight);
				}
			}
			return queenAttackList;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by a player's king.
		/// </summary>
		private ISet<BoardPosition> KingAttack(int byPlayer)
		{
			ISet<BoardPosition> kingAttackList = new HashSet<BoardPosition>();
			IEnumerable<BoardPosition> positions = GetPositionsOfPiece((ChessPieceType)6, byPlayer);
			List<BoardPosition> attackPositions;
			BoardPosition tempPos;

			foreach (BoardPosition pos in positions)
			{
				attackPositions = new List<BoardPosition>();

				tempPos = pos;
				BoardPosition topLeft = tempPos.Translate(1, -1);
				tempPos = pos;
				BoardPosition middleLeft = tempPos.Translate(0, -1);
				tempPos = pos;
				BoardPosition bottomLeft = tempPos.Translate(-1, -1);
				tempPos = pos;
				BoardPosition middleUp = tempPos.Translate(1, 0);
				tempPos = pos;
				BoardPosition middleDown = tempPos.Translate(-1, 0);
				tempPos = pos;
				BoardPosition topRight = tempPos.Translate(1, 1);
				tempPos = pos;
				BoardPosition middleRight = tempPos.Translate(0, 1);
				tempPos = pos;
				BoardPosition bottomRight = tempPos.Translate(-1, 1);

				attackPositions.Add(topLeft);
				attackPositions.Add(middleLeft);
				attackPositions.Add(bottomLeft);
				attackPositions.Add(middleUp);
				attackPositions.Add(middleDown);
				attackPositions.Add(topRight);
				attackPositions.Add(middleRight);
				attackPositions.Add(bottomRight);

				foreach (BoardPosition attackedPos in attackPositions)
				{
					if (PositionInBounds(attackedPos))
					{
						kingAttackList.Add(attackedPos);
					}
				}
			}
			return kingAttackList;
		}

		#endregion

		#region Explicit IGameBoard implementations.
		IEnumerable<IGameMove> IGameBoard.GetPossibleMoves()
		{
			return GetPossibleMoves();
		}
		void IGameBoard.ApplyMove(IGameMove m)
		{
			ApplyMove(m as ChessMove);
		}
		IReadOnlyList<IGameMove> IGameBoard.MoveHistory => mMoveHistory;
		#endregion

		// You may or may not need to add code to this constructor.
		public ChessBoard()
		{

			// create fresh board
			mBoard = new byte[32];

			// set first player to 1 
			mCurrentPlayer = 1;

			// initialize list of captured moves and number of moves made
			CapturedPieces = new List<ChessPiece>();
			NumMovesNoPawnOrCapture = 0;

			WhitePawnStartingPositions = new List<BoardPosition>();
			BlackPawnStartingPositions = new List<BoardPosition>();

			// initialized booleans for castling check (rook/king moved)
			BlackA8RookMoved = false;
			BlackH8RookMoved = false;
			BlackKingMoved = false;
			WhiteA1RookMoved = false;
			WhiteH1RookMoved = false;
			WhiteKingMoved = false;

			// this for en passant
			LastMoveDoubleStep = false;

			// black pieces on bottom  (0xxx), white on top (1xxx)
			ChessPieceType[] initial = {
				ChessPieceType.Rook,
				ChessPieceType.Knight,
				ChessPieceType.Bishop,
				ChessPieceType.Queen,
				ChessPieceType.King,
				ChessPieceType.Bishop,
				ChessPieceType.Knight,
				ChessPieceType.Rook
			};

			BoardPosition black, bPawn, white, wPawn;
			ChessPiece piece;
			// 8 columns to set (BoardSize)
			for (int i = 0; i < BoardSize; i++)
			{
				// set white pieces
				white = new BoardPosition(BoardSize - 1, i);
				piece = new ChessPiece(initial[i], 1);
				SetPieceAtPosition(white, piece);
				// white pawns
				wPawn = new BoardPosition(BoardSize - 2, i);
				piece = new ChessPiece(ChessPieceType.Pawn, 1);
				SetPieceAtPosition(wPawn, piece);
				WhitePawnStartingPositions.Add(wPawn);

				// set black pieces
				black = new BoardPosition(0, i);
				piece = new ChessPiece(initial[i], 2);
				SetPieceAtPosition(black, piece);
				// black pawns
				bPawn = new BoardPosition(1, i);
				piece = new ChessPiece(ChessPieceType.Pawn, 2);
				SetPieceAtPosition(bPawn, piece);
				BlackPawnStartingPositions.Add(bPawn);
			}
		}

		public ChessBoard(IEnumerable<Tuple<BoardPosition, ChessPiece>> startingPositions)
			: this()
		{
			var king1 = startingPositions.Where(t => t.Item2.Player == 1 && t.Item2.PieceType == ChessPieceType.King);
			var king2 = startingPositions.Where(t => t.Item2.Player == 2 && t.Item2.PieceType == ChessPieceType.King);
			if (king1.Count() != 1 || king2.Count() != 1)
			{
				throw new ArgumentException("A chess board must have a single king for each player");
			}

			foreach (var position in BoardPosition.GetRectangularPositions(8, 8))
			{
				SetPieceAtPosition(position, ChessPiece.Empty);
			}

			int player;
			ChessPieceType type;
			int[] values = { 0, 0 };
			foreach (var pos in startingPositions)
			{
				SetPieceAtPosition(pos.Item1, pos.Item2);
				// calculate the overall advantage for this board, in terms of the pieces
				// that the board has started with. "pos.Item2" will give you the chess piece being placed
				// on this particular position.
				type = pos.Item2.PieceType;
				player = pos.Item2.Player;
				UpdateAdvantage(type, player);

			}

			// initialized booleans for castling check (rook/king moved)
			WhiteA1RookMoved = (GetPieceAtPosition(new BoardPosition(7, 0)).PieceType == ChessPieceType.Rook) ? false : true;
			WhiteH1RookMoved = (GetPieceAtPosition(new BoardPosition(7, 7)).PieceType == ChessPieceType.Rook) ? false : true;
			BlackA8RookMoved = (GetPieceAtPosition(new BoardPosition(0, 0)).PieceType == ChessPieceType.Rook) ? false : true;
			BlackH8RookMoved = (GetPieceAtPosition(new BoardPosition(0, 7)).PieceType == ChessPieceType.Rook) ? false : true;
			WhiteKingMoved = (GetPieceAtPosition(new BoardPosition(7, 4)).PieceType == ChessPieceType.King) ? false : true;
			BlackKingMoved = (GetPieceAtPosition(new BoardPosition(0, 4)).PieceType == ChessPieceType.King) ? false : true;
		}

		public string BoardToString()
		{
			char[] LABELS = { '.', 'P', 'R', 'N', 'B', 'Q', 'K' };
			StringBuilder str = new StringBuilder();

			for (int i = 0; i < ChessBoard.BoardSize; i++)
			{
				str.Append(8 - i);
				str.Append(" ");
				for (int j = 0; j < ChessBoard.BoardSize; j++)
				{
					var space = GetPieceAtPosition(new BoardPosition(i, j));
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

	}
}
