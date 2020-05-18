using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cecs475.BoardGames.WpfApp {
  public class Game {
      public string Name { get; set; }
      public List<Info> Files { get; set; }
  }
}
