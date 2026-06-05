using System.Reflection;

namespace Presentation.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Путь к XML-файлу с документацией текущего проекта
            var currentXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var currentXmlPath = Path.Combine(AppContext.BaseDirectory, currentXmlFile);
            options.IncludeXmlComments(currentXmlPath);

            // Ищем остальные xml
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlPath in xmlFiles)
            {
                try
                {
                    options.IncludeXmlComments(xmlPath);
                }
                catch
                {
                    // Игнорируем файлы, которые не являются валидной документацией
                }
            }
        });

        return services;
    }
}

