namespace DataAccess.Storage;

public static class StorageErrors
{
    public static string PageNotFound(int page, int totalPages) =>
        $"Указанная страница {page} не существует. Максимальная в текущем запросе страница - {totalPages}";

    public static readonly string PageMustBePositive = "Страница не может быть меньше 1";
    public static readonly string PageSizeMustBePositive = "Размер страницы не может быть меньше 1";

    public static string ItemWithIdAlreadyExist(Guid id) => 
        $"Элемент с идентификатором {id} уже присутствует в хранилище";
}
