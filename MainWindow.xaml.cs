using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SurfaceTouchDeck.Models;
using SurfaceTouchDeck.Services;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace SurfaceTouchDeck;

public partial class MainWindow : Window
{
    private readonly LayoutStore _layoutStore = new();
    private readonly StyleResolver _styleResolver = new();
    private readonly NotifyIcon _tray = new();

    private LayoutDefinition _layout = null!;
    private string _currentStyleSelection = "default";
    private EditMode _editMode = EditMode.Green;
    private bool _homeHeld;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureTray();
        HookMenuEvents();
        RestoreLastMode();
        LoadLayout("layout1");
        RenderControls();
    }


    private void RestoreLastMode()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "last-mode.txt");
        if (File.Exists(path) && Enum.TryParse<EditMode>(File.ReadAllText(path), out var mode))
        {
            _editMode = mode;
        }
    }
    private void ConfigureTray()
    {
        _tray.Icon = System.Drawing.SystemIcons.Application;
        _tray.Visible = true;
        _tray.Text = "Surface Touch Deck";
        _tray.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowHomeMenu();
            }
            else if (e.Button == MouseButtons.Right)
            {
                var context = new ContextMenuStrip();
                context.Items.Add("Quit", null, (_, _) => Application.Current.Shutdown());
                context.Show(Cursor.Position);
            }
        };
    }

    private void HookMenuEvents()
    {
        OpacitySlider.ValueChanged += (_, _) =>
        {
            _layout.UiOpacity = OpacitySlider.Value / 100.0;
            OpacityLabel.Text = $"{OpacitySlider.Value:0}% 🪟";
            OverlayCanvas.Opacity = _layout.UiOpacity;
            _layoutStore.Save(_layout);
        };

        SizeSlider.ValueChanged += (_, _) =>
        {
            _layout.UiScale = SizeSlider.Value / 100.0;
            SizeLabel.Text = $"{SizeSlider.Value:0}% 🔍";
            RenderControls();
            _layoutStore.Save(_layout);
        };

        SelectStyleButton.Click += (_, _) => OpenStyleMenu();
        ExitButton.Click += (_, _) => Application.Current.Shutdown();
        StyleCancel.Click += (_, _) => StyleMenu.Visibility = Visibility.Collapsed;
        StyleConfirm.Click += (_, _) =>
        {
            _layout.StyleName = _currentStyleSelection;
            _layoutStore.Save(_layout);
            StyleMenu.Visibility = Visibility.Collapsed;
            RenderControls();
        };

        EditCancel.Click += (_, _) => EditBar.Visibility = Visibility.Collapsed;
        EditSave.Click += (_, _) =>
        {
            _layoutStore.Save(_layout);
            EditBar.Visibility = Visibility.Collapsed;
        };

        AddButton.Click += (_, _) => ShowAddMenuStub();
        TrashButton.Click += (_, _) => MessageBox.Show("Select an element in purple mode to remove it.", "Trash");
    }

    private void LoadLayout(string name)
    {
        _layout = _layoutStore.LoadOrCreate(name);
        _currentStyleSelection = _layout.StyleName;
        OpacitySlider.Value = _layout.UiOpacity * 100;
        SizeSlider.Value = _layout.UiScale * 100;
        BuildLayoutList();
    }

    private void BuildLayoutList()
    {
        LayoutItems.Items.Clear();
        var layouts = _layoutStore.ListLayoutNames();
        if (layouts.Count == 0)
        {
            layouts = ["layout1", "layout2", "layout3", "layout4"];
        }

        foreach (var layoutName in layouts)
        {
            var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var delete = new Button { Content = "X", Width = 26, Height = 24, Margin = new Thickness(0, 0, 6, 0) };
            delete.Click += (_, _) =>
            {
                if (MessageBox.Show($"Delete {layoutName}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _layoutStore.Delete(layoutName);
                    BuildLayoutList();
                }
            };

            var edit = new Button { Content = "✎", Width = 26, Height = 24, Margin = new Thickness(6, 0, 0, 0) };
            edit.Click += (_, _) => EnterEditMode();

            var select = new Button { Content = layoutName, MinWidth = 130, HorizontalContentAlignment = HorizontalAlignment.Left };
            select.Click += (_, _) =>
            {
                LoadLayout(layoutName);
                RenderControls();
            };

            DockPanel.SetDock(delete, Dock.Left);
            DockPanel.SetDock(edit, Dock.Right);
            row.Children.Add(delete);
            row.Children.Add(edit);
            row.Children.Add(select);
            LayoutItems.Items.Add(row);
        }
    }

    private void OpenStyleMenu()
    {
        StyleList.ItemsSource = _styleResolver.ListStyles();
        StyleList.SelectionChanged += (_, _) =>
        {
            if (StyleList.SelectedItem is string style)
            {
                _currentStyleSelection = style;
                _layout.StyleName = style;
                RenderControls();
            }
        };

        StyleList.SelectedItem = _currentStyleSelection;
        StyleMenu.Visibility = Visibility.Visible;
    }

    private void RenderControls()
    {
        OverlayCanvas.Children.Clear();
        OverlayCanvas.Opacity = _layout.UiOpacity;
        foreach (var control in _layout.Controls)
        {
            if (_editMode == EditMode.Purple && !control.Enabled)
            {
                continue;
            }

            var visual = BuildControlVisual(control);
            OverlayCanvas.Children.Add(visual);
        }

        if (_layout.UseClassicAbxyCluster)
        {
            DrawAbxyClusterCross();
        }
    }

    private FrameworkElement BuildControlVisual(ControlDefinition control)
    {
        var baseSize = 72.0;
        var scale = _layout.UiScale <= 0 ? 0.01 : _layout.UiScale;
        var controlSize = baseSize * control.Size * scale;

        if (_layout.UseClassicAbxyCluster && control.IsClassicClusterMember)
        {
            controlSize *= 0.9;
        }

        var border = new Border
        {
            Width = controlSize,
            Height = controlSize,
            CornerRadius = new CornerRadius(controlSize / 2),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            BorderBrush = ResolveBorder(control),
            BorderThickness = new Thickness(2),
            Child = BuildInnerContent(control)
        };

        border.MouseLeftButtonDown += (_, e) =>
        {
            if (control.Kind == ControlKind.Home)
            {
                _homeHeld = true;
                _ = Task.Delay(600).ContinueWith(_ => Dispatcher.Invoke(() =>
                {
                    if (_homeHeld)
                    {
                        ShowHomeMenu();
                    }
                }));
            }

            if (_editMode == EditMode.Green)
            {
                control.Enabled = !control.Enabled;
                RenderControls();
            }

            e.Handled = true;
        };

        border.MouseLeftButtonUp += (_, _) => _homeHeld = false;

        border.MouseRightButtonUp += (_, _) => ShowButtonConfig(control);

        if (_editMode == EditMode.Purple)
        {
            border.MouseMove += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var p = e.GetPosition(OverlayCanvas);
                    control.X = p.X - (controlSize / 2);
                    control.Y = p.Y - (controlSize / 2);
                    Canvas.SetLeft(border, control.X);
                    Canvas.SetTop(border, control.Y);
                }
            };
        }

        Canvas.SetLeft(border, ControlLeft(control, controlSize));
        Canvas.SetTop(border, ControlTop(control, controlSize));

        return border;
    }

    private static double ControlLeft(ControlDefinition c, double size) => c.X + ((72 * c.Size) - size) / 2;

    private static double ControlTop(ControlDefinition c, double size) => c.Y + ((72 * c.Size) - size) / 2;

    private Brush ResolveBorder(ControlDefinition control)
    {
        if (!control.Enabled)
        {
            return Brushes.Yellow;
        }

        if (_editMode == EditMode.Green)
        {
            return control.Kind == ControlKind.Home ? Brushes.MediumPurple : Brushes.Lime;
        }

        return control.Kind == ControlKind.Home ? Brushes.Lime : Brushes.MediumPurple;
    }

    private UIElement BuildInnerContent(ControlDefinition control)
    {
        var image = _styleResolver.TryResolveIcon(_layout.StyleName, control.ButtonName, pressed: false);
        if (image is not null)
        {
            return new Image { Source = image, Stretch = Stretch.Fill, Opacity = 0.95 };
        }

        return new TextBlock
        {
            Text = control.Id.ToUpperInvariant(),
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
    }

    private void DrawAbxyClusterCross()
    {
        var cluster = _layout.Controls.Where(c => c.IsClassicClusterMember).ToList();
        if (cluster.Count != 4)
        {
            return;
        }

        var center = new Point(cluster.Average(c => c.X + 36), cluster.Average(c => c.Y + 36));

        OverlayCanvas.Children.Add(new Line
        {
            X1 = center.X - 48,
            Y1 = center.Y,
            X2 = center.X + 48,
            Y2 = center.Y,
            Stroke = Brushes.White,
            StrokeThickness = 1.5
        });

        OverlayCanvas.Children.Add(new Line
        {
            X1 = center.X,
            Y1 = center.Y - 48,
            X2 = center.X,
            Y2 = center.Y + 48,
            Stroke = Brushes.White,
            StrokeThickness = 1.5
        });

        var centerButton = new Ellipse
        {
            Width = 24,
            Height = 24,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };

        centerButton.MouseLeftButtonDown += (_, _) =>
        {
            centerButton.Fill = Brushes.MediumPurple;
            _layout.UseClassicAbxyCluster = true;
            _layoutStore.Save(_layout);
        };

        Canvas.SetLeft(centerButton, center.X - 12);
        Canvas.SetTop(centerButton, center.Y - 12);
        OverlayCanvas.Children.Add(centerButton);
    }

    private void EnterEditMode()
    {
        EditBar.Visibility = Visibility.Visible;
        _editMode = _editMode == EditMode.Green ? EditMode.Purple : EditMode.Green;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "last-mode.txt"), _editMode.ToString());
        RenderControls();
    }

    private void ShowHomeMenu()
    {
        HomeMenu.Visibility = Visibility.Visible;
        HomeMenu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        HomeMenu.Width = HomeMenu.DesiredSize.Width;
    }

    private void ShowAddMenuStub()
    {
        MessageBox.Show("Add menu: shows faded Xbox 360 baseline, ABXY classic-center lock, Cancel/Confirm, and stacked center placement on confirm.", "Add controls");
    }

    private void ShowButtonConfig(ControlDefinition control)
    {
        var win = new Window
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            Width = 300,
            Height = control.Kind == ControlKind.Joystick ? 260 : 210,
            Title = control.Kind == ControlKind.Joystick ? "Joystick Config" : "Button Config"
        };

        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(new TextBlock { Text = $"assigned key: [{control.AssignedKey}]", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new TextBlock { Text = "default controller key", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new TextBlock { Text = $"button name: [{control.ButtonName}]", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

        if (control.Kind == ControlKind.Joystick)
        {
            stack.Children.Add(new TextBlock { Text = "assigned task: [controller / wasd / ← ↑ → ↓]", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) });
            stack.Children.Add(new TextBlock { Text = "mode: [freestyle / fixed]", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) });
            stack.Children.Add(new TextBlock { Text = "defined area: [on/off]", Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 6) });
        }

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => win.Close();
        var save = new Button { Content = "Save" };
        save.Click += (_, _) => { _layoutStore.Save(_layout); win.Close(); };
        actions.Children.Add(cancel);
        actions.Children.Add(save);
        stack.Children.Add(actions);

        win.Content = stack;
        win.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        _tray.Visible = false;
        _tray.Dispose();
        base.OnClosed(e);
    }
}
