using Cecs475.BoardGames.Chess.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Cecs475.BoardGames.Chess.WpfView {
  public class PieceImageConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			try {
				ChessPiece c = (ChessPiece)value; // help
				string src = c.ToString();
				if (src != "empty"){
					return new BitmapImage(new Uri("/Cecs475.BoardGames.Chess.WpfView;component/Resources/" + src + ".png", UriKind.Relative));
				}
				return null;

			}
			catch (Exception e) {
				return null;
			}
		}

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotImplementedException();
    }
  }
}
