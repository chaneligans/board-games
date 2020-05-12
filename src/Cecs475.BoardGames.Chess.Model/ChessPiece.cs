using System;
using System.Collections.Generic;
using System.Text;

namespace Cecs475.BoardGames.Chess.Model {
	/// <summary>
	/// Represents a chess piece owned by a particular player.
	/// </summary>
	public struct ChessPiece {
		public ChessPieceType PieceType { get; }
		public sbyte Player { get; }

		public ChessPiece(ChessPieceType pieceType, int player) {
			PieceType = pieceType;
			Player = (sbyte)player;
		}

		public static ChessPiece Empty{ get; } = new ChessPiece(ChessPieceType.Empty, 0);

		public bool Equals(ChessPiece other) {
			if (PieceType == other.PieceType && Player == other.Player){
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			string r = "";

			if (Player == 1)
			{
				switch (this.PieceType)
				{
					case ChessPieceType.Empty:
						r = "empty";
						break;
					case ChessPieceType.Pawn:
						r = "white_pawn";
						break;
					case ChessPieceType.Rook:
						r = "white_rook";
						break;
					case ChessPieceType.Knight:
						r = "white_knight";
						break;
					case ChessPieceType.Bishop:
						r = "white_bishop";
						break;
					case ChessPieceType.Queen:
						r = "white_queen";
						break;
					case ChessPieceType.King:
						r = "white_king";
						break;
				}
			}

			else
			{
				switch (this.PieceType)
				{
					case ChessPieceType.Empty:
						r = "empty";
						break;
					case ChessPieceType.Pawn:
						r = "black_pawn";
						break;
					case ChessPieceType.Rook:
						r = "black_rook";
						break;
					case ChessPieceType.Knight:
						r = "black_knight";
						break;
					case ChessPieceType.Bishop:
						r = "black_bishop";
						break;
					case ChessPieceType.Queen:
						r = "black_queen";
						break;
					case ChessPieceType.King:
						r = "black_king";
						break;
				}
			}

			return r;

		}

	}
}
