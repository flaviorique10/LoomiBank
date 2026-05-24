using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Customers.Application.Services;

namespace Customers.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "profile-pictures";

    public AzureBlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadProfilePictureAsync(Stream fileStream, string fileName, string contentType)
    {
        // 1. Pega a referência do container que criamos na Azure
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

        // 2. Cria um nome único para o arquivo (evita que a foto do João sobrescreva a da Maria se tiverem o mesmo nome)
        var uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        // 3. Configura o Content-Type (ex: image/jpeg) para a foto abrir no navegador em vez de forçar o download
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

        // 4. Faz o envio do arquivo para a nuvem
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

        // 5. Retorna a URL pública gerada
        return blobClient.Uri.ToString();
    }
}