using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tower_Defense_Game.GameLogic;
using Tower_Defense_Game.GameState;

namespace Tower_Defense_Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Baut das UI aus der XAML Datei auf

            // Setzt das GameStateModel als "Datenquelle" für die UI
            // Ab jetzt kann die UI (XAML) auf Leben, Gold usw. zugreifen.
            DataContext = new GameStateModel();
        }

        // Wenn der Button geklickt wird wird ContinueButton() ausgelöst
        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as GameStateModel)?.ContinueButton();
        }
    }
}