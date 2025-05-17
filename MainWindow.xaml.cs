using Microsoft.VisualBasic.FileIO;
using Slippi.NET;
using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;
using System.IO;
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


namespace Slippi_Stats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private FileSystemWatcher watcher;
        public static Image cimg = new Image();
        public static TextBlock title;

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.Manual;
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;
            Left = screenWidth - Width;
            Top = 0;
            this.Opacity = 0.75;
            string path = SpecialDirectories.MyDocuments + "\\Slippi";

            CreateUI(this);

            InitializeFileWatcher(@path);
        }

        private void CreateUI(MainWindow mw)
        {
            // Character Images
            {
                cimg.Opacity = 0.75;
                cimg.HorizontalAlignment = HorizontalAlignment.Right;
                cimg.VerticalAlignment = VerticalAlignment.Top;
                cimg.Margin = new Thickness(10);
                cimg.Width = 100;
                cimg.Height = Width;
                OverlayGrid.Children.Add(cimg);
            }
            //Title
            {
                title.Opacity = 0.50;
                title.VerticalAlignment = VerticalAlignment.Top;
                title.Margin = new Thickness(10);
            }

        }
        private void InitializeFileWatcher(string path)
        {
            watcher = new FileSystemWatcher(path)
            {
                Filter = "*.slp",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            watcher.Created += async (sender, e) =>
            {
                await Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            using (FileStream stream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                await Dispatcher.InvokeAsync(() => LoadGameAsync(e.FullPath));
                                break;
                            }
                        }
                        catch (IOException)
                        {
                            await Task.Delay(200);
                        }
                    }
                });
            };
        }

        private async Task LoadGameAsync(string filePath)
        {
            SlippiGame game = new SlippiGame(filePath);
            int totalFrames = game.GetFrames().Count();
            int totalSeconds = totalFrames / 60;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string time = $"{minutes:D2}:{seconds:D2}";

            Metadata gameMetadata = game.GetMetadata();
            //gameMetadata.Players.TryGetValue(0, out PlayerMetadata p1);
            //gameMetadata.Players.TryGetValue(1, out PlayerMetadata p2);
            StatsInfo stats = game.GetStats();
            GameStart start = game.GetSettings();

            Player player1 = new Player(stats, 0, start);
            Player player2 = new Player(stats, 1, start);
        }


        public class Player
        {
            string name;
            string code;
            string character;
            int? characterid;
            GameStart start;
            float? avgcombodmg = 0;
            float? maxcombodmg = 0;
            float? mincombodmg = float.MaxValue;
            StatsInfo stats;
            List<Conversion> conversions = new List<Conversion>();
            List<ComboInfo> combos = new List<ComboInfo>();
            string curdir = Directory.GetCurrentDirectory() + "\\characters\\";

            public Player(StatsInfo pstats, int index, GameStart gstart)
            {
                //get playerdata
                name = gstart.Players[index].DisplayName;
                code = gstart.Players[index].ConnectCode;
                characterid = gstart.Players[index].CharacterId;
                Character characterEnum = (Character)characterid;
                character = characterEnum.ToString();
                character = char.ToUpper(character[0]) + character.Substring(1).ToLower();
                cimg.Source = new BitmapImage(new Uri(@curdir + character + ".png"));

                if (character.Contains('_'))
                {
                    character = character.Replace('_', ' ');
                    character = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(character);
                }
                stats = pstats;
                start = gstart;

                foreach (Conversion x in stats.Conversions)
                {
                    if (x.PlayerIndex == index)
                    {
                        conversions.Add(x);
                    }
                }

                foreach (ComboInfo x in stats.Combos)
                {
                    if (x.PlayerIndex == index)
                    {
                        combos.Add(x);
                    }
                }

                foreach (ComboInfo x in combos)
                {
                    float? combodmg = (x.EndPercent - x.StartPercent);
                    if (maxcombodmg < combodmg)
                    {
                        maxcombodmg = combodmg;
                    }
                    if (mincombodmg > combodmg)
                    {
                        mincombodmg = combodmg;
                    }
                    avgcombodmg += combodmg;
                }
                avgcombodmg = avgcombodmg / combos.Count;
            }
        }
    }

}