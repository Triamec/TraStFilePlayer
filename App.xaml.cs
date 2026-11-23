// Copyright © 2026 Triamec Motion AG

using TraStFilePlayer.Services;

namespace TraStFilePlayer {
	public partial class App : Application {
		readonly MotionSystemService _motionSystemService;
		readonly PlaybackService _playbackService;

		/// <summary>
		/// Initializes a new instance of the <see cref="App"/> class.
		/// </summary>
		/// <param name="serviceProvider">The application service provider.</param>
		public App(IServiceProvider serviceProvider) {
			ArgumentNullException.ThrowIfNull(serviceProvider);
			_motionSystemService = serviceProvider.GetRequiredService<MotionSystemService>();
			_playbackService = serviceProvider.GetRequiredService<PlaybackService>();
			InitializeComponent();
			MainPage = new MainPage();
		}

		/// <inheritdoc/>
		protected override Window CreateWindow(IActivationState? activationState) {
			var window = base.CreateWindow(activationState);
			window.Title = "Trast File Player";
			window.Width = 620;
			window.Height = 850;

#if WINDOWS
			window.Created += (s, e) => {
				Microsoft.UI.Xaml.Window? nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
				if (nativeWindow != null) {
					Microsoft.UI.Windowing.AppWindow? appWindow = nativeWindow.AppWindow;
					if (appWindow != null) {
						bool isShuttingDown = false;
						bool isShutdownComplete = false;

						appWindow.Closing += async (sender, args) => {
							if (isShutdownComplete) {
								return;
							}

							args.Cancel = true;

							if (isShuttingDown) {
								return;
							}

							isShuttingDown = true;
							try {
								await _playbackService.DisposeAsync().ConfigureAwait(false);
								await _motionSystemService.DisposeAsync().ConfigureAwait(false);
#pragma warning disable CA1031 // Do not catch general exception types
							} catch (Exception ex) {
#pragma warning restore CA1031
								System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex}");
							} finally {
								isShutdownComplete = true;
								if (nativeWindow.DispatcherQueue.HasThreadAccess) {
									nativeWindow.Close();
								} else {
									nativeWindow.DispatcherQueue.TryEnqueue(() => nativeWindow.Close());
								}
							}
						};
					}
				}
			};
#endif

			window.Destroying += (s, e) => {
				_motionSystemService.Dispose();
			};

			return window;
		}
	}
}
