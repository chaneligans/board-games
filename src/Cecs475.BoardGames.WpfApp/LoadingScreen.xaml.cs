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

using Newtonsoft.Json;
using RestSharp;

namespace Cecs475.BoardGames.WpfApp {
  /// <summary>
  /// Interaction logic for LoadingScreen.xaml
  /// </summary>
  public partial class LoadingScreen : Window {
    public LoadingScreen() {
      InitializeComponent();
    }

    public async void OnLoad(object sender, RoutedEventArgs e) {
      var client = new RestClient("https://cecs475-boardamges.herokuapp.com/api/games");
      var request = new RestRequest(Method.GET);
      var response = await client.ExecuteAsync(request);

      // idk what to do here
      var deserialized = JsonConvert.DeserializeObject(response.Content);
      deserialized.ToString();
      // up to here

      GameChoiceWindow gameChoiceWindow = new GameChoiceWindow();
      gameChoiceWindow.Show();
      this.Close();
    }
  }
}
