using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Documents.TreeView;

// Adds a tool window and a command that will show it. The command is added to the View menu and a
// keyboard shortcut is added to the main window. Keyboard shortcut Ctrl+Alt+Z shows the tool window.

namespace TraceSpy.Extension {
	// Adds the 'OpenToolWindow' command to the main window and sets keyboard shortcut to Ctrl+Alt+Z
	[ExportAutoLoaded]
	sealed class ToolWindowLoader : IAutoLoaded {
		public static readonly RoutedCommand OpenToolWindow = new RoutedCommand("OpenToolWindow", typeof(ToolWindowLoader));

		[ImportingConstructor]
		ToolWindowLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(OpenToolWindow, new RelayCommand(a => toolWindowService.Show(ToolWindowContentImpl.THE_GUID)));
			cmds.Add(OpenToolWindow, ModifierKeys.Control | ModifierKeys.Alt, Key.Z);
		}
	}

	// Adds a menu item to the View menu to show the tool window
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Trace Spy", InputGestureText = "Ctrl+Alt+Z", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 2000)]
	sealed class ViewCommand1 : MenuItemCommand {
		ViewCommand1()
			: base(ToolWindowLoader.OpenToolWindow) {
		}
	}

    [ExportMenuItem(Header = "Trace Spy: Filter for selected PID", Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 50)]
    sealed class TraceSpyFilterPIDCtxMenuCommand : MenuItemBase
    {
 
        [ImportingConstructor]
        TraceSpyFilterPIDCtxMenuCommand()
        {
            
        }

        //public override bool IsEnabled(IMenuItemContext context) => ToolWindowControl.TSpyListView.HasItems;
        public override bool IsEnabled(IMenuItemContext context)
        {
            if (TraceSpy.Extension.ToolWindowControl.TSpy != null && TraceSpy.Extension.ToolWindowControl.TSpy.Queue.Count > 0) return true;
            else return false;
        }
        public override void Execute(IMenuItemContext context)
        {
            if(TraceSpy.Extension.ToolWindowControl.TSpy != null && ToolWindowControl.TSpyListView != null && ToolWindowControl.TSpyListView.SelectedItem != null)
            {
                Tracers.TraceLine line = ToolWindowControl.TSpyListView.SelectedItem as Tracers.TraceLine;
                ToolWindowControl.Instance.TraceSpyFilterPID.Text = line.Pid.ToString();
                TraceSpy.Extension.ToolWindowControl.TSpy.FilterPID = line.Pid;
            }
            else
            {
                TraceSpyLogger.Instance.WriteLine(TextColor.Error, "TraceSpy: You need to select an exitisting TraceLine.");
                //for debugging until we have a custom GUID for the ListView
                //TraceSpyLogger.Instance.WriteLine(TextColor.Error, "TraceSpy: context.CreatorObject.Guid: " + context.CreatorObject.Guid);
            }
        }
        //public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid("9bd7c228-91a0-4140-8e8b-ab0450b418ca");
        public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_UNKNOWN_GUID); 

    }
	


    // Dummy dependency "needed" by MainToolWindowContentProvider
    [Export]
	sealed class DeppDep {
		public void Hello() {
		}
	}

	// Called by dnSpy to create the tool window
	[Export(typeof(IToolWindowContentProvider))]
	sealed class MainToolWindowContentProvider : IToolWindowContentProvider {
		// Caches the created tool window
		ToolWindowContentImpl ToolWindowContent => traceSpyToolWindowContent ?? (traceSpyToolWindowContent = new ToolWindowContentImpl());
		ToolWindowContentImpl traceSpyToolWindowContent;

		// Add any deps to the constructor if needed, else remove the constructor
		[ImportingConstructor]
		MainToolWindowContentProvider(DeppDep deppDep) {
			deppDep.Hello();
		}

		// Lets dnSpy know which tool windows it can create and their default locations
		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ToolWindowContentImpl.THE_GUID, ToolWindowContentImpl.DEFAULT_LOCATION, 0, false); }
		}

		// Called by dnSpy. If it's your tool window guid, return the instance. Make sure it's
		// cached since it can be called multiple times.
		public ToolWindowContent GetOrCreate(Guid guid) {
			if (guid == ToolWindowContentImpl.THE_GUID)
				return ToolWindowContent;
			return null;
		}
	}

	sealed class ToolWindowContentImpl : ToolWindowContent
    {
        //DONE: Use your own guid
        //public static readonly Guid THE_GUID = new Guid("18785447-21A8-41DB-B8AD-0F166AEC0D08");
        public static readonly Guid THE_GUID = new Guid("9fa674c6-d1b0-42de-832d-ceb5c561987d");
        public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override Guid Guid => THE_GUID;
		public override string Title => "TraceSpy";

		// This is the object shown in the UI. Return a WPF object or a .NET object with a DataTemplate
		public override object UIObject => toolWindowControl;

        // The element inside UIObject that gets the focus when the tool window should be focused.
        // If it's not as easy as calling FocusedElement.Focus() to focus it, you must implement
        // dnSpy.Contracts.Controls.IFocusable.
        //public override IInputElement FocusedElement => toolWindowControl.option1TextBox;
        public override IInputElement FocusedElement => toolWindowControl.TraceSpyButton1;

        // The element that gets scaled when the user zooms in or out. Return null if zooming isn't
        // possible
        public override FrameworkElement ZoomElement => toolWindowControl;

		readonly ToolWindowControl toolWindowControl;
		readonly ToolWindowVM toolWindowVM;

		public ToolWindowContentImpl() {
			this.toolWindowControl = new ToolWindowControl();
			this.toolWindowVM = new ToolWindowVM();
			this.toolWindowControl.DataContext = this.toolWindowVM;
		}

		// Gets notified when the content gets hidden, visible, etc. Can be used to tell the view
		// model to stop doing stuff when it gets hidden in case it does a lot of work.
		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				toolWindowVM.IsEnabled = true;
				break;

			case ToolWindowContentVisibilityEvent.Removed:
				toolWindowVM.IsEnabled = false;
				break;

			case ToolWindowContentVisibilityEvent.Visible:
				toolWindowVM.IsVisible = true;
				break;

			case ToolWindowContentVisibilityEvent.Hidden:
				toolWindowVM.IsVisible = false;
				break;
			}
		}
	}

	sealed class ToolWindowVM : ViewModelBase {
		public bool IsEnabled { get; set; }
		public bool IsVisible { get; set; }
	}
}
