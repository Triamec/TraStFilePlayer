// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer.WinUI {
	public partial class App : MauiWinUIApplication {
		public App() {
			string userDataFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Triamec",
				"TraStFilePlayer",
				"WebView2");
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
			InitializeComponent();
		}

		protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
	}
}
