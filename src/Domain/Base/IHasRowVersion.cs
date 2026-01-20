namespace Domain.Base;

public interface IHasRowVersion
{
    byte[] RowVersion { get;}
}