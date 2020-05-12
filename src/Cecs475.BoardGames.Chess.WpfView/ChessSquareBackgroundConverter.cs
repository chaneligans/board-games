using Cecs475.BoardGames.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Cecs475.BoardGames.Chess.WpfView {
	public class ChessSquareBackgroundConverter : IMultiValueConverter {
		private static SolidColorBrush HIGHLIGHT_BRUSH = Brushes.LightGreen;
		private static SolidColorBrush DEFAULT_BRUSH_LIGHT = Brushes.LightBlue;
		private static SolidColorBrush DEFAULT_BRUSH_DARK = Brushes.AliceBlue;
		private static SolidColorBrush CHECK_BRUSH = Brushes.Yellow;
		private static SolidColorBrush SELECTED_BRUSH = Brushes.Red;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			// This converter will receive two properties: the Position of the square, and whether it
			// is being hovered.
			BoardPosition pos = (BoardPosition) values[0];
			bool isHovered = (bool) values[1];
			bool isSelected = (bool) values[2]; // if the user has selected (clicked) the square
			bool isCheck = (bool) values[3];
			//bool isEndPosition = (bool) values[3]; // if the current square is an end position of a move starting at the selected piece
			//bool isStartPosition = (bool) values[4]; // if no piece is selected and the hovering square that is a start pos of a poss move

			// red for clicked/selected square
			if (isSelected) {
				// light green for possible move of the clicked/selected square
				//if (isEndPosition) {
				//	return POSSIBLE_MOVE_BRUSH;
				//}				
				return SELECTED_BRUSH;
			}
			//else if (isStartPosition) {
			//	// light green
			//	return POSSIBLE_MOVE_BRUSH;
			//}

			// Hovered squares have a specific color.
			if (isHovered) {
				return HIGHLIGHT_BRUSH;
			}

			if (isCheck) {
				return CHECK_BRUSH;
			}

			// Change colors for every other square.
			if (((pos.Row % 2 == 0) && (pos.Col % 2 == 1)) || ((pos.Row % 2 == 1) && (pos.Col % 2 == 0))) {
				return DEFAULT_BRUSH_DARK;
			}

			// Inner squares are drawn light blue.
			return DEFAULT_BRUSH_LIGHT;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
