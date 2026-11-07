using Avalonia.Controls;

namespace Lumio.Views;

public partial class MainView : DrawerPage
{
    public MainView()
    {
        InitializeComponent();
        // On phones the drawer overlays the content: close it once a section is picked.
        SectionList.SelectionChanged += (_, _) =>
        {
            if (DisplayMode is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.CompactOverlay)
                IsOpen = false;
        };
    }

    public NavigationPage RootNavigation => this.RootNavigationPage;
}
