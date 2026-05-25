namespace Customers.Application.Services;

public interface IBlobStorageService
{
    // Recebe o arquivo em formato de fluxo de dados (Stream) para manter a camada limpa de dependências HTTP
    Task<string> UploadProfilePictureAsync(Stream fileStream, string fileName, string contentType);
}