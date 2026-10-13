// Copyright © 2026 Triamec Motion AG

using Microsoft.Extensions.Logging;
using TraStFilePlayer.Services;

namespace TraStFilePlayer {
	internal static class MauiProgram {
		public static MauiApp CreateMauiApp() {
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts => {
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});

			builder.Services.AddMauiBlazorWebView();

			builder.Services.AddBlazorWebViewDeveloperTools();
			builder.Logging.AddDebug();

			//builder.Services.AddSingleton<RecentFilesService>();
			builder.Services.AddSingleton<MotionSystemService>();
			builder.Services.AddSingleton<PlaybackService>(sp =>
				new PlaybackService(sp.GetRequiredService<MotionSystemService>()));

			return builder.Build();
		}
	}
}
