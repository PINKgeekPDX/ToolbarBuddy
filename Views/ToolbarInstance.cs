// ToolbarInstance class to manage multiple toolbars
public class ToolbarInstance
{
    private readonly MainWindow _mainWindow;
    private readonly ConfigurationService _configService;
    private readonly LoggingService _loggingService;
    private readonly PluginService _pluginService;

    private Window _toolbarWindow;
    private bool _isVisible = true;

    public ToolbarInstance(MainWindow mainWindow, ConfigurationService configService, LoggingService loggingService, PluginService pluginService)
    {
        _mainWindow = mainWindow;
        _configService = configService;
        _loggingService = loggingService;
        _pluginService = pluginService;

        InitializeToolbarWindow();
    }

    private void InitializeToolbarWindow()
    {
        _toolbarWindow = new ToolbarWindow(_configService, _loggingService, _pluginService)
        {
            Owner = _mainWindow,
            WindowStartupLocation = WindowStartupLocation.Manual
        };
        // Load toolbar position from settings
        var settingsTask = _configService.LoadConfigurationAsync();
        settingsTask.Wait();
        var settings = settingsTask.Result;
        // Set initial position (could be enhanced to have individual positions per toolbar)
        _toolbarWindow.Left = 0;
        _toolbarWindow.Top = 0;
        _toolbarWindow.Show();
    }

    public void ToggleVisibility()
    {
        if (_isVisible)
        {
            _toolbarWindow.Hide();
            _isVisible = false;
        }
        else
        {
            _toolbarWindow.Show();
            _isVisible = true;
        }
    }

    public void ShowOrHide()
    {
        ToggleVisibility();
    }
}