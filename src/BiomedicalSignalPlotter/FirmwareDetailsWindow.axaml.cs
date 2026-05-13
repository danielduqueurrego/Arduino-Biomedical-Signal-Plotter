using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter;

public partial class FirmwareDetailsWindow : Window
{
    private readonly FirmwareInfoService _firmwareInfoService;

    public FirmwareDetailsWindow()
        : this(new FirmwareInfoService(), FirmwareUploadLog.Empty(FirmwareInfoService.ResolveDefaultSketchFolderPath()))
    {
    }

    public FirmwareDetailsWindow(FirmwareInfoService firmwareInfoService, FirmwareUploadLog uploadLog)
    {
        _firmwareInfoService = firmwareInfoService;

        InitializeComponent();

        FirmwarePathText.Text = $"Firmware file: {_firmwareInfoService.SketchFilePath}";
        UploadLogTextBox.Text = uploadLog.FormatForDisplay();
        _ = LoadFirmwareSourceAsync();
    }

    private async Task LoadFirmwareSourceAsync()
    {
        FirmwareSourceTextBox.Text = "Loading firmware source...";
        FirmwareSourceReadResult result = await _firmwareInfoService.ReadSketchAsync();
        FirmwareDetailsStatusText.Text = result.Message;
        FirmwareSourceTextBox.Text = result.Succeeded
            ? result.SourceText
            : result.Message;
    }

    private void OpenFirmwareFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(_firmwareInfoService.SketchFolderPath))
            {
                FirmwareDetailsStatusText.Text = $"Firmware folder not found: {_firmwareInfoService.SketchFolderPath}";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _firmwareInfoService.SketchFolderPath,
                UseShellExecute = true
            });

            FirmwareDetailsStatusText.Text = $"Opened firmware folder: {_firmwareInfoService.SketchFolderPath}";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            FirmwareDetailsStatusText.Text = $"Unable to open firmware folder. Path: {_firmwareInfoService.SketchFolderPath}. {ex.Message}";
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
