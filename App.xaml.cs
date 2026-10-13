// Copyright © 2026 Triamec Motion AG

namespace TraStFilePlayer {
	public partial class App : Application {
		public App() {
			InitializeComponent();
			MainPage = new MainPage();
		}

		protected override Window CreateWindow(IActivationState? activationState) {
			var window = base.CreateWindow(activationState);
			window.Title = "Trast File Player";
			window.Width = 620;
			window.Height = 850;
			return window;
		}
	}
}
