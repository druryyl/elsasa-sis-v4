namespace ElsasaSis.Modules.Fin.Domain;

public interface IReceiptSequenceGenerator
{
    Task<string> GenerateNextReceiptNumberAsync(string month, CancellationToken cancellationToken = default);
}
