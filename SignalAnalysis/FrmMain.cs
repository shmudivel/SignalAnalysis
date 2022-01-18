using System.Text;
using FftSharp;

namespace SignalAnalysis;

public partial class FrmMain : Form
{
    //private string strDefaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    //private string strUserSavePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    //private string strDefaultOpenPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\examples";
    //private string strUserOpenPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\examples";
    //private bool RememberFileDialogPath = true;
    //private string strNumericFormat = "G4";
    //private string strDataFormat = "#0.0##";
    //private string strMillisecondsFormat = "1.fff";
    //private System.Globalization.CultureInfo appCulture = System.Globalization.CultureInfo.CurrentCulture;

    private double[][] _signalData = Array.Empty<double[]>();
    private double[] _signalFFT = Array.Empty<double>();
    private string[] seriesLabels = Array.Empty<string>();
    private int nSeries = 0;
    double nSampleFreq = 0.0;
    
    DateTime nStart;
    ClassSettings _settings = new();
    Stats Results;
    ToolStripComboBox stripComboSeries;
    ToolStripComboBox stripComboWindows;

    Task statsTask;
    private CancellationTokenSource tokenSource;
    private CancellationToken token;

    /// <summary>
    /// https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/y99d1cd3(v=vs.100)?WT.mc_id=DT-MVP-5003235
    /// https://stackoverflow.com/questions/32989100/how-to-make-multi-language-app-in-winforms
    /// </summary>
    private readonly System.Resources.ResourceManager StringsRM = new("SignalAnalysis.localization.strings", typeof(FrmMain).Assembly);

    public FrmMain()
    {
        // Load settings
        LoadProgramSettingsJSON();

        InitializeToolStripPanel();
        InitializeStatusStrip();
        InitializeMenu();
        InitializeComponent();
        
        this.plotOriginal.SnapToPoint = true;
        this.plotFFT.SnapToPoint = true;

        PopulateComboWindow();

        UpdateUI_Language();
    }

    // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstrippanel?view=windowsdesktop-6.0
    // https://stackoverflow.com/questions/40382105/how-to-add-two-toolstripcombobox-and-separator-horizontally-to-one-toolstripdrop
    private void InitializeToolStripPanel()
    {
        String path = Path.GetDirectoryName(Environment.ProcessPath);
        Font toolFont = new ("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

        stripComboSeries = new()
        {
            AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.None,
            DropDownHeight = 110,
            DropDownWidth = 122,
            FlatStyle = System.Windows.Forms.FlatStyle.Standard,
            Font = toolFont,
            IntegralHeight = true,
            MaxDropDownItems = 9,
            MergeAction = System.Windows.Forms.MergeAction.MatchOnly,
            Name = "cboSeries",
            Size = new System.Drawing.Size(120, 25),
            Sorted = false,
            ToolTipText = "Select data series"
        };
        stripComboWindows = new()
        {
            AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.None,
            DropDownHeight = 110,
            DropDownWidth = 122,
            FlatStyle = System.Windows.Forms.FlatStyle.Standard,
            Font = toolFont,
            IntegralHeight = true,
            MaxDropDownItems = 9,
            MergeAction = System.Windows.Forms.MergeAction.MatchOnly,
            Name = "cboWindows",
            Size = new System.Drawing.Size(120, 25),
            Sorted = false,
            ToolTipText = "Select FFT window"
        };
        stripComboSeries.SelectedIndexChanged += ComboSeries_SelectedIndexChanged;
        stripComboWindows.SelectedIndexChanged += ComboWindow_SelectedIndexChanged;

        ToolStripPanel tspTop = new();
        tspTop.Dock = DockStyle.Top;
        tspTop.Name = "StripPanelTop";

        ToolStrip toolStripMain = new()
        {
            Font = toolFont,
            ImageScalingSize = new System.Drawing.Size(48, 48),
            Location = new System.Drawing.Point(0, 0),
            Renderer = new customRenderer<ToolStripButton>(System.Drawing.Brushes.SteelBlue, System.Drawing.Brushes.LightSkyBlue),
            RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional,
            Text = "Main toolbar"
        };

        ToolStripItem toolStripItem;
        toolStripItem = toolStripMain.Items.Add("Exit", new System.Drawing.Icon(path + @"\images\exit.ico", 48, 48).ToBitmap(), new EventHandler(Exit_Click));
        toolStripItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
        toolStripItem.Name = "Exit";
        toolStripItem = toolStripMain.Items.Add("Open", new System.Drawing.Icon(path + @"\images\openfolder.ico", 48, 48).ToBitmap(), new EventHandler(Open_Click));
        toolStripItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
        toolStripItem.Name = "Open";
        toolStripItem = toolStripMain.Items.Add("Export", new System.Drawing.Icon(path + @"\images\save.ico", 48, 48).ToBitmap(), new EventHandler(Export_Click));
        toolStripItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
        toolStripItem.Name = "Export";
        toolStripMain.Items.Add(new ToolStripSeparator());
        toolStripMain.Items.Add(stripComboSeries);
        toolStripMain.Items.Add(stripComboWindows);
        toolStripMain.Items.Add(new ToolStripSeparator());
        toolStripItem = toolStripMain.Items.Add("Settings", new System.Drawing.Icon(path + @"\images\settings.ico", 48, 48).ToBitmap(), new EventHandler(Settings_Click));
        toolStripItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
        toolStripItem.Name = "Settings";
        toolStripMain.Items.Add(new ToolStripSeparator());
        toolStripItem = toolStripMain.Items.Add("About", new System.Drawing.Icon(path + @"\images\about.ico", 48, 48).ToBitmap(), new EventHandler(About_Click));
        toolStripItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
        toolStripItem.Name = "About";

        tspTop.Join(toolStripMain);
        this.Controls.Add(tspTop);
        
        return;
    }

    private void InitializeStatusStrip()
    {
        ToolStripPanel tspBottom = new();
        tspBottom.Dock = DockStyle.Bottom;
        tspBottom.Name = "StripPanelBottom";

        StatusStrip statusStrip = new()
        {
            Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point),
            ImageScalingSize = new System.Drawing.Size(16, 16),
            Name = "StatusStrip",
            ShowItemToolTips = true,
            Renderer = new customRenderer<ToolStripStatusLabelEx>(Brushes.SteelBlue, Brushes.LightSkyBlue),
            RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional,
            Size = new System.Drawing.Size(934, 28),
            TabIndex = 1,
            Text = "Status bar"
        };

        int item;
        item = statusStrip.Items.Add(new ToolStripStatusLabel()
        {
            AutoSize = true,
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Name = "LabelEmpty",
            Spring = true,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            ToolTipText = ""
        });

        item = statusStrip.Items.Add(new ToolStripStatusLabelEx()
        {
            AutoSize = false,
            Checked = false,    // Inverse because we are simulating a click
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Name = "LabelExPower",
            Size = new System.Drawing.Size(28, 23),
            Text = "P",
            ToolTipText = "Power spectra (dB)"
        });
        statusStrip.Items[item].Click += LabelEx_Click;
        LabelEx_Click(statusStrip.Items[item], EventArgs.Empty);

        item = statusStrip.Items.Add(new ToolStripStatusLabelEx()
        {
            AutoSize = false,
            Checked = true,    // Inverse because we are simulating a click
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Name = "LabelExCumulative",
            Size = new System.Drawing.Size(28, 23),
            Text = "F",
            ToolTipText = "Cumulative fractal dimension"
        });
        statusStrip.Items[item].Click += LabelEx_Click;
        LabelEx_Click(statusStrip.Items[item], EventArgs.Empty);

        item = statusStrip.Items.Add(new ToolStripStatusLabelEx()
        {
            AutoSize = false,
            Checked = true,    // Inverse because we are simulating a click
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Name = "LabelExEntropy",
            Size = new System.Drawing.Size(28, 23),
            Text = "E",
            ToolTipText = "Approximate and sample entropy"
        });
        statusStrip.Items[item].Click += LabelEx_Click;
        LabelEx_Click(statusStrip.Items[item], EventArgs.Empty);

        item = statusStrip.Items.Add(new ToolStripStatusLabelEx()
        {
            AutoSize = false,
            Checked = true,    // Inverse because we are simulating a click
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Name = "LabelExCrossHair",
            Size = new System.Drawing.Size(28, 23),
            Text = "C",
            ToolTipText = "Plot's crosshair mode"
        });
        statusStrip.Items[item].Click += LabelEx_Click;
        LabelEx_Click(statusStrip.Items[item], EventArgs.Empty);

        tspBottom.Join(statusStrip);
        this.Controls.Add(tspBottom);
    }

    private void InitializeMenu()
    {

    }

    private void toolStripMain_Exit_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        using (new CenterWinDialog(this))
        {
            if (DialogResult.No == MessageBox.Show(this,
                                                    "Are you sure you want to exit\nthe application?",
                                                    "Exit?",
                                                    MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Question,
                                                    MessageBoxDefaultButton.Button2))
            {
                // Cancel
                e.Cancel = true;
            }
        }

        // Save settings data
        SaveProgramSettingsJSON();
    }

    

    private void PopulateComboSeries(params string[] values)
    {
        stripComboSeries.Items.Clear();
        if (values.Length != 0)
            stripComboSeries.Items.AddRange(values);
        else
            stripComboSeries.Items.AddRange(seriesLabels);
        stripComboSeries.SelectedIndex = 0;
    }
    private void PopulateComboWindow()
    {
        IWindow[] windows = Window.GetWindows();
        stripComboWindows.Items.AddRange(windows);
        stripComboWindows.SelectedIndex = windows.ToList().FindIndex(x => x.Name == "Hanning");
    }

    private void ComboSeries_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_signalData.Length == 0) return;

        //int nIndex = stripComboSeries.SelectedIndex;
        //nPoints = _settings.IndexEnd - _settings.IndexStart + 1;
        //_signalRange = new double[nPoints];

        //Array.Copy(
        //    _signalData[nIndex],
        //    _settings.IndexStart,
        //    _signalRange,
        //    0,
        //    nPoints);
        //var test = _signalData[nIndex][_settings.IndexStart..(_settings.IndexEnd + 1)];
        ComputeStats();   
    }

    private void ComboWindow_SelectedIndexChanged(object sender, EventArgs e)
    {
        IWindow window = (IWindow)stripComboWindows.SelectedItem;
        if (window is null)
        {
            //richTextBox1.Clear();
            return;
        }
        else
        {
            //richTextBox1.Text = window.Description;
        }

        if (stripComboSeries.SelectedIndex < 0) return;

        // Extract the values 
        var signal = _signalData[stripComboSeries.SelectedIndex][_settings.IndexStart..(_settings.IndexEnd + 1)];
        if (signal is null || signal.Length == 0) return;

        // Adjust to the lowest power of 2
        int power2 = (int)Math.Log2(signal.Length);
        //int evenPower = (power2 % 2 == 0) ? power2 : power2 - 1;
        _signalFFT = new double[(int)Math.Pow(2, power2)];
        Array.Copy(_signalData[stripComboSeries.SelectedIndex], _signalFFT, _signalFFT.Length);

        // apply window
        double[] signalWindow = new double[_signalFFT.Length];
        Array.Copy(_signalFFT, signalWindow, _signalFFT.Length);
        window.ApplyInPlace(signalWindow);

        UpdateKernel(window, signal.Length);
        UpdateWindowed(signalWindow);
        UpdateFFT(signalWindow);
    }

    private void FrmMain_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Escape && statsTask.Status == TaskStatus.Running)
            tokenSource.Cancel();   
    }

    private async Task ComputeStats()
    {
        tokenSource = new();
        token = tokenSource.Token;
        Results = new();

        // Extract the values 
        var signal = _signalData[stripComboSeries.SelectedIndex][_settings.IndexStart..(_settings.IndexEnd + 1)];
        if (signal is null || signal.Length == 0) return;

        statsTask = Task.Run(() =>
        {
            UpdateStats(signal, _settings.CumulativeDimension, _settings.Entropy);
        }, token);
        await statsTask;

        txtStats.Text = Results.ToString();

        // Update plots
        UpdateOriginal(signal);
        UpdateFractal(signal, stripComboSeries.SelectedItem.ToString() ?? string.Empty, _settings.CumulativeDimension);
        ComboWindow_SelectedIndexChanged(this, EventArgs.Empty);
    }

    private void UpdateUI_Language()
    {
        this.Text = StringsRM.GetString("strFrmTitle");
        
        // Update plots if they contain series
        if(plotOriginal.Plot.GetPlottables().Length > 2)
        {
            plotOriginal.Plot.Title(StringsRM.GetString("strPlotOriginalTitle"));
            plotOriginal.Plot.YLabel(StringsRM.GetString("strPlotOriginalYLabel"));
            plotOriginal.Plot.XLabel(StringsRM.GetString("strPlotOriginalXLabel"));
        }

        if (plotWindow.Plot.GetPlottables().Length > 2)
        {
            plotWindow.Plot.Title(StringsRM.GetString("strPlotWindowTitle"));
            plotWindow.Plot.YLabel(StringsRM.GetString("strPlotWindowYLabel"));
            plotWindow.Plot.XLabel(StringsRM.GetString("strPlotWindowXLabel"));
        }
        if (plotApplied.Plot.GetPlottables().Length > 2)
        {
            plotApplied.Plot.Title(StringsRM.GetString("strPlotAppliedTitle"));
            plotApplied.Plot.YLabel(StringsRM.GetString("strPlotAppliedYLabel"));
            plotApplied.Plot.XLabel(StringsRM.GetString("strPlotAppliedXLabel"));
        }

        if (plotFractal.Plot.GetPlottables().Length > 2)
        {
            plotFractal.Plot.YLabel(StringsRM.GetString("strPlotFractalYLabel"));
            plotFractal.Plot.XLabel(StringsRM.GetString("strPlotFractalXLabel"));
        }

        if (plotFFT.Plot.GetPlottables().Length > 2)
        {
            plotFFT.Plot.Title(StringsRM.GetString("strPlotFFTTitle"));
            plotFFT.Plot.YLabel(_settings.PowerSpectra ? StringsRM.GetString("strPlotFFTYLabelPow") : StringsRM.GetString("strPlotFFTXLabelMag"));
            plotFFT.Plot.XLabel(StringsRM.GetString("strPlotFFTXLabel"));
        }
        
    }

}
