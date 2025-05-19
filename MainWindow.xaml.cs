using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Slippi.NET;
using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace Slippi_Stats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private FileSystemWatcher watcher;
        public static Image cimg = new Image();
        public static TextBlock title = new TextBlock();
        public static TextBlock combodata = new TextBlock();
        public static TextBlock timeplayed = new TextBlock();
        public static TextBlock gamefinished = new TextBlock();
        public static TextBlock conversiondata = new TextBlock();

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
            LoadNewestSLP(path);
            OpenWithSlippi();
        }

        private void OpenWithSlippi()
        {

        }

        private async void LoadNewestSLP(string path)
        {
            try
            {
                var newestFile = Directory
                    .EnumerateFiles(path, "*.slp", System.IO.SearchOption.AllDirectories)
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (newestFile != null)
                {
                    await LoadGameAsync(newestFile.FullName);
                }
            }
            catch
            {
                MessageBox.Show("Couldn't find an .SLP file to load");
            }
        }

        private void CreateUI(MainWindow mw)
        {
            cimg.Opacity = 0.60;
            cimg.HorizontalAlignment = HorizontalAlignment.Right;
            cimg.VerticalAlignment = VerticalAlignment.Top;
            cimg.Width = 90;
            cimg.Height = Width;
            cimg.Margin = new Thickness(0, 5, 20, 0);
            OverlayGrid.Children.Add(cimg);

            // Title
            title.Opacity = 0.80;
            title.Height = 60;
            title.Margin = new Thickness(0, 0, 0, 0);
            OverlayStackPanel.Children.Add(title);

            // Game finished
            gamefinished.Opacity = 0.8;
            gamefinished.FontSize = 16;
            gamefinished.Height = 20;
            gamefinished.Margin = new Thickness(0, 10, 0, 0);
            OverlayStackPanel.Children.Add(gamefinished);

            // Time played
            timeplayed.Opacity = 0.8;
            timeplayed.FontSize = 16;
            timeplayed.Height = 50;
            timeplayed.Margin = new Thickness(0, 0, 0, 0);
            OverlayStackPanel.Children.Add(timeplayed);

            // Combo data
            combodata.Opacity = 0.8;
            combodata.Margin = new Thickness(0, 5, 0, 0);
            OverlayStackPanel.Children.Add(combodata);

            // Conversion data
            conversiondata.Opacity = 0.8;
            conversiondata.Margin = new Thickness(0, 5, 0, 0);
            OverlayStackPanel.Children.Add(conversiondata);
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
                await System.Threading.Tasks.Task.Run(async () =>
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
            StatsInfo? stats = game.GetStats();
            GameStart? start = game.GetSettings();
            int index = GetPlayerIndex(start);
            int oppindex;
            if (index == 0)
            {
                oppindex = 1;
            }
            else
            {
                oppindex = 0;
            }

            gameMetadata.Players.TryGetValue(index, out PlayerMetadata mdata);
            gameMetadata.Players.TryGetValue(oppindex, out PlayerMetadata oppmdata);
            timeplayed.Text = $"Game lasted {time}" + "\nOpponent: " + start.Players[oppindex].DisplayName.ToString() +" " + oppmdata.Names.Code.ToString();


            Player player = new Player(stats, mdata, index, start);
        }

        private int GetPlayerIndex(GameStart? start)
        {
            string p1id = start.Players[0].ConnectCode;
            string p2id = start.Players[1].ConnectCode;

            string jsonpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Slippi Launcher\\netplay\\User\\Slippi\\user.json";
            string json = File.ReadAllText(jsonpath);
            userdata userdata = JsonConvert.DeserializeObject<userdata>(json);

            int index;
            if (userdata.uid == p1id)
            {
                index = 0;
            }
            else
            {
                index = 1;
            }

            return index;
        }

        public class userdata
        {
            public string uid { get; set; }
            public string connectCode{ get; set; }
            public string playKey { get; set; }
            public string displayName { get; set; }
            public string latestVersion { get; set; }
        }

        public class Player
        {
            string? name;
            string? code;
            string? character;
            string curdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JimboINC\\characters\\";
            float? avgcombodmg = 0;
            float? maxcombodmg = 0;
            float? mincombodmg = float.MaxValue;
            float? combokills = 0;
            float? avgconversiondmg = 0;
            float? maxconversiondmg = 0;
            float? minconversiondmg = float.MaxValue;
            float? conversionkills = 0;
            int? characterid;
            bool isFinished = false;
            PlayerMetadata mdata;
            GameStart start;
            StatsInfo stats;
            List<Conversion> conversions = new List<Conversion>();
            List<ComboInfo> combos = new List<ComboInfo>();
            OverallStats ostats;

            public Player(StatsInfo stats, PlayerMetadata mdata, int index, GameStart start)
            {
                //get playerdata
                this.start = start;
                this.stats = stats;
                this.mdata = mdata;
                name = this.start.Players[index].DisplayName;
                code = this.mdata.Names.Code;
                characterid = start.Players[index].CharacterId;
                Character characterEnum = (Character)characterid;
                character = characterEnum.ToString();
                character = char.ToUpper(character[0]) + character.Substring(1).ToLower();
                isFinished = stats.GameComplete;

                if (character.Contains('_'))
                {
                    character = character.Replace('_', ' ');
                    character = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(character);
                }
                

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

                foreach (OverallStats x in stats.Overall)
                {
                    if (x.PlayerIndex == index)
                    {
                        ostats = x;
                    }
                }

                //combo data
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
                    if(x.DidKill)
                    {
                        combokills++;
                    }
                }
                avgcombodmg = avgcombodmg / combos.Count;

                //conversion data
                foreach (Conversion x in conversions)
                {
                    float? conversiondmg = (x.EndPercent - x.StartPercent);
                    if (maxconversiondmg < conversiondmg)
                    {
                        maxconversiondmg = conversiondmg;
                    }
                    if (minconversiondmg > conversiondmg)
                    {
                        minconversiondmg = conversiondmg;
                    }
                    avgconversiondmg += conversiondmg;
                    if (x.DidKill)
                    {
                        conversionkills++;
                    }
                }

                avgconversiondmg = avgconversiondmg / conversions.Count;

                UpdateUI();
            }

            private void UpdateUI()
            {
                cimg.Source = new BitmapImage(new Uri(@curdir + character + ".png"));
                title.Text = "";
                combodata.Text = "";
                gamefinished.Text = "";
                conversiondata.Text = "";

                title.Inlines.Add(new Run("Hi, " + name + "\n")
                {
                    FontSize = 24
                });
                title.Inlines.Add(new Run(code)
                {
                    FontSize = 16,
                    Foreground = Brushes.Gray
                });

                combodata.Inlines.Add(new Run("Avg Combo Damage: " + (avgcombodmg ?? 0).ToString("F1") + "%\n")
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                });

                combodata.Inlines.Add(new Run(
                    "Max Combo Damage: " + (maxcombodmg ?? 0).ToString("F1") + "%\n" +
                    "Min Combo Damage: " + (mincombodmg ?? 0).ToString("F1") + "%\n" +
                    (ostats.InputsPerMinute.Ratio ?? 0).ToString("F1") + " Inputs/Minute\n" +
                    (ostats.DigitalInputsPerMinute.Ratio ?? 0).ToString("F1") + " Digital Inputs/Minute\n"+
                    ostats.KillCount.ToString() + " Kills\n")
                {
                    FontSize = 16,
                });

                conversiondata.Inlines.Add(new Run("Avg Conversion Damage: " + (avgconversiondmg ?? 0).ToString("F1") + "%\n")
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                });

                conversiondata.Inlines.Add(new Run(
                    "Max Conversion Damage: " + (maxconversiondmg ?? 0).ToString("F1") + "%\n" +
                    "Min Conversion Damage: " + (minconversiondmg ?? 0).ToString("F1") + "%\n" +
                    ostats.SuccessfulConversions.Total.ToString("F1") + " Successful Conversions\n" +
                    (ostats.OpeningsPerKill.Ratio ?? 0).ToString("F1") + " Openings/Kill    \n" +
                    (ostats.DamagePerOpening.Ratio ?? 0).ToString("F1") + "% Damage/Opening")
                {
                    FontSize = 16,
                });

                if (isFinished)
                {
                    gamefinished.Text = "Game Complete";
                }
                else
                {
                    gamefinished.Text = "Game Incomplete";
                }

            }
        }
    }

}