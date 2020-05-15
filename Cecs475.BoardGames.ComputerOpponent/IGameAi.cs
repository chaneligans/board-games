using Cecs475.BoardGames.Model;
using System;

namespace Cecs475.BoardGames.ComputerOpponent {
	public interface IGameAi {
		IGameMove FindBestMove(IGameBoard b);
	}
}
