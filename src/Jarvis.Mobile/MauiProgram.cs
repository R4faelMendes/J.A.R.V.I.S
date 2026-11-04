using Microsoft.Extensions.Logging;

namespace Jarvis.Mobile;
using SkiaSharp.Views.Maui.Controls.Hosting;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("IBMPlexSans-Regular.ttf", "IBMPlexSansRegular");
                fonts.AddFont("IBMPlexSans-Medium.ttf", "IBMPlexSansMedium");
                fonts.AddFont("IBMPlexSans-SemiBold.ttf", "IBMPlexSansSemiBold");
                fonts.AddFont("ChakraPetch-SemiBold.ttf", "ChakraPetchSemiBold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
