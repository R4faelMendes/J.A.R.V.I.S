// Ponto de entrada standalone (dotnet run). Embutido, o Desktop usa HospedeJarvis.Criar().
using Jarvis.Server;

var app = HospedeJarvis.Criar(args);
app.Run();
