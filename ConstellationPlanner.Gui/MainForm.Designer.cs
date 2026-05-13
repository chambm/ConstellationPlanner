namespace ConstellationPlanner.Gui;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private System.Windows.Forms.SplitContainer _split;
    private System.Windows.Forms.FlowLayoutPanel _leftFlow;
    private System.Windows.Forms.SplitContainer _rightSplit;
    private System.Windows.Forms.FlowLayoutPanel _bottomFlow;
    private System.Windows.Forms.FlowLayoutPanel _layersColumn;
    private System.Windows.Forms.Label _playbackStatus;
    private System.Windows.Forms.PictureBox _pic;
    private System.Windows.Forms.Label _hover;
    private System.Windows.Forms.TextBox _status;

    private System.Windows.Forms.Label _lblOrbitType;
    private System.Windows.Forms.ComboBox _orbitType;
    private System.Windows.Forms.Label _lblApogee;
    private System.Windows.Forms.NumericUpDown _apogee;
    private System.Windows.Forms.Label _lblEccentricity;
    private System.Windows.Forms.Label _eccentricity;
    private System.Windows.Forms.Label _lblLan;
    private System.Windows.Forms.NumericUpDown _lanOffset;
    private System.Windows.Forms.Label _lblArgPe;
    private System.Windows.Forms.NumericUpDown _argPe;
    private System.Windows.Forms.GroupBox _grpConstellation;
    private System.Windows.Forms.GroupBox _grpSatHardware;
    private System.Windows.Forms.GroupBox _grpGroundAntennas;
    private System.Windows.Forms.GroupBox _grpGroundAimList;
    private System.Windows.Forms.GroupBox _grpIslAntennas;
    private System.Windows.Forms.Label _lblSkoposConnection;
    private System.Windows.Forms.ComboBox _skoposConnection;
    private System.Windows.Forms.Button _btnTestAllConnections;
    private System.Windows.Forms.GroupBox _grpRelayPath;
    private System.Windows.Forms.GroupBox _grpRender;
    private System.Windows.Forms.GroupBox _grpLayers;
    private System.Windows.Forms.GroupBox _grpAnimation;

    private System.Windows.Forms.TableLayoutPanel _tlpConstellation;
    private System.Windows.Forms.TableLayoutPanel _tlpSatHardware;
    private System.Windows.Forms.TableLayoutPanel _tlpGroundAntennas;
    private System.Windows.Forms.TableLayoutPanel _tlpIslAntennas;
    private System.Windows.Forms.TableLayoutPanel _tlpRelayPath;
    private System.Windows.Forms.TableLayoutPanel _tlpRender;
    private System.Windows.Forms.TableLayoutPanel _tlpLayers;
    private System.Windows.Forms.TableLayoutPanel _tlpAnimation;

    private System.Windows.Forms.Label _lblAltitude;
    private System.Windows.Forms.NumericUpDown _altitude;
    private System.Windows.Forms.Label _lblInclination;
    private System.Windows.Forms.NumericUpDown _inclination;
    private System.Windows.Forms.Label _lblT;
    private System.Windows.Forms.NumericUpDown _t;
    private System.Windows.Forms.Label _lblP;
    private System.Windows.Forms.NumericUpDown _p;
    private System.Windows.Forms.Label _lblF;
    private System.Windows.Forms.NumericUpDown _f;
    private System.Windows.Forms.Label _lblPhaseOffset;
    private System.Windows.Forms.NumericUpDown _phaseOffset;
    private System.Windows.Forms.Label _lblMinElev;
    private System.Windows.Forms.NumericUpDown _minElev;

    private System.Windows.Forms.Label _lblTechLevel;
    private System.Windows.Forms.NumericUpDown _techLevel;

    private System.Windows.Forms.Label _lblGroundAnt;
    private System.Windows.Forms.ComboBox _groundAnt;
    private System.Windows.Forms.Label _lblGroundBand;
    private System.Windows.Forms.ComboBox _groundBand;
    private System.Windows.Forms.Label _lblGroundStationGain;
    private System.Windows.Forms.Label _groundStationGain;
    private System.Windows.Forms.Label _lblGroundTxPower;
    private System.Windows.Forms.NumericUpDown _groundTxPower;
    private System.Windows.Forms.Label _lblGroundInfoCaption;
    private System.Windows.Forms.Label _groundInfo;

    private System.Windows.Forms.TextBox _groundAnts;

    private System.Windows.Forms.Label _lblIslMode;
    private System.Windows.Forms.ComboBox _islMode;
    private System.Windows.Forms.Label _lblIslAnt;
    private System.Windows.Forms.ComboBox _islAnt;
    private System.Windows.Forms.Label _lblIslBand;
    private System.Windows.Forms.ComboBox _islBand;
    private System.Windows.Forms.Label _lblIslTxPower;
    private System.Windows.Forms.NumericUpDown _islTxPower;
    private System.Windows.Forms.Label _lblIslInfoCaption;
    private System.Windows.Forms.Label _islInfo;

    private System.Windows.Forms.Label _lblPathFrom;
    private System.Windows.Forms.ComboBox _pathFrom;
    private System.Windows.Forms.Label _lblPathTo;
    private System.Windows.Forms.ComboBox _pathTo;
    private System.Windows.Forms.Label _lblRequiredRate;
    private System.Windows.Forms.NumericUpDown _requiredRate;
    private System.Windows.Forms.Label _lblLatencyLimit;
    private System.Windows.Forms.NumericUpDown _latencyLimit;

    private System.Windows.Forms.Label _lblMetric;
    private System.Windows.Forms.ComboBox _metric;
    private System.Windows.Forms.Label _lblCoverageMode;
    private System.Windows.Forms.ComboBox _coverageMode;
    private System.Windows.Forms.Label _lblTimeOffset;
    private System.Windows.Forms.NumericUpDown _timeOffset;
    private System.Windows.Forms.Label _lblUpscale;
    private System.Windows.Forms.NumericUpDown _upscale;

    private System.Windows.Forms.CheckBox _chkTrackingLinks;
    private System.Windows.Forms.CheckBox _chkTelecomLinks;
    private System.Windows.Forms.CheckBox _chkIsls;
    private System.Windows.Forms.CheckBox _chkFootprints;

    private System.Windows.Forms.Label _lblFrameCount;
    private System.Windows.Forms.NumericUpDown _frameCount;
    private System.Windows.Forms.Label _lblAnimDuration;
    private System.Windows.Forms.NumericUpDown _animDurationH;
    private System.Windows.Forms.Label _lblPlayFps;
    private System.Windows.Forms.NumericUpDown _playFps;
    private System.Windows.Forms.Label _lblAnimBtn;
    private System.Windows.Forms.Button _animBtn;

    /// <summary>
    /// Required method for Designer support — do not modify the contents of this method
    /// with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this._split = new System.Windows.Forms.SplitContainer();
        this._leftFlow = new System.Windows.Forms.FlowLayoutPanel();
        this._rightSplit = new System.Windows.Forms.SplitContainer();
        this._bottomFlow = new System.Windows.Forms.FlowLayoutPanel();
        this._layersColumn = new System.Windows.Forms.FlowLayoutPanel();
        this._playbackStatus = new System.Windows.Forms.Label();
        this._pic = new System.Windows.Forms.PictureBox();
        this._hover = new System.Windows.Forms.Label();
        this._status = new System.Windows.Forms.TextBox();
        this._lblOrbitType = new System.Windows.Forms.Label();
        this._orbitType = new System.Windows.Forms.ComboBox();
        this._lblApogee = new System.Windows.Forms.Label();
        this._apogee = new System.Windows.Forms.NumericUpDown();
        this._lblEccentricity = new System.Windows.Forms.Label();
        this._eccentricity = new System.Windows.Forms.Label();
        this._lblLan = new System.Windows.Forms.Label();
        this._lanOffset = new System.Windows.Forms.NumericUpDown();
        this._lblArgPe = new System.Windows.Forms.Label();
        this._argPe = new System.Windows.Forms.NumericUpDown();
        this._grpConstellation = new System.Windows.Forms.GroupBox();
        this._tlpConstellation = new System.Windows.Forms.TableLayoutPanel();
        this._lblAltitude = new System.Windows.Forms.Label();
        this._altitude = new System.Windows.Forms.NumericUpDown();
        this._lblInclination = new System.Windows.Forms.Label();
        this._inclination = new System.Windows.Forms.NumericUpDown();
        this._lblT = new System.Windows.Forms.Label();
        this._t = new System.Windows.Forms.NumericUpDown();
        this._lblP = new System.Windows.Forms.Label();
        this._p = new System.Windows.Forms.NumericUpDown();
        this._lblF = new System.Windows.Forms.Label();
        this._f = new System.Windows.Forms.NumericUpDown();
        this._lblPhaseOffset = new System.Windows.Forms.Label();
        this._phaseOffset = new System.Windows.Forms.NumericUpDown();
        this._lblMinElev = new System.Windows.Forms.Label();
        this._minElev = new System.Windows.Forms.NumericUpDown();
        this._grpSatHardware = new System.Windows.Forms.GroupBox();
        this._tlpSatHardware = new System.Windows.Forms.TableLayoutPanel();
        this._lblTechLevel = new System.Windows.Forms.Label();
        this._techLevel = new System.Windows.Forms.NumericUpDown();
        this._grpGroundAntennas = new System.Windows.Forms.GroupBox();
        this._tlpGroundAntennas = new System.Windows.Forms.TableLayoutPanel();
        this._lblGroundAnt = new System.Windows.Forms.Label();
        this._groundAnt = new System.Windows.Forms.ComboBox();
        this._lblGroundBand = new System.Windows.Forms.Label();
        this._groundBand = new System.Windows.Forms.ComboBox();
        this._lblGroundStationGain = new System.Windows.Forms.Label();
        this._groundStationGain = new System.Windows.Forms.Label();
        this._lblGroundTxPower = new System.Windows.Forms.Label();
        this._groundTxPower = new System.Windows.Forms.NumericUpDown();
        this._lblGroundInfoCaption = new System.Windows.Forms.Label();
        this._groundInfo = new System.Windows.Forms.Label();
        this._grpGroundAimList = new System.Windows.Forms.GroupBox();
        this._groundAnts = new System.Windows.Forms.TextBox();
        this._grpIslAntennas = new System.Windows.Forms.GroupBox();
        this._tlpIslAntennas = new System.Windows.Forms.TableLayoutPanel();
        this._lblIslMode = new System.Windows.Forms.Label();
        this._islMode = new System.Windows.Forms.ComboBox();
        this._lblIslAnt = new System.Windows.Forms.Label();
        this._islAnt = new System.Windows.Forms.ComboBox();
        this._lblIslBand = new System.Windows.Forms.Label();
        this._islBand = new System.Windows.Forms.ComboBox();
        this._lblIslTxPower = new System.Windows.Forms.Label();
        this._islTxPower = new System.Windows.Forms.NumericUpDown();
        this._lblIslInfoCaption = new System.Windows.Forms.Label();
        this._islInfo = new System.Windows.Forms.Label();
        this._lblSkoposConnection = new System.Windows.Forms.Label();
        this._skoposConnection = new System.Windows.Forms.ComboBox();
        this._btnTestAllConnections = new System.Windows.Forms.Button();
        this._grpRelayPath = new System.Windows.Forms.GroupBox();
        this._tlpRelayPath = new System.Windows.Forms.TableLayoutPanel();
        this._lblPathFrom = new System.Windows.Forms.Label();
        this._pathFrom = new System.Windows.Forms.ComboBox();
        this._lblPathTo = new System.Windows.Forms.Label();
        this._pathTo = new System.Windows.Forms.ComboBox();
        this._lblRequiredRate = new System.Windows.Forms.Label();
        this._requiredRate = new System.Windows.Forms.NumericUpDown();
        this._lblLatencyLimit = new System.Windows.Forms.Label();
        this._latencyLimit = new System.Windows.Forms.NumericUpDown();
        this._grpRender = new System.Windows.Forms.GroupBox();
        this._tlpRender = new System.Windows.Forms.TableLayoutPanel();
        this._lblMetric = new System.Windows.Forms.Label();
        this._metric = new System.Windows.Forms.ComboBox();
        this._lblCoverageMode = new System.Windows.Forms.Label();
        this._coverageMode = new System.Windows.Forms.ComboBox();
        this._lblTimeOffset = new System.Windows.Forms.Label();
        this._timeOffset = new System.Windows.Forms.NumericUpDown();
        this._lblUpscale = new System.Windows.Forms.Label();
        this._upscale = new System.Windows.Forms.NumericUpDown();
        this._grpLayers = new System.Windows.Forms.GroupBox();
        this._tlpLayers = new System.Windows.Forms.TableLayoutPanel();
        this._chkTrackingLinks = new System.Windows.Forms.CheckBox();
        this._chkTelecomLinks = new System.Windows.Forms.CheckBox();
        this._chkIsls = new System.Windows.Forms.CheckBox();
        this._chkFootprints = new System.Windows.Forms.CheckBox();
        this._grpAnimation = new System.Windows.Forms.GroupBox();
        this._tlpAnimation = new System.Windows.Forms.TableLayoutPanel();
        this._lblFrameCount = new System.Windows.Forms.Label();
        this._frameCount = new System.Windows.Forms.NumericUpDown();
        this._lblAnimDuration = new System.Windows.Forms.Label();
        this._animDurationH = new System.Windows.Forms.NumericUpDown();
        this._lblPlayFps = new System.Windows.Forms.Label();
        this._playFps = new System.Windows.Forms.NumericUpDown();
        this._lblAnimBtn = new System.Windows.Forms.Label();
        this._animBtn = new System.Windows.Forms.Button();

        ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
        this._split.Panel1.SuspendLayout();
        this._split.Panel2.SuspendLayout();
        this._split.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._rightSplit)).BeginInit();
        this._rightSplit.Panel1.SuspendLayout();
        this._rightSplit.Panel2.SuspendLayout();
        this._rightSplit.SuspendLayout();
        this._bottomFlow.SuspendLayout();
        this._layersColumn.SuspendLayout();
        this._leftFlow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._pic)).BeginInit();
        this._grpConstellation.SuspendLayout();
        this._tlpConstellation.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._altitude)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._apogee)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._inclination)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._lanOffset)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._argPe)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._t)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._p)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._f)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._phaseOffset)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._minElev)).BeginInit();
        this._grpSatHardware.SuspendLayout();
        this._tlpSatHardware.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._techLevel)).BeginInit();
        this._grpGroundAntennas.SuspendLayout();
        this._tlpGroundAntennas.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groundTxPower)).BeginInit();
        this._grpGroundAimList.SuspendLayout();
        this._grpIslAntennas.SuspendLayout();
        this._tlpIslAntennas.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._islTxPower)).BeginInit();
        this._grpRelayPath.SuspendLayout();
        this._tlpRelayPath.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._requiredRate)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._latencyLimit)).BeginInit();
        this._grpRender.SuspendLayout();
        this._tlpRender.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._timeOffset)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._upscale)).BeginInit();
        this._grpLayers.SuspendLayout();
        this._tlpLayers.SuspendLayout();
        this._grpAnimation.SuspendLayout();
        this._tlpAnimation.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this._frameCount)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._animDurationH)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this._playFps)).BeginInit();
        this.SuspendLayout();
        //
        // _split
        //
        this._split.Dock = System.Windows.Forms.DockStyle.Fill;
        this._split.Location = new System.Drawing.Point(0, 0);
        this._split.Name = "_split";
        this._split.Orientation = System.Windows.Forms.Orientation.Vertical;
        this._split.Panel1.Controls.Add(this._leftFlow);
        this._split.Panel1MinSize = 470;
        this._split.Panel2.Controls.Add(this._rightSplit);
        this._split.Size = new System.Drawing.Size(1700, 1080);
        this._split.SplitterDistance = 590;
        this._split.TabIndex = 0;
        //
        // _leftFlow
        //
        this._leftFlow.AutoScroll = true;
        this._leftFlow.Controls.Add(this._grpConstellation);
        this._leftFlow.Controls.Add(this._grpSatHardware);
        this._leftFlow.Controls.Add(this._grpGroundAntennas);
        this._leftFlow.Controls.Add(this._grpGroundAimList);
        this._leftFlow.Controls.Add(this._grpIslAntennas);
        this._leftFlow.Controls.Add(this._grpRelayPath);
        this._leftFlow.Controls.Add(this._grpRender);
        this._leftFlow.Dock = System.Windows.Forms.DockStyle.Fill;
        this._leftFlow.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this._leftFlow.Location = new System.Drawing.Point(0, 0);
        this._leftFlow.Name = "_leftFlow";
        this._leftFlow.Padding = new System.Windows.Forms.Padding(6);
        this._leftFlow.Size = new System.Drawing.Size(590, 1080);
        this._leftFlow.WrapContents = false;
        //
        // _rightSplit (vertical split inside _split.Panel2: top = map / status, bottom = controls)
        //
        this._rightSplit.Dock = System.Windows.Forms.DockStyle.Fill;
        this._rightSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
        this._rightSplit.Name = "_rightSplit";
        this._rightSplit.Panel1.Controls.Add(this._pic);
        this._rightSplit.Panel1.Controls.Add(this._hover);
        // Order matters: docked controls apply in reverse z-order (last-added docks first).
        // _status (Fill) goes in first so it fills whatever's left after _bottomFlow (Left) takes
        // the space its controls need.
        this._rightSplit.Panel2.Controls.Add(this._status);
        this._rightSplit.Panel2.Controls.Add(this._bottomFlow);
        this._rightSplit.Panel2MinSize = 80;
        this._rightSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;   // bottom keeps fixed height when window resizes
        //
        // _bottomFlow (left-to-right flow holding render / layers / animation group boxes)
        //
        this._bottomFlow.AutoScroll = true;
        this._bottomFlow.AutoSize = true;
        this._bottomFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._bottomFlow.Controls.Add(this._layersColumn);
        this._bottomFlow.Controls.Add(this._grpAnimation);
        this._bottomFlow.Dock = System.Windows.Forms.DockStyle.Left;
        this._bottomFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        this._bottomFlow.Name = "_bottomFlow";
        this._bottomFlow.Padding = new System.Windows.Forms.Padding(6);
        this._bottomFlow.WrapContents = false;
        //
        // _layersColumn — vertical sub-panel that stacks the Display Layers group box on top of
        // a small persistent playback-status label so the green status textbox doesn't get
        // overwritten by transient "playing X/Y" updates.
        //
        this._layersColumn.AutoSize = true;
        this._layersColumn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._layersColumn.Controls.Add(this._grpLayers);
        this._layersColumn.Controls.Add(this._playbackStatus);
        this._layersColumn.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this._layersColumn.Margin = new System.Windows.Forms.Padding(0);
        this._layersColumn.Name = "_layersColumn";
        this._layersColumn.WrapContents = false;
        //
        // _playbackStatus
        //
        this._playbackStatus.AutoEllipsis = true;
        this._playbackStatus.AutoSize = false;
        this._playbackStatus.Font = new System.Drawing.Font("Consolas", 9F);
        this._playbackStatus.Margin = new System.Windows.Forms.Padding(3, 4, 3, 8);
        this._playbackStatus.Name = "_playbackStatus";
        this._playbackStatus.Size = new System.Drawing.Size(454, 22);
        this._playbackStatus.Text = "no animation rendered";
        this._playbackStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _pic
        //
        this._pic.BackColor = System.Drawing.Color.Black;
        this._pic.Dock = System.Windows.Forms.DockStyle.Fill;
        this._pic.Location = new System.Drawing.Point(0, 0);
        this._pic.Name = "_pic";
        this._pic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this._pic.TabStop = false;
        //
        // _hover
        //
        this._hover.BackColor = System.Drawing.Color.Black;
        this._hover.Dock = System.Windows.Forms.DockStyle.Bottom;
        this._hover.Font = new System.Drawing.Font("Consolas", 9F);
        this._hover.ForeColor = System.Drawing.Color.LightGreen;
        this._hover.Name = "_hover";
        this._hover.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
        this._hover.Size = new System.Drawing.Size(1106, 22);
        this._hover.Text = "";
        this._hover.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _status
        //
        this._status.BackColor = System.Drawing.Color.Black;
        this._status.Dock = System.Windows.Forms.DockStyle.Fill;
        this._status.Font = new System.Drawing.Font("Consolas", 9F);
        this._status.ForeColor = System.Drawing.Color.LightGreen;
        this._status.Multiline = true;
        this._status.Name = "_status";
        this._status.ReadOnly = true;
        this._status.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        //
        // _grpConstellation
        //
        this._grpConstellation.Controls.Add(this._tlpConstellation);
        this._grpConstellation.Location = new System.Drawing.Point(9, 9);
        this._grpConstellation.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpConstellation.Name = "_grpConstellation";
        this._grpConstellation.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpConstellation.Size = new System.Drawing.Size(460, 380);
        this._grpConstellation.TabStop = false;
        this._grpConstellation.Text = "Constellation";
        //
        // _tlpConstellation
        //
        this._tlpConstellation.ColumnCount = 2;
        this._tlpConstellation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpConstellation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpConstellation.Controls.Add(this._lblOrbitType, 0, 0);
        this._tlpConstellation.Controls.Add(this._orbitType, 1, 0);
        this._tlpConstellation.Controls.Add(this._lblAltitude, 0, 1);
        this._tlpConstellation.Controls.Add(this._altitude, 1, 1);
        this._tlpConstellation.Controls.Add(this._lblApogee, 0, 2);
        this._tlpConstellation.Controls.Add(this._apogee, 1, 2);
        this._tlpConstellation.Controls.Add(this._lblEccentricity, 0, 3);
        this._tlpConstellation.Controls.Add(this._eccentricity, 1, 3);
        this._tlpConstellation.Controls.Add(this._lblInclination, 0, 4);
        this._tlpConstellation.Controls.Add(this._inclination, 1, 4);
        this._tlpConstellation.Controls.Add(this._lblLan, 0, 5);
        this._tlpConstellation.Controls.Add(this._lanOffset, 1, 5);
        this._tlpConstellation.Controls.Add(this._lblArgPe, 0, 6);
        this._tlpConstellation.Controls.Add(this._argPe, 1, 6);
        this._tlpConstellation.Controls.Add(this._lblT, 0, 7);
        this._tlpConstellation.Controls.Add(this._t, 1, 7);
        this._tlpConstellation.Controls.Add(this._lblP, 0, 8);
        this._tlpConstellation.Controls.Add(this._p, 1, 8);
        this._tlpConstellation.Controls.Add(this._lblF, 0, 9);
        this._tlpConstellation.Controls.Add(this._f, 1, 9);
        this._tlpConstellation.Controls.Add(this._lblPhaseOffset, 0, 10);
        this._tlpConstellation.Controls.Add(this._phaseOffset, 1, 10);
        this._tlpConstellation.Controls.Add(this._lblMinElev, 0, 11);
        this._tlpConstellation.Controls.Add(this._minElev, 1, 11);
        this._tlpConstellation.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpConstellation.Location = new System.Drawing.Point(8, 24);
        this._tlpConstellation.Name = "_tlpConstellation";
        this._tlpConstellation.RowCount = 12;
        for (int _r = 0; _r < 12; _r++)
            this._tlpConstellation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpConstellation.Size = new System.Drawing.Size(444, 348);
        //
        // _lblOrbitType
        //
        this._lblOrbitType.AutoSize = false;
        this._lblOrbitType.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblOrbitType.Name = "_lblOrbitType";
        this._lblOrbitType.Size = new System.Drawing.Size(175, 24);
        this._lblOrbitType.Text = "Orbit type";
        this._lblOrbitType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _orbitType
        //
        this._orbitType.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._orbitType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._orbitType.Items.AddRange(new object[] { "Walker-delta (circular)", "Walker-star (polar circular)", "Molniya (12h, i=63.4°, ω=270°)", "Tundra (24h, i=63.4°, ω=270°)", "Custom (free-form)" });
        this._orbitType.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._orbitType.Name = "_orbitType";
        this._orbitType.Size = new System.Drawing.Size(260, 23);
        //
        // _lblAltitude
        //
        this._lblAltitude.AutoSize = false;
        this._lblAltitude.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblAltitude.Name = "_lblAltitude";
        this._lblAltitude.Size = new System.Drawing.Size(175, 24);
        this._lblAltitude.Text = "Altitude (km)";
        this._lblAltitude.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _lblApogee
        //
        this._lblApogee.AutoSize = false;
        this._lblApogee.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblApogee.Name = "_lblApogee";
        this._lblApogee.Size = new System.Drawing.Size(175, 24);
        this._lblApogee.Text = "Apogee (km)";
        this._lblApogee.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _apogee
        //
        this._apogee.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._apogee.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        this._apogee.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._apogee.Maximum = new decimal(new int[] { 200000, 0, 0, 0 });
        this._apogee.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
        this._apogee.Name = "_apogee";
        this._apogee.Size = new System.Drawing.Size(260, 23);
        this._apogee.Value = new decimal(new int[] { 35786, 0, 0, 0 });
        //
        // _lblEccentricity
        //
        this._lblEccentricity.AutoSize = false;
        this._lblEccentricity.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblEccentricity.Name = "_lblEccentricity";
        this._lblEccentricity.Size = new System.Drawing.Size(175, 24);
        this._lblEccentricity.Text = "Eccentricity";
        this._lblEccentricity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _eccentricity (read-only display label, derived from Pe + Ap)
        //
        this._eccentricity.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._eccentricity.AutoSize = false;
        this._eccentricity.Font = new System.Drawing.Font("Consolas", 9F);
        this._eccentricity.ForeColor = System.Drawing.Color.DarkGreen;
        this._eccentricity.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._eccentricity.Name = "_eccentricity";
        this._eccentricity.Size = new System.Drawing.Size(260, 23);
        this._eccentricity.Text = "0.000";
        this._eccentricity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _lblLan
        //
        this._lblLan.AutoSize = false;
        this._lblLan.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblLan.Name = "_lblLan";
        this._lblLan.Size = new System.Drawing.Size(175, 24);
        this._lblLan.Text = "LAN offset (°)";
        this._lblLan.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _lanOffset
        //
        this._lanOffset.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._lanOffset.DecimalPlaces = 1;
        this._lanOffset.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._lanOffset.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        this._lanOffset.Minimum = new decimal(new int[] { 360, 0, 0, int.MinValue });
        this._lanOffset.Name = "_lanOffset";
        this._lanOffset.Size = new System.Drawing.Size(260, 23);
        //
        // _lblArgPe
        //
        this._lblArgPe.AutoSize = false;
        this._lblArgPe.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblArgPe.Name = "_lblArgPe";
        this._lblArgPe.Size = new System.Drawing.Size(175, 24);
        this._lblArgPe.Text = "Arg. perigee ω (°)";
        this._lblArgPe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _argPe
        //
        this._argPe.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._argPe.DecimalPlaces = 1;
        this._argPe.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._argPe.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        this._argPe.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        this._argPe.Name = "_argPe";
        this._argPe.Size = new System.Drawing.Size(260, 23);
        this._argPe.Value = new decimal(new int[] { 270, 0, 0, 0 });
        //
        // _altitude
        //
        this._altitude.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._altitude.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        this._altitude.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._altitude.Maximum = new decimal(new int[] { 200000, 0, 0, 0 });
        this._altitude.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
        this._altitude.Name = "_altitude";
        this._altitude.Size = new System.Drawing.Size(260, 23);
        this._altitude.Value = new decimal(new int[] { 35786, 0, 0, 0 });
        //
        // _lblInclination
        //
        this._lblInclination.AutoSize = false;
        this._lblInclination.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblInclination.Name = "_lblInclination";
        this._lblInclination.Size = new System.Drawing.Size(175, 24);
        this._lblInclination.Text = "Inclination (°)";
        this._lblInclination.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _inclination
        //
        this._inclination.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._inclination.DecimalPlaces = 1;
        this._inclination.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._inclination.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
        this._inclination.Name = "_inclination";
        this._inclination.Size = new System.Drawing.Size(260, 23);
        //
        // _lblT
        //
        this._lblT.AutoSize = false;
        this._lblT.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblT.Name = "_lblT";
        this._lblT.Size = new System.Drawing.Size(175, 24);
        this._lblT.Text = "T (sats total)";
        this._lblT.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _t
        //
        this._t.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._t.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._t.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        this._t.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._t.Name = "_t";
        this._t.Size = new System.Drawing.Size(260, 23);
        this._t.Value = new decimal(new int[] { 4, 0, 0, 0 });
        //
        // _lblP
        //
        this._lblP.AutoSize = false;
        this._lblP.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblP.Name = "_lblP";
        this._lblP.Size = new System.Drawing.Size(175, 24);
        this._lblP.Text = "P (planes)";
        this._lblP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _p
        //
        this._p.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._p.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._p.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        this._p.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._p.Name = "_p";
        this._p.Size = new System.Drawing.Size(260, 23);
        this._p.Value = new decimal(new int[] { 1, 0, 0, 0 });
        //
        // _lblF
        //
        this._lblF.AutoSize = false;
        this._lblF.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblF.Name = "_lblF";
        this._lblF.Size = new System.Drawing.Size(175, 24);
        this._lblF.Text = "F (phasing)";
        this._lblF.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _f
        //
        this._f.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._f.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._f.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        this._f.Name = "_f";
        this._f.Size = new System.Drawing.Size(260, 23);
        //
        // _lblPhaseOffset
        //
        this._lblPhaseOffset.AutoSize = false;
        this._lblPhaseOffset.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblPhaseOffset.Name = "_lblPhaseOffset";
        this._lblPhaseOffset.Size = new System.Drawing.Size(175, 24);
        this._lblPhaseOffset.Text = "Phase offset (°)";
        this._lblPhaseOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _phaseOffset
        //
        this._phaseOffset.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._phaseOffset.DecimalPlaces = 1;
        this._phaseOffset.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        this._phaseOffset.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._phaseOffset.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        this._phaseOffset.Minimum = new decimal(new int[] { 360, 0, 0, int.MinValue });
        this._phaseOffset.Name = "_phaseOffset";
        this._phaseOffset.Size = new System.Drawing.Size(260, 23);
        this._phaseOffset.Value = new decimal(new int[] { 45, 0, 0, 0 });
        //
        // _lblMinElev
        //
        this._lblMinElev.AutoSize = false;
        this._lblMinElev.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblMinElev.Name = "_lblMinElev";
        this._lblMinElev.Size = new System.Drawing.Size(175, 24);
        this._lblMinElev.Text = "Min elevation (°)";
        this._lblMinElev.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _minElev
        //
        this._minElev.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._minElev.DecimalPlaces = 1;
        this._minElev.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._minElev.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
        this._minElev.Name = "_minElev";
        this._minElev.Size = new System.Drawing.Size(260, 23);
        this._minElev.Value = new decimal(new int[] { 10, 0, 0, 0 });
        //
        // _grpSatHardware
        //
        this._grpSatHardware.Controls.Add(this._tlpSatHardware);
        this._grpSatHardware.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpSatHardware.Name = "_grpSatHardware";
        this._grpSatHardware.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpSatHardware.Size = new System.Drawing.Size(460, 60);
        this._grpSatHardware.TabStop = false;
        this._grpSatHardware.Text = "Sat hardware";
        //
        // _tlpSatHardware
        //
        this._tlpSatHardware.ColumnCount = 2;
        this._tlpSatHardware.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpSatHardware.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpSatHardware.Controls.Add(this._lblTechLevel, 0, 0);
        this._tlpSatHardware.Controls.Add(this._techLevel, 1, 0);
        this._tlpSatHardware.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpSatHardware.Name = "_tlpSatHardware";
        this._tlpSatHardware.RowCount = 1;
        this._tlpSatHardware.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _lblTechLevel
        //
        this._lblTechLevel.AutoSize = false;
        this._lblTechLevel.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblTechLevel.Name = "_lblTechLevel";
        this._lblTechLevel.Size = new System.Drawing.Size(175, 24);
        this._lblTechLevel.Text = "Tech level (0-10)";
        this._lblTechLevel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _techLevel
        //
        this._techLevel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._techLevel.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._techLevel.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        this._techLevel.Name = "_techLevel";
        this._techLevel.Size = new System.Drawing.Size(260, 23);
        this._techLevel.Value = new decimal(new int[] { 3, 0, 0, 0 });
        //
        // _grpGroundAntennas
        //
        this._grpGroundAntennas.Controls.Add(this._tlpGroundAntennas);
        this._grpGroundAntennas.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpGroundAntennas.Name = "_grpGroundAntennas";
        this._grpGroundAntennas.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpGroundAntennas.Size = new System.Drawing.Size(460, 175);
        this._grpGroundAntennas.TabStop = false;
        this._grpGroundAntennas.Text = "Ground antennas";
        //
        // _tlpGroundAntennas
        //
        this._tlpGroundAntennas.ColumnCount = 2;
        this._tlpGroundAntennas.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpGroundAntennas.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpGroundAntennas.Controls.Add(this._lblGroundAnt, 0, 0);
        this._tlpGroundAntennas.Controls.Add(this._groundAnt, 1, 0);
        this._tlpGroundAntennas.Controls.Add(this._lblGroundBand, 0, 1);
        this._tlpGroundAntennas.Controls.Add(this._groundBand, 1, 1);
        this._tlpGroundAntennas.Controls.Add(this._lblGroundStationGain, 0, 2);
        this._tlpGroundAntennas.Controls.Add(this._groundStationGain, 1, 2);
        this._tlpGroundAntennas.Controls.Add(this._lblGroundTxPower, 0, 3);
        this._tlpGroundAntennas.Controls.Add(this._groundTxPower, 1, 3);
        this._tlpGroundAntennas.Controls.Add(this._lblGroundInfoCaption, 0, 4);
        this._tlpGroundAntennas.Controls.Add(this._groundInfo, 1, 4);
        this._tlpGroundAntennas.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpGroundAntennas.Name = "_tlpGroundAntennas";
        this._tlpGroundAntennas.RowCount = 5;
        this._tlpGroundAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpGroundAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpGroundAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpGroundAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpGroundAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _lblGroundAnt
        //
        this._lblGroundAnt.AutoSize = false;
        this._lblGroundAnt.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblGroundAnt.Name = "_lblGroundAnt";
        this._lblGroundAnt.Size = new System.Drawing.Size(175, 24);
        this._lblGroundAnt.Text = "Antenna model";
        this._lblGroundAnt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _groundAnt
        //
        this._groundAnt.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groundAnt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._groundAnt.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._groundAnt.Name = "_groundAnt";
        this._groundAnt.Size = new System.Drawing.Size(260, 23);
        //
        // _lblGroundBand
        //
        this._lblGroundBand.AutoSize = false;
        this._lblGroundBand.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblGroundBand.Name = "_lblGroundBand";
        this._lblGroundBand.Size = new System.Drawing.Size(175, 24);
        this._lblGroundBand.Text = "Band";
        this._lblGroundBand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _groundBand
        //
        this._groundBand.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groundBand.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._groundBand.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._groundBand.Name = "_groundBand";
        this._groundBand.Size = new System.Drawing.Size(260, 23);
        //
        // _lblGroundStationGain
        //
        this._lblGroundStationGain.AutoSize = false;
        this._lblGroundStationGain.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblGroundStationGain.Name = "_lblGroundStationGain";
        this._lblGroundStationGain.Size = new System.Drawing.Size(175, 24);
        this._lblGroundStationGain.Text = "Station gain (dBi)";
        this._lblGroundStationGain.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _groundStationGain (read-only label, auto-derived from telecom.cfg per-station data)
        //
        this._groundStationGain.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groundStationGain.AutoEllipsis = true;
        this._groundStationGain.AutoSize = false;
        this._groundStationGain.Font = new System.Drawing.Font("Consolas", 9F);
        this._groundStationGain.ForeColor = System.Drawing.Color.DarkGreen;
        this._groundStationGain.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._groundStationGain.Name = "_groundStationGain";
        this._groundStationGain.Size = new System.Drawing.Size(260, 23);
        this._groundStationGain.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _lblGroundTxPower
        //
        this._lblGroundTxPower.AutoSize = false;
        this._lblGroundTxPower.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblGroundTxPower.Name = "_lblGroundTxPower";
        this._lblGroundTxPower.Size = new System.Drawing.Size(175, 24);
        this._lblGroundTxPower.Text = "Sat TX power (dBm, 0=TL)";
        this._lblGroundTxPower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _groundTxPower
        //
        this._groundTxPower.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groundTxPower.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._groundTxPower.Maximum = new decimal(new int[] { 80, 0, 0, 0 });
        this._groundTxPower.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        this._groundTxPower.Name = "_groundTxPower";
        this._groundTxPower.Size = new System.Drawing.Size(260, 23);
        this._groundTxPower.Value = new decimal(new int[] { 0, 0, 0, 0 });
        //
        // _lblGroundInfoCaption
        //
        this._lblGroundInfoCaption.AutoSize = false;
        this._lblGroundInfoCaption.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblGroundInfoCaption.Name = "_lblGroundInfoCaption";
        this._lblGroundInfoCaption.Size = new System.Drawing.Size(175, 24);
        this._lblGroundInfoCaption.Text = "→ resulting";
        this._lblGroundInfoCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _groundInfo
        //
        this._groundInfo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._groundInfo.AutoEllipsis = true;
        this._groundInfo.AutoSize = false;
        this._groundInfo.Font = new System.Drawing.Font("Consolas", 9F);
        this._groundInfo.ForeColor = System.Drawing.Color.DarkGreen;
        this._groundInfo.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._groundInfo.Name = "_groundInfo";
        this._groundInfo.Size = new System.Drawing.Size(260, 22);
        this._groundInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _grpGroundAimList
        //
        this._grpGroundAimList.Controls.Add(this._groundAnts);
        this._grpGroundAimList.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpGroundAimList.Name = "_grpGroundAimList";
        this._grpGroundAimList.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpGroundAimList.Size = new System.Drawing.Size(460, 130);
        this._grpGroundAimList.TabStop = false;
        this._grpGroundAimList.Text = "Ground aim list (name az° el° per line)";
        //
        // _groundAnts
        //
        this._groundAnts.AcceptsReturn = true;
        this._groundAnts.Anchor = System.Windows.Forms.AnchorStyles.Top
                                | System.Windows.Forms.AnchorStyles.Left
                                | System.Windows.Forms.AnchorStyles.Right
                                | System.Windows.Forms.AnchorStyles.Bottom;
        this._groundAnts.Font = new System.Drawing.Font("Consolas", 9F);
        this._groundAnts.Location = new System.Drawing.Point(8, 24);
        this._groundAnts.Multiline = true;
        this._groundAnts.Name = "_groundAnts";
        this._groundAnts.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this._groundAnts.Size = new System.Drawing.Size(444, 90);
        this._groundAnts.Text = "nadir 270 0";
        this._groundAnts.WordWrap = false;
        //
        // _grpIslAntennas
        //
        this._grpIslAntennas.Controls.Add(this._tlpIslAntennas);
        this._grpIslAntennas.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpIslAntennas.Name = "_grpIslAntennas";
        this._grpIslAntennas.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpIslAntennas.Size = new System.Drawing.Size(460, 170);
        this._grpIslAntennas.TabStop = false;
        this._grpIslAntennas.Text = "ISL antennas";
        //
        // _tlpIslAntennas
        //
        this._tlpIslAntennas.ColumnCount = 2;
        this._tlpIslAntennas.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpIslAntennas.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpIslAntennas.Controls.Add(this._lblIslMode, 0, 0);
        this._tlpIslAntennas.Controls.Add(this._islMode, 1, 0);
        this._tlpIslAntennas.Controls.Add(this._lblIslAnt, 0, 1);
        this._tlpIslAntennas.Controls.Add(this._islAnt, 1, 1);
        this._tlpIslAntennas.Controls.Add(this._lblIslBand, 0, 2);
        this._tlpIslAntennas.Controls.Add(this._islBand, 1, 2);
        this._tlpIslAntennas.Controls.Add(this._lblIslTxPower, 0, 3);
        this._tlpIslAntennas.Controls.Add(this._islTxPower, 1, 3);
        this._tlpIslAntennas.Controls.Add(this._lblIslInfoCaption, 0, 4);
        this._tlpIslAntennas.Controls.Add(this._islInfo, 1, 4);
        this._tlpIslAntennas.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpIslAntennas.Name = "_tlpIslAntennas";
        this._tlpIslAntennas.RowCount = 5;
        this._tlpIslAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpIslAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpIslAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpIslAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpIslAntennas.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _lblIslMode
        //
        this._lblIslMode.AutoSize = false;
        this._lblIslMode.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblIslMode.Name = "_lblIslMode";
        this._lblIslMode.Size = new System.Drawing.Size(175, 24);
        this._lblIslMode.Text = "Mode";
        this._lblIslMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _islMode
        //
        this._islMode.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._islMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._islMode.Items.AddRange(new object[] { "None", "Omni", "Directional (4× @ 45°: fwd/aft/port/stbd)", "Targeted (4× lock-on-neighbour)" });
        this._islMode.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._islMode.Name = "_islMode";
        this._islMode.Size = new System.Drawing.Size(260, 23);
        //
        // _lblIslAnt
        //
        this._lblIslAnt.AutoSize = false;
        this._lblIslAnt.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblIslAnt.Name = "_lblIslAnt";
        this._lblIslAnt.Size = new System.Drawing.Size(175, 24);
        this._lblIslAnt.Text = "Antenna model";
        this._lblIslAnt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _islAnt
        //
        this._islAnt.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._islAnt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._islAnt.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._islAnt.Name = "_islAnt";
        this._islAnt.Size = new System.Drawing.Size(260, 23);
        //
        // _lblIslBand
        //
        this._lblIslBand.AutoSize = false;
        this._lblIslBand.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblIslBand.Name = "_lblIslBand";
        this._lblIslBand.Size = new System.Drawing.Size(175, 24);
        this._lblIslBand.Text = "Band";
        this._lblIslBand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _islBand
        //
        this._islBand.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._islBand.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._islBand.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._islBand.Name = "_islBand";
        this._islBand.Size = new System.Drawing.Size(260, 23);
        //
        // _lblIslTxPower
        //
        this._lblIslTxPower.AutoSize = false;
        this._lblIslTxPower.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblIslTxPower.Name = "_lblIslTxPower";
        this._lblIslTxPower.Size = new System.Drawing.Size(175, 24);
        this._lblIslTxPower.Text = "Sat TX power (dBm, 0=TL)";
        this._lblIslTxPower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _islTxPower
        //
        this._islTxPower.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._islTxPower.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._islTxPower.Maximum = new decimal(new int[] { 80, 0, 0, 0 });
        this._islTxPower.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        this._islTxPower.Name = "_islTxPower";
        this._islTxPower.Size = new System.Drawing.Size(260, 23);
        this._islTxPower.Value = new decimal(new int[] { 0, 0, 0, 0 });
        //
        // _lblIslInfoCaption
        //
        this._lblIslInfoCaption.AutoSize = false;
        this._lblIslInfoCaption.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblIslInfoCaption.Name = "_lblIslInfoCaption";
        this._lblIslInfoCaption.Size = new System.Drawing.Size(175, 24);
        this._lblIslInfoCaption.Text = "→ resulting";
        this._lblIslInfoCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _islInfo
        //
        this._islInfo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._islInfo.AutoEllipsis = true;
        this._islInfo.AutoSize = false;
        this._islInfo.Font = new System.Drawing.Font("Consolas", 9F);
        this._islInfo.ForeColor = System.Drawing.Color.DarkGreen;
        this._islInfo.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._islInfo.Name = "_islInfo";
        this._islInfo.Size = new System.Drawing.Size(260, 22);
        this._islInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _grpRelayPath
        //
        this._grpRelayPath.Controls.Add(this._tlpRelayPath);
        this._grpRelayPath.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpRelayPath.Name = "_grpRelayPath";
        this._grpRelayPath.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpRelayPath.Size = new System.Drawing.Size(460, 200);
        this._grpRelayPath.TabStop = false;
        this._grpRelayPath.Text = "Relay path";
        //
        // _tlpRelayPath
        //
        this._tlpRelayPath.ColumnCount = 2;
        this._tlpRelayPath.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpRelayPath.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpRelayPath.Controls.Add(this._lblSkoposConnection, 0, 0);
        this._tlpRelayPath.Controls.Add(this._skoposConnection, 1, 0);
        this._tlpRelayPath.Controls.Add(this._lblPathFrom, 0, 1);
        this._tlpRelayPath.Controls.Add(this._pathFrom, 1, 1);
        this._tlpRelayPath.Controls.Add(this._lblPathTo, 0, 2);
        this._tlpRelayPath.Controls.Add(this._pathTo, 1, 2);
        this._tlpRelayPath.Controls.Add(this._lblRequiredRate, 0, 3);
        this._tlpRelayPath.Controls.Add(this._requiredRate, 1, 3);
        this._tlpRelayPath.Controls.Add(this._lblLatencyLimit, 0, 4);
        this._tlpRelayPath.Controls.Add(this._latencyLimit, 1, 4);
        this._tlpRelayPath.Controls.Add(this._btnTestAllConnections, 1, 5);
        this._tlpRelayPath.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpRelayPath.Name = "_tlpRelayPath";
        this._tlpRelayPath.RowCount = 6;
        for (int _r = 0; _r < 6; _r++)
            this._tlpRelayPath.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _btnTestAllConnections
        //
        this._btnTestAllConnections.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._btnTestAllConnections.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
        this._btnTestAllConnections.Name = "_btnTestAllConnections";
        this._btnTestAllConnections.Size = new System.Drawing.Size(260, 26);
        this._btnTestAllConnections.Text = "Test all Skopos connections (multi-conn capacity)";
        this._btnTestAllConnections.UseVisualStyleBackColor = true;
        //
        // _lblSkoposConnection
        //
        this._lblSkoposConnection.AutoSize = false;
        this._lblSkoposConnection.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblSkoposConnection.Name = "_lblSkoposConnection";
        this._lblSkoposConnection.Size = new System.Drawing.Size(175, 24);
        this._lblSkoposConnection.Text = "Skopos connection";
        this._lblSkoposConnection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _skoposConnection
        //
        this._skoposConnection.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._skoposConnection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._skoposConnection.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._skoposConnection.Name = "_skoposConnection";
        this._skoposConnection.Size = new System.Drawing.Size(260, 23);
        //
        // _lblPathFrom
        //
        this._lblPathFrom.AutoSize = false;
        this._lblPathFrom.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblPathFrom.Name = "_lblPathFrom";
        this._lblPathFrom.Size = new System.Drawing.Size(175, 24);
        this._lblPathFrom.Text = "From station";
        this._lblPathFrom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _pathFrom
        //
        this._pathFrom.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._pathFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._pathFrom.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._pathFrom.Name = "_pathFrom";
        this._pathFrom.Size = new System.Drawing.Size(260, 23);
        this._pathFrom.Sorted = true;
        //
        // _lblPathTo
        //
        this._lblPathTo.AutoSize = false;
        this._lblPathTo.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblPathTo.Name = "_lblPathTo";
        this._lblPathTo.Size = new System.Drawing.Size(175, 24);
        this._lblPathTo.Text = "To station";
        this._lblPathTo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _pathTo
        //
        this._pathTo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._pathTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._pathTo.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._pathTo.Name = "_pathTo";
        this._pathTo.Size = new System.Drawing.Size(260, 23);
        this._pathTo.Sorted = true;
        //
        // _lblRequiredRate
        //
        this._lblRequiredRate.AutoSize = false;
        this._lblRequiredRate.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblRequiredRate.Name = "_lblRequiredRate";
        this._lblRequiredRate.Size = new System.Drawing.Size(175, 24);
        this._lblRequiredRate.Text = "Required rate (Mbps)";
        this._lblRequiredRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _requiredRate
        //
        this._requiredRate.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._requiredRate.DecimalPlaces = 2;
        this._requiredRate.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._requiredRate.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        this._requiredRate.Name = "_requiredRate";
        this._requiredRate.Size = new System.Drawing.Size(260, 23);
        //
        // _lblLatencyLimit
        //
        this._lblLatencyLimit.AutoSize = false;
        this._lblLatencyLimit.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblLatencyLimit.Name = "_lblLatencyLimit";
        this._lblLatencyLimit.Size = new System.Drawing.Size(175, 24);
        this._lblLatencyLimit.Text = "Latency limit (s)";
        this._lblLatencyLimit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _latencyLimit
        //
        this._latencyLimit.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._latencyLimit.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._latencyLimit.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this._latencyLimit.Name = "_latencyLimit";
        this._latencyLimit.Size = new System.Drawing.Size(260, 23);
        this._latencyLimit.Value = new decimal(new int[] { 30, 0, 0, 0 });
        //
        // _grpRender
        //
        this._grpRender.Controls.Add(this._tlpRender);
        this._grpRender.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpRender.Name = "_grpRender";
        this._grpRender.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpRender.Size = new System.Drawing.Size(460, 140);
        this._grpRender.TabStop = false;
        this._grpRender.Text = "Render";
        //
        // _tlpRender
        //
        this._tlpRender.ColumnCount = 2;
        this._tlpRender.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpRender.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpRender.Controls.Add(this._lblMetric, 0, 0);
        this._tlpRender.Controls.Add(this._metric, 1, 0);
        this._tlpRender.Controls.Add(this._lblCoverageMode, 0, 1);
        this._tlpRender.Controls.Add(this._coverageMode, 1, 1);
        this._tlpRender.Controls.Add(this._lblTimeOffset, 0, 2);
        this._tlpRender.Controls.Add(this._timeOffset, 1, 2);
        this._tlpRender.Controls.Add(this._lblUpscale, 0, 3);
        this._tlpRender.Controls.Add(this._upscale, 1, 3);
        this._tlpRender.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpRender.Name = "_tlpRender";
        this._tlpRender.RowCount = 4;
        this._tlpRender.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpRender.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpRender.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpRender.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _lblMetric
        //
        this._lblMetric.AutoSize = false;
        this._lblMetric.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblMetric.Name = "_lblMetric";
        this._lblMetric.Size = new System.Drawing.Size(175, 24);
        this._lblMetric.Text = "Heatmap metric";
        this._lblMetric.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _metric
        //
        this._metric.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._metric.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._metric.Items.AddRange(new object[] { "rx-power (dBm)", "data rate (bps)" });
        this._metric.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._metric.Name = "_metric";
        this._metric.Size = new System.Drawing.Size(260, 23);
        //
        // _lblCoverageMode
        //
        this._lblCoverageMode.AutoSize = false;
        this._lblCoverageMode.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblCoverageMode.Name = "_lblCoverageMode";
        this._lblCoverageMode.Size = new System.Drawing.Size(175, 24);
        this._lblCoverageMode.Text = "Coverage mode";
        this._lblCoverageMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _coverageMode
        //
        this._coverageMode.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._coverageMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._coverageMode.Items.AddRange(new object[] { "Daily average (best across one sidereal day)", "Instantaneous (single moment)" });
        this._coverageMode.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._coverageMode.Name = "_coverageMode";
        this._coverageMode.Size = new System.Drawing.Size(260, 23);
        //
        // _lblTimeOffset
        //
        this._lblTimeOffset.AutoSize = false;
        this._lblTimeOffset.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblTimeOffset.Name = "_lblTimeOffset";
        this._lblTimeOffset.Size = new System.Drawing.Size(175, 24);
        this._lblTimeOffset.Text = "Time offset (h)";
        this._lblTimeOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _timeOffset
        //
        this._timeOffset.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._timeOffset.DecimalPlaces = 2;
        this._timeOffset.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        this._timeOffset.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._timeOffset.Maximum = new decimal(new int[] { 24, 0, 0, 0 });
        this._timeOffset.Name = "_timeOffset";
        this._timeOffset.Size = new System.Drawing.Size(260, 23);
        //
        // _lblUpscale
        //
        this._lblUpscale.AutoSize = false;
        this._lblUpscale.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblUpscale.Name = "_lblUpscale";
        this._lblUpscale.Size = new System.Drawing.Size(175, 24);
        this._lblUpscale.Text = "Upscale (1-8)";
        this._lblUpscale.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _upscale
        //
        this._upscale.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._upscale.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._upscale.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        this._upscale.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._upscale.Name = "_upscale";
        this._upscale.Size = new System.Drawing.Size(260, 23);
        this._upscale.Value = new decimal(new int[] { 3, 0, 0, 0 });
        //
        // _grpLayers
        //
        this._grpLayers.Controls.Add(this._tlpLayers);
        this._grpLayers.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpLayers.Name = "_grpLayers";
        this._grpLayers.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpLayers.Size = new System.Drawing.Size(460, 80);
        this._grpLayers.TabStop = false;
        this._grpLayers.Text = "Display layers";
        //
        // _tlpLayers
        //
        this._tlpLayers.ColumnCount = 2;
        this._tlpLayers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._tlpLayers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this._tlpLayers.Controls.Add(this._chkTrackingLinks, 0, 0);
        this._tlpLayers.Controls.Add(this._chkTelecomLinks, 1, 0);
        this._tlpLayers.Controls.Add(this._chkIsls, 0, 1);
        this._tlpLayers.Controls.Add(this._chkFootprints, 1, 1);
        this._tlpLayers.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpLayers.Name = "_tlpLayers";
        this._tlpLayers.RowCount = 2;
        this._tlpLayers.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpLayers.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _chkTrackingLinks
        //
        this._chkTrackingLinks.AutoSize = true;
        this._chkTrackingLinks.Checked = true;
        this._chkTrackingLinks.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkTrackingLinks.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
        this._chkTrackingLinks.Name = "_chkTrackingLinks";
        this._chkTrackingLinks.Text = "Tracking station links";
        this._chkTrackingLinks.UseVisualStyleBackColor = true;
        //
        // _chkTelecomLinks
        //
        this._chkTelecomLinks.AutoSize = true;
        this._chkTelecomLinks.Checked = true;
        this._chkTelecomLinks.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkTelecomLinks.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
        this._chkTelecomLinks.Name = "_chkTelecomLinks";
        this._chkTelecomLinks.Text = "Telecom station links";
        this._chkTelecomLinks.UseVisualStyleBackColor = true;
        //
        // _chkIsls
        //
        this._chkIsls.AutoSize = true;
        this._chkIsls.Checked = true;
        this._chkIsls.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkIsls.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
        this._chkIsls.Name = "_chkIsls";
        this._chkIsls.Text = "ISLs";
        this._chkIsls.UseVisualStyleBackColor = true;
        //
        // _chkFootprints
        //
        this._chkFootprints.AutoSize = true;
        this._chkFootprints.Checked = true;
        this._chkFootprints.CheckState = System.Windows.Forms.CheckState.Checked;
        this._chkFootprints.Margin = new System.Windows.Forms.Padding(0, 2, 4, 2);
        this._chkFootprints.Name = "_chkFootprints";
        this._chkFootprints.Text = "Footprints";
        this._chkFootprints.UseVisualStyleBackColor = true;
        //
        // _grpAnimation
        //
        this._grpAnimation.Controls.Add(this._tlpAnimation);
        this._grpAnimation.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
        this._grpAnimation.Name = "_grpAnimation";
        this._grpAnimation.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
        this._grpAnimation.Size = new System.Drawing.Size(460, 175);
        this._grpAnimation.TabStop = false;
        this._grpAnimation.Text = "Animation";
        //
        // _tlpAnimation
        //
        this._tlpAnimation.ColumnCount = 2;
        this._tlpAnimation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
        this._tlpAnimation.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._tlpAnimation.Controls.Add(this._lblFrameCount, 0, 0);
        this._tlpAnimation.Controls.Add(this._frameCount, 1, 0);
        this._tlpAnimation.Controls.Add(this._lblAnimDuration, 0, 1);
        this._tlpAnimation.Controls.Add(this._animDurationH, 1, 1);
        this._tlpAnimation.Controls.Add(this._lblPlayFps, 0, 2);
        this._tlpAnimation.Controls.Add(this._playFps, 1, 2);
        this._tlpAnimation.Controls.Add(this._lblAnimBtn, 0, 3);
        this._tlpAnimation.Controls.Add(this._animBtn, 1, 3);
        this._tlpAnimation.Dock = System.Windows.Forms.DockStyle.Fill;
        this._tlpAnimation.Name = "_tlpAnimation";
        this._tlpAnimation.RowCount = 4;
        this._tlpAnimation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpAnimation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpAnimation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._tlpAnimation.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // _lblFrameCount
        //
        this._lblFrameCount.AutoSize = false;
        this._lblFrameCount.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblFrameCount.Name = "_lblFrameCount";
        this._lblFrameCount.Size = new System.Drawing.Size(175, 24);
        this._lblFrameCount.Text = "Frame count";
        this._lblFrameCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _frameCount
        //
        this._frameCount.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._frameCount.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._frameCount.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
        this._frameCount.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        this._frameCount.Name = "_frameCount";
        this._frameCount.Size = new System.Drawing.Size(260, 23);
        this._frameCount.Value = new decimal(new int[] { 24, 0, 0, 0 });
        //
        // _lblAnimDuration
        //
        this._lblAnimDuration.AutoSize = false;
        this._lblAnimDuration.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblAnimDuration.Name = "_lblAnimDuration";
        this._lblAnimDuration.Size = new System.Drawing.Size(175, 24);
        this._lblAnimDuration.Text = "Duration (h)";
        this._lblAnimDuration.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _animDurationH
        //
        this._animDurationH.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._animDurationH.DecimalPlaces = 2;
        this._animDurationH.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._animDurationH.Maximum = new decimal(new int[] { 168, 0, 0, 0 });
        this._animDurationH.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        this._animDurationH.Name = "_animDurationH";
        this._animDurationH.Size = new System.Drawing.Size(260, 23);
        this._animDurationH.Value = new decimal(new int[] { 24, 0, 0, 0 });
        //
        // _lblPlayFps
        //
        this._lblPlayFps.AutoSize = false;
        this._lblPlayFps.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblPlayFps.Name = "_lblPlayFps";
        this._lblPlayFps.Size = new System.Drawing.Size(175, 24);
        this._lblPlayFps.Text = "Playback FPS";
        this._lblPlayFps.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _playFps
        //
        this._playFps.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._playFps.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._playFps.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        this._playFps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this._playFps.Name = "_playFps";
        this._playFps.Size = new System.Drawing.Size(260, 23);
        this._playFps.Value = new decimal(new int[] { 10, 0, 0, 0 });
        //
        // _lblAnimBtn
        //
        this._lblAnimBtn.AutoSize = false;
        this._lblAnimBtn.Margin = new System.Windows.Forms.Padding(0, 4, 4, 0);
        this._lblAnimBtn.Name = "_lblAnimBtn";
        this._lblAnimBtn.Size = new System.Drawing.Size(175, 24);
        this._lblAnimBtn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _animBtn
        //
        this._animBtn.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this._animBtn.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
        this._animBtn.Name = "_animBtn";
        this._animBtn.Size = new System.Drawing.Size(260, 26);
        this._animBtn.Text = "Play";
        this._animBtn.UseVisualStyleBackColor = true;
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1700, 1080);
        this.Controls.Add(this._split);
        this.KeyPreview = true;
        this.Name = "MainForm";
        this.Text = "Constellation Planner";
        ((System.ComponentModel.ISupportInitialize)(this._playFps)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._animDurationH)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._frameCount)).EndInit();
        this._tlpAnimation.ResumeLayout(false);
        this._grpAnimation.ResumeLayout(false);
        this._tlpLayers.ResumeLayout(false);
        this._tlpLayers.PerformLayout();
        this._grpLayers.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._upscale)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._timeOffset)).EndInit();
        this._tlpRender.ResumeLayout(false);
        this._grpRender.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._latencyLimit)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._requiredRate)).EndInit();
        this._tlpRelayPath.ResumeLayout(false);
        this._grpRelayPath.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._islTxPower)).EndInit();
        this._tlpIslAntennas.ResumeLayout(false);
        this._grpIslAntennas.ResumeLayout(false);
        this._grpGroundAimList.ResumeLayout(false);
        this._grpGroundAimList.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._groundTxPower)).EndInit();
        this._tlpGroundAntennas.ResumeLayout(false);
        this._grpGroundAntennas.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._techLevel)).EndInit();
        this._tlpSatHardware.ResumeLayout(false);
        this._grpSatHardware.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._minElev)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._phaseOffset)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._f)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._p)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._t)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._inclination)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._argPe)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._lanOffset)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._apogee)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this._altitude)).EndInit();
        this._tlpConstellation.ResumeLayout(false);
        this._grpConstellation.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._pic)).EndInit();
        this._leftFlow.ResumeLayout(false);
        this._layersColumn.ResumeLayout(false);
        this._bottomFlow.ResumeLayout(false);
        this._bottomFlow.PerformLayout();
        this._rightSplit.Panel1.ResumeLayout(false);
        this._rightSplit.Panel1.PerformLayout();
        this._rightSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this._rightSplit)).EndInit();
        this._rightSplit.ResumeLayout(false);
        this._split.Panel1.ResumeLayout(false);
        this._split.Panel2.ResumeLayout(false);
        this._split.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
        this._split.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
